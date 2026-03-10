/*﻿/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using HelseId.Library.ClientCredentials.Interfaces;
using HelseId.Library.Configuration;
using HelseId.Library.ExtensionMethods;
using HelseId.Library.Interfaces.JwtTokens;
using HelseId.Library.Models;
using HelseId.Library.Models.DetailsFromClient;
using Helsenorge.Registries.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Utilities;

internal class RestServiceInvoker
{
    private readonly ILogger _logger;
    private readonly ProxyHttpClientFactory _proxyHttpClientFactory;

    private readonly IHelseIdClientCredentialsFlow _helseIdClientCredentialsFlow;
    private readonly IDPoPProofCreatorForApiRequests _dPoPProofCreator;
    private readonly OrganizationNumbers _organizationNumbers; // TODO register in config
    private readonly HelseIdConfiguration _helseIdConfiguration;

    internal RestServiceInvoker(ILogger logger, 
        ProxyHttpClientFactory proxyHttpClientFactory,
            IHelseIdClientCredentialsFlow helseIdClientCredentialsFlow,
            IDPoPProofCreatorForApiRequests dPoPProofCreator,
            OrganizationNumbers organizationNumbers,
            HelseIdConfiguration helseIdConfiguration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _proxyHttpClientFactory =
            proxyHttpClientFactory ?? throw new ArgumentNullException(nameof(proxyHttpClientFactory));

        _helseIdClientCredentialsFlow = helseIdClientCredentialsFlow ?? throw new ArgumentNullException(nameof(helseIdClientCredentialsFlow));
        _dPoPProofCreator = dPoPProofCreator ?? throw new ArgumentNullException(nameof(dPoPProofCreator));         
        _organizationNumbers = organizationNumbers ?? throw new ArgumentNullException(nameof(organizationNumbers)); 
        _helseIdConfiguration = helseIdConfiguration ?? throw new ArgumentNullException(nameof(helseIdConfiguration)); 
    }

    [ExcludeFromCodeCoverage] // requires wire communication
    internal async Task<string> ExecuteAsync(RequestParameters request, string operationName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(operationName);

        var httpClient = _proxyHttpClientFactory.CreateHttpClient();

        var httpRequest = await GenerateAuthorizationToken(request, httpClient.BaseAddress.AbsolutePath);

        var absoluteUri = new Uri(httpClient.BaseAddress, httpRequest.RequestUri);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Start-ServiceCall: {OperationName} {Address}",
                operationName, absoluteUri);
            stopwatch.Start();

            var response = await httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
            {
                await TryLogContent(response);
                throw new HttpRequestException(
                    $"Error calling {operationName} on {absoluteUri}. Status code: {response.StatusCode}");
            }
            var result = await response.Content.ReadAsStringAsync();

            stopwatch.Stop();
            _logger.LogInformation("End-ServiceCall: {OperationName} {Address} Execution time: {ElapsedMilliseconds} ms",
                operationName, absoluteUri, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (HttpRequestException ex)
        {
            ex.Data.Add("Endpoint-Name", absoluteUri.Host);
            ex.Data.Add("Endpoint-Address", absoluteUri.AbsoluteUri);
            ex.Data.Add("Endpoint-Operation", operationName);
            throw;
        }
    }

    private async Task<HttpRequestMessage> GenerateAuthorizationToken(RequestParameters request, string baseAddress)
    {
        var httpRequest = new HttpRequestMessage(request.Method, request.PathAndQuery);
        httpRequest.Headers.Add("Accept", request.AcceptHeader);

        _logger.LogInformation("Generation AccessToken");
        TokenResponse response;
        if (string.IsNullOrEmpty(_helseIdConfiguration.Scope) && !_organizationNumbers.HasOrganizationNumbers)
        {
            response = await _helseIdClientCredentialsFlow.GetTokenResponseAsync();
        }
        else if (string.IsNullOrEmpty(_helseIdConfiguration.Scope))
        {
            response = await _helseIdClientCredentialsFlow.GetTokenResponseAsync(_organizationNumbers);
        }
        else
        {
            response = await _helseIdClientCredentialsFlow.GetTokenResponseAsync(_helseIdConfiguration.Scope, _organizationNumbers);
        }

        if (!response.IsSuccessful(out var accessTokenResponse))
        {
            // Handle an error response from HelseID
            var errorResponse = response.AsError();

            throw new HttpRequestException($"{errorResponse.Error} {errorResponse.ErrorDescription}");
        }

        _logger.LogInformation("Generation DPOP proof");

        var dpopProof = await _dPoPProofCreator.CreateDPoPProofForApiRequest(
            HttpMethod.Post,
            baseAddress + request.Path,
            accessTokenResponse);

        httpRequest.Headers.Add("DPoP", dpopProof);

        if (request.IsDpopEnabled)
        {
            _logger.LogInformation("Authorization DPOP");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("DPoP", accessTokenResponse.AccessToken);
        }
        else
        {
            _logger.LogInformation("Authorization Bearer");
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessTokenResponse.AccessToken);
        }

        return httpRequest;
    }

    private async Task TryLogContent(HttpResponseMessage response)
    {
        try
        {
            _logger.LogInformation($"Request unsuccessful. Response content: {await response.Content.ReadAsStringAsync()}");
        }
        catch
        {
            // ignored
        }
    }
}

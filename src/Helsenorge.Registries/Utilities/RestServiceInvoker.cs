/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using HelseId.Library;
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
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Utilities;

internal class RestServiceInvoker
{
    private readonly ILogger _logger;
    private readonly ProxyHttpClientFactory _proxyHttpClientFactory;

    private readonly IHelseIdClientCredentialsFlow _helseIdClientCredentialsFlow;
    private readonly IDPoPProofCreatorForApiRequests _dPoPProofCreator;
    private readonly OrganizationNumbers _organizationNumbers; 
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

        var httpRequest = await GenerateAuthorizationToken(request, httpClient.BaseAddress);

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

    private async Task<HttpRequestMessage> GenerateAuthorizationToken(RequestParameters request, Uri baseAddress)
    {
        var httpRequest = new HttpRequestMessage(request.Method, request.Path);
        httpRequest.Headers.Add("Accept", request.AcceptHeader);

        TokenResponse response;
        if (!string.IsNullOrEmpty(_helseIdConfiguration.Scope) && _organizationNumbers.HasOrganizationNumbers)
        {
            _logger.LogInformation("Generate AccessToken with Scope and OrganizationNumbers");
            response = await _helseIdClientCredentialsFlow.GetTokenResponseAsync(_helseIdConfiguration.Scope, _organizationNumbers);
        }
        else if (_organizationNumbers.HasOrganizationNumbers)
        {
            _logger.LogInformation("Generate AccessToken with OrganizationNumbers");
            response = await _helseIdClientCredentialsFlow.GetTokenResponseAsync(_organizationNumbers);
        }
        else if (!string.IsNullOrEmpty(_helseIdConfiguration.Scope))
        {
            _logger.LogInformation("Generate AccessToken with Scope");
            response = await _helseIdClientCredentialsFlow.GetTokenResponseAsync(scope: _helseIdConfiguration.Scope);
        }
        else
        {
            _logger.LogInformation("Generate AccessToken with neither Scope or OrganizationNumbers");
            response = await _helseIdClientCredentialsFlow.GetTokenResponseAsync();
        }

        if (!response.IsSuccessful(out var accessTokenResponse))
        {
            // Handle an error response from HelseID
            var errorResponse = response.AsError();

            throw new HttpRequestException($"{errorResponse.Error} {errorResponse.ErrorDescription}");
        }

        var proofUri = baseAddress + request.Path;
        _logger.LogInformation("Generate DPOP proof for endpoint {ProofUri}", proofUri);

        var dpopProof = await _dPoPProofCreator.CreateDPoPProofForApiRequest(
            request.Method,
            proofUri,
            accessTokenResponse);

        if (request.IsDpopEnabled)
        {
            httpRequest.SetDPoPTokenAndProof(accessTokenResponse, dpopProof);
            _logger.LogInformation("Use Authorization DPOP");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("DPoP", accessTokenResponse.AccessToken);
        }
        else
        {
            _logger.LogInformation("Use Authorization Bearer");
            httpRequest.Headers.Add("DPoP", dpopProof);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenResponse.AccessToken);
        }

        return httpRequest;
    }

    private async Task TryLogContent(HttpResponseMessage response)
    {
        try
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Request unsuccessful. Response content: {ResponseContent}", responseContent);
        }
        catch
        {
            // ignored
        }
    }
}
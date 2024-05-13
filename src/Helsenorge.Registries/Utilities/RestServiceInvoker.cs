/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;
using Helsenorge.Registries.Configuration;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Utilities;

internal class RestServiceInvoker
{
    private readonly ILogger _logger;
    private readonly ProxyHttpClientFactory _proxyHttpClientFactory;

    internal RestServiceInvoker(ILogger logger, ProxyHttpClientFactory proxyHttpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _proxyHttpClientFactory =
            proxyHttpClientFactory ?? throw new ArgumentNullException(nameof(proxyHttpClientFactory));
    }

    [ExcludeFromCodeCoverage] // requires wire communication
    internal async Task<string> ExecuteAsync(RequestParameters request, string operationName)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

        var httpRequest = CreateHttpRequestMessage(request);
        var httpClient = _proxyHttpClientFactory.CreateHttpClient();
        var absoluteUri = new Uri(httpClient.BaseAddress, httpRequest.RequestUri);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Start-ServiceCall: {OperationName} {Address}",
                operationName, absoluteUri);
            stopwatch.Start();

            var response = await httpClient.SendAsync(httpRequest);
            var result = await response.Content.ReadAsStringAsync();

            stopwatch.Stop();
            _logger.LogInformation("End-ServiceCall: {OperationName} {Address} ExecutionTime: {ElapsedMilliseconds} ms",
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

    private HttpRequestMessage CreateHttpRequestMessage(RequestParameters request)
    {
        if (string.IsNullOrEmpty(request.Path)) throw new ArgumentNullException(nameof(request.Path));

        var httpRequest = new HttpRequestMessage(request.Method, request.Path);
        httpRequest.Headers.Add("Accept", "application/xml");
        httpRequest.SetBearerToken(request.BearerToken);
        return httpRequest;
    }
}
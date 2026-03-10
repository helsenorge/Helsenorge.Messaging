/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Net;
using System.Net.Http;

namespace Helsenorge.Registries.Configuration;

public class ProxyHttpClientFactory
{
    private readonly RestConfiguration _configuration;

    public ProxyHttpClientFactory(RestConfiguration restConfiguration)
    {
        _configuration = restConfiguration ?? throw new ArgumentNullException(nameof(restConfiguration));
    }

    /// <summary>
    /// Creates a new instance of <see cref="HttpClient"/> with the configuration provided.
    /// It supports proxy
    /// </summary>
    /// <returns>Instance of HttpClient with configuration provided</returns>
    /// <exception cref="ArgumentNullException">Requires RestConfiguration as class dependency</exception>
    /// <exception cref="ArgumentException">Requires a endpoint to be configured</exception>
    public HttpClient CreateHttpClient()
    {
        if (_configuration == null)
            throw new ArgumentNullException(nameof(_configuration));
        if (string.IsNullOrEmpty(_configuration.Address))
            throw new ArgumentException(nameof(_configuration.Address));

        var httpClientHandler = new HttpClientHandler();

        if (_configuration.UseDefaultWebProxy == true)
        {
            httpClientHandler.UseProxy = true;
            httpClientHandler.Proxy = WebRequest.DefaultWebProxy;
        }
        else if (_configuration.ProxyAddress != null && _configuration.ProxyAddress.IsAbsoluteUri)
        {
            httpClientHandler.UseProxy = true;
            httpClientHandler.Proxy = new WebProxy(_configuration.ProxyAddress.AbsoluteUri,
                _configuration.BypassProxyOnLocal ?? false);
        }

        var httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(_configuration.Address)
        };

        return httpClient;
    }
}
/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */


using System;

namespace Helsenorge.Registries.Configuration;

public class RestConfiguration
{
    /// <summary>
    ///     Endpoint address.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    ///     Gets or sets a value that indicates whether the auto-configured HTTP proxy of the system should be used, if
    ///     available.
    /// </summary>
    public bool? UseDefaultWebProxy { get; set; }

    /// <summary>
    ///     Gets or sets a value that indicates whether to bypass the proxy server for local addresses.
    /// </summary>
    public bool? BypassProxyOnLocal { get; set; }

    /// <summary>
    ///     Gets or sets the URI address of the HTTP proxy.
    /// </summary>
    public Uri ProxyAddress { get; set; }
}
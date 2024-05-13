/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */


namespace Helsenorge.Registries.Configuration;

public class HelseIdConfiguration
{
    /// <summary>
    ///     HelseId client id.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    ///     Endpoint address.
    /// </summary>
    public string TokenEndpoint { get; set; }

    /// <summary>
    ///     HelseId scope.
    /// </summary>
    public string ScopeName { get; set; }
}
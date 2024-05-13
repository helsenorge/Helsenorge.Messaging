/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using Helsenorge.Registries.Configuration;

namespace Helsenorge.Registries;

public class AddressRegistryRestSettings
{
    /// <summary>
    ///     The configuration containing WCF settings
    /// </summary>
    public RestConfiguration RestConfiguration { get; set; }

    /// <summary>
    ///     The amount of time values should be cached
    /// </summary>
    public TimeSpan CachingInterval { get; set; } = new(1, 0, 0);
}
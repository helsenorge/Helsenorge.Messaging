﻿/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using Helsenorge.Registries.Configuration;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Information required when communicating with the address registry
    /// </summary>
    public class AddressRegistrySettings
    {
        /// <summary>
        /// The configuration containing WCF settings
        /// </summary>
        public WcfConfiguration WcfConfiguration { get; set; }

        /// <summary>
        /// The amount of time values should be cached
        /// </summary>
        public TimeSpan CachingInterval { get; set; } = new TimeSpan(1, 0, 0);

        /// <summary>
        /// Gets or sets the type of formatter to use when caching.
        /// </summary>
        public Utilities.CacheFormatterType CachingFormatter { get; set; } =
            Utilities.CacheFormatterType.BinaryFormatter;
    }
}

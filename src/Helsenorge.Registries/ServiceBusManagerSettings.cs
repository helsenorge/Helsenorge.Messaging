/* 
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
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
    /// Information required when communicating with the ServiceBusManager service.
    /// </summary>
    public class ServiceBusManagerSettings
    {
        /// <summary>
        /// The configuration containing WCF settings
        /// </summary>
        public WcfConfiguration WcfConfiguration { get; set; }
        /// <summary>
        /// The amount of time values should be cached
        /// </summary>
        public TimeSpan CachingInterval { get; set; } = new TimeSpan(1, 0, 0);
    }
}

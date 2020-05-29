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
        public TimeSpan CachingInterval { get; set; }
    }
}

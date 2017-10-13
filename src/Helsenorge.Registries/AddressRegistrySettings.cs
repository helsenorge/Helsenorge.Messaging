using System;
using System.Configuration;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Information required when communicating with the address registry
    /// </summary>
    public class AddressRegistrySettings
    {
        /// <summary>
        /// Username used for connecting
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Password used for connecting
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// The endpoint name found in the WCF configuration
        /// </summary>
        public string EndpointName { get; set; }
        /// <summary>
        /// The configuration containing WCF settings
        /// </summary>
        public Configuration WcfConfiguration { get; set; }
        /// <summary>
        /// The amount of time values should be cached
        /// </summary>
        public TimeSpan CachingInterval { get; set; }
    }
}

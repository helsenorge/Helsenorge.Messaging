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
        /// Default constructor for AddressRegistrySettings class
        /// </summary>
        public AddressRegistrySettings()
        {
            SoapConfiguration = new SoapConfiguration
            {
                MaxBufferSize = 2147483647,
                MaxBufferPoolSize = 2147483647,
                MaxRecievedMessageSize = 2147483647
            };
        }
        /// <summary>
        /// SOAP configuration
        /// </summary>
        public SoapConfiguration SoapConfiguration  { get; set; }
        /// <summary>
        /// The amount of time values should be cached
        /// </summary>
        public TimeSpan CachingInterval { get; set; }
    }
}

using System;
using System.Configuration;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Information required when communicating with the collaboration protocol registry
    /// </summary>
    public class CollaborationProtocolRegistrySettings
    {
        /// <summary>
        /// The default constructor for CollaborationProtocolRegistrySettings class.
        /// </summary>
        public CollaborationProtocolRegistrySettings()
        {
            SoapConfiguration = new SoapConfiguration
            {
                MaxBufferSize = 262144,
                MaxBufferPoolSize = 524288,
                MaxRecievedMessageSize = 262144
            };
        }
        /// <summary>
        /// SOAP configuration
        /// </summary>
        public SoapConfiguration SoapConfiguration { get; set; }
        /// <summary>
        /// The amount of time values should be cached
        /// </summary>
        public TimeSpan CachingInterval { get; set; }
        /// <summary>
        /// The HerId that belongs to me. In CPA operations, two communication parties may be returned, need to know which one is us
        /// </summary>
        public int MyHerId { get; set; }
        /// <summary>
        /// Use online certificate revocation list (CRL) check. Default true.
        /// </summary>
        public bool UseOnlineRevocationCheck { get; set; } = true;
    }
}

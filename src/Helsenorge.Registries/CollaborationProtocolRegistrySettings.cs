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

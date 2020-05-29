using Helsenorge.Registries.Configuration;
using System;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Information required when communicating with the collaboration protocol registry
    /// </summary>
    public class CollaborationProtocolRegistrySettings
    {
        /// <summary>
        /// The configuration containing WCF settings
        /// </summary>
        public WcfConfiguration WcfConfiguration { get; set; }
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

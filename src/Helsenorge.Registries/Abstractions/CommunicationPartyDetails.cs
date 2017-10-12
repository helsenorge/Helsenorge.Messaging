using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Represents information about a communication party
    /// </summary>
    public class CommunicationPartyDetails
    {
        /// <summary>
        /// The HER-ID of the communication party. This is identifies this party in the Address Registry
        /// </summary>
        public int HerId { get; set; }
        /// <summary>
        /// Name of the communication party.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Name of the queue where the party expects synchronous messages to be received
        /// </summary>
        public string SynchronousQueueName { get; set; }
        /// <summary>
        /// Name of the queue where the party expectes asynchronous message to be received
        /// </summary>
        public string AsynchronousQueueName { get; set; }
        /// <summary>
        /// Name of the queue where the party expects error messages to be received
        /// </summary>
        public string ErrorQueueName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Defines the different types of delivery protocols we support
    /// </summary>
    public enum DeliveryProtocol
    {
        /// <summary>
        /// We don't have enough information to determine the protocol
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// We are using AMQP (service bus)
        /// </summary>
        Amqp = 1
    }

    /// <summary>
    /// Represents information about a message
    /// </summary>
    [Serializable]
    public class CollaborationProtocolMessage
    {
        /// <summary>
        /// Name of message. i.e. DIALOG_INNBYGGER_KOORDINATOR
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The delivery channel that should be used. This will be the full queue name
        /// </summary>
        public string DeliveryChannel { get; set; }
        /// <summary>
        /// The type of protocol to be used
        /// </summary>
        public DeliveryProtocol DeliveryProtocol { get; set; }
        /// <summary>
        /// A list over parts the make up the message. If a message can contain information from many different xsd's, there should be one entry per xsd.
        /// Earlier versions of some roles may not contain all xsd information, so one has to assume a default version if nothing is provided.
        /// </summary>
        public IEnumerable<CollaborationProtocolMessagePart> Parts { get; set; }
    }
}

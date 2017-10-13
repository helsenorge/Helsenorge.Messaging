using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides an interface for creating messaging entities
    /// </summary>
    public interface IMessagingFactory : ICachedMessagingEntity
    {
        /// <summary>
        /// Creates a receiver
        /// </summary>
        /// <param name="id">Id representing the receiver</param>
        /// <returns></returns>
        IMessagingReceiver CreateMessageReceiver(string id);
        /// <summary>
        /// Creates a sender
        /// </summary>
        /// <param name="id">Id representing the receiver</param>
        /// <returns></returns>
        IMessagingSender CreateMessageSender(string id);
        /// <summary>
        /// Creates an empty message
        /// </summary>
        /// <param name="stream">Stream containing the information</param>
        /// <returns></returns>
        IMessagingMessage CreteMessage(Stream stream, OutgoingMessage outgoingMessage);
    }
}

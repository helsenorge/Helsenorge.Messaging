using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Main interface for sending messages
    /// </summary>
    public interface IMessagingClient
    {
        /// <summary>
        /// Send a message and continue with other work (asynchronous messaging)
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message">Details about the message being sent</param>
        /// <returns></returns>
        Task SendAndContinueAsync(ILogger logger, OutgoingMessage message);

        /// <summary>
        /// Send a message and wait for a reply (synchronous messaging)
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message">Details about the message being sent</param>
        /// <returns></returns>
        Task<XDocument> SendAndWaitAsync(ILogger logger, OutgoingMessage message);
    }
}

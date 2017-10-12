using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides an interface for sending a message for a specific implementation
    /// </summary>
    public interface IMessagingSender : ICachedMessagingEntity
    {
        /// <summary>
        /// Sends the message
        /// </summary>
        /// <param name="message">The messag to send</param>
        /// <returns></returns>
        Task SendAsync(IMessagingMessage message);
    }
}

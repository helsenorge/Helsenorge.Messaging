using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Interface for items in <see cref="MessagingEntityCache{T}"/>
    /// Since those operations are thread safe (locking) we don't support async methods
    /// </summary>
    public interface ICachedMessagingEntity
    {
        /// <summary>
        /// Checks if the item is closed
        /// </summary>
        bool IsClosed { get; }
        /// <summary>
        /// Closes the item
        /// </summary>
        void Close();
    }
}

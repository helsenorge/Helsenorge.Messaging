/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Provides an interface around the messages we send and receive
    /// </summary>
    public interface IMessagingMessage : IDisposable
    {
        /// <summary>
        /// The Her id of the communication party that sent the message
        /// </summary>
        int FromHerId { get; set; }
        /// <summary>
        /// The Her id of the communication party that receives the message
        /// </summary>
        int ToHerId { get; set; }
        /// <summary>
        ///The time when the application sent the message
        /// </summary>
        DateTime ApplicationTimestamp { get; set; }
        /// <summary>
        /// The id og the Collaboration Protocol Agreement that was in use when sending the message
        /// </summary>
        string CpaId { get; set; }
        /// <summary>
        /// Gets the time the message was sent on the transport plaform
        /// </summary>
        DateTime EnqueuedTimeUtc { get; }
        /// <summary>
        /// Gets the time when the message expires
        /// </summary>
        DateTime ExpiresAtUtc { get; }
        /// <summary>
        /// Gets additional properties related to the message
        /// </summary>
        IDictionary<string, object> Properties { get; }
        /// <summary>
        /// Gets the size of the message
        /// </summary>
        long Size { get; }
        /// <summary>
        /// Gets or sets the type of content the message contains
        /// </summary>
        string ContentType { get; set; }
        /// <summary>
        /// Gets or sets a correlation id
        /// </summary>
        string CorrelationId { get; set; }
        /// <summary>
        /// Gets or sets the message function
        /// </summary>
        string MessageFunction { get; set; }
        /// <summary>
        /// Gets or sets the message id
        /// </summary>
        string MessageId { get; set; }
        /// <summary>
        /// Gets or sets the GroupId
        /// </summary>
        string GroupId { get; set; }
        /// <summary>
        /// Gets or sets the path where replies are sent
        /// </summary>
        string ReplyTo { get; set; }
        /// <summary>
        /// Gets or sets the time when the message should be made available on the transport layer
        /// </summary>
        DateTime ScheduledEnqueueTimeUtc { get; set; }
        /// <summary>
        /// Gets a value indicating how long this message will be locked on the server
        /// </summary>
        DateTime LockedUntil { get;  }
        /// <summary>
        /// The amount of time this message should live
        /// </summary>
        TimeSpan TimeToLive { get; set; }
        /// <summary>
        /// The path where this item is sent
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "To")]
        string To { get; set; }
        /// <summary>
        /// Completes processing of this message
        /// </summary>
        void Complete();
        /// <summary>
        /// Completes processing of this message
        /// </summary>
        Task CompleteAsync();
        /// <summary>
        /// Released the message
        /// </summary>
        void Release();
        /// <summary>
        /// Released the message
        /// </summary>
        Task RelaseAsync();
        /// <summary>
        /// Rejects the message
        /// </summary>
        void Reject();
        /// <summary>
        /// Rejects the message
        /// </summary>
        Task RejectAsync();
        /// <summary>
        /// Modifies a message. It sends a modified outcome to the peer.
        /// </summary>
        /// <param name="deliveryFailed">If set, the message's delivery-count is incremented.</param>
        /// <param name="undeliverableHere">Indicates if the message should not be redelivered to this endpoint.</param>
        void Modify(bool deliveryFailed, bool undeliverableHere = false);
        /// <summary>
        /// Modifies a message. It sends a modified outcome to the peer.
        /// </summary>
        /// <param name="deliveryFailed">If set, the message's delivery-count is incremented.</param>
        /// <param name="undeliverableHere">Indicates if the message should not be redelivered to this endpoint.</param>
        Task ModifyAsync(bool deliveryFailed, bool undeliverableHere = false);
        /// <summary>
        /// Creates a clone of the message
        /// </summary>
        /// <returns></returns>
        IMessagingMessage Clone(bool includePayload = true);
        /// <summary>
        /// Gets a reference to the object that is implementation specific
        /// </summary>
        object OriginalObject { get; }
        /// <summary>
        /// Gets the body of the message
        /// </summary>
        /// <returns></returns>
        Stream GetBody();
        /// <summary>
        /// Adds relevant information to the Data dictionary of an exception
        /// </summary>
        /// <param name="ex"></param>
        void AddDetailsToException(Exception ex);
        /// <summary>
        /// Sends this message to the deadletter queue
        /// </summary>
        void DeadLetter();
        /// <summary>
        /// Sends this message to the deadletter queue
        /// </summary>
        Task DeadLetterAsync();
        /// <summary>
        /// Gets the number of deliveries.
        /// </summary>
        int DeliveryCount { get; }
        /// <summary>
        /// Set additional properties related to the message
        /// </summary>
        void SetApplicationProperty(string key, string value);
        /// <summary>
        /// Set additional properties related to the message
        /// </summary>
        void SetApplicationProperty(string key, DateTime value);
        /// <summary>
        /// Set additional properties related to the message
        /// </summary>
        void SetApplicationProperty(string key, int value);

    }
}

﻿/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Registries.Abstractions;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Represents information about an incoming message. This is presented to the application layer for further processing
    /// </summary>
    public class IncomingMessage
    {
        /// <summary>
        /// The XML payload
        /// </summary>
        public XDocument Payload { get; set; }
        /// <summary>
        /// The message function being used
        /// </summary>
        public string MessageFunction { get; set; }
        /// <summary>
        /// The Her id that sent the message
        /// </summary>
        public int FromHerId { get; set; }
        /// <summary>
        /// The Her id that received the message
        /// </summary>
        public int ToHerId { get; set; }
        /// <summary>
        /// The CPA agreement that is in use when processing the message
        /// </summary>
        public CollaborationProtocolProfile CollaborationAgreement { get; set; }
        /// <summary>
        /// The id of the message
        /// </summary>
        public string MessageId { get; set; }
        /// <summary>
        /// The correlation id of the message in cases where this is related to some other message
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// The time the message was added to the transport layer
        /// </summary>
        public DateTime EnqueuedTimeUtc { get; set; }
        /// <summary>
        /// Indication on how decryption succeeded. Allows application layer to take appropriate action.
        /// </summary>
        public CertificateErrors DecryptionError { get; set; }
        /// <summary>
        /// Status indication of the legacy decryption certificate. Allows application layer to take appropriate action.
        /// </summary>
        public CertificateErrors LegacyDecryptionError { get; set; }
        /// <summary>
        /// Indication of how signature validation succeeded. Allows application layer to take appropriate action.
        /// </summary>
        public CertificateErrors SignatureError { get; set; }
        /// <summary>
        /// Indication of whether the content was signed or not.
        /// </summary>
        public bool ContentWasSigned { get; set; }
        /// <summary>
        /// Renews the lock of the message
        /// </summary>
        public Action RenewLock { get; internal set; }
        /// <summary>
        /// Renews the lock of the message
        /// </summary>
        public Func<Task> RenewLockAsync { get; internal set; }
        /// <summary>
        /// Removes the message from the queue.
        /// </summary>
        public Action Complete { get; internal set; }
        /// <summary>
        /// Removes the message from the queue.
        /// </summary>
        public Func<Task> CompleteAsync { get; internal set; }
        /// <summary>
        /// Releases the message.
        /// </summary>
        public Action Release { get; internal set; }
        /// <summary>
        /// Releases the message.
        /// </summary>
        public Func<Task> ReleaseAsync { get; internal set; }
        /// <summary>
        /// Deadletters the message.
        /// </summary>
        public Action Deadletter { get; internal set; }
        /// <summary>
        /// Deadletters the message.
        /// </summary>
        public Func<Task> DeadletterAsync { get; internal set; }
        /// <summary>
        /// Returns true if this is the first time the message is aquried.
        /// </summary>
        public bool FirstAquirer { get; internal set; }
        /// <summary>
        /// Gets the number of deliveries.
        /// </summary>
        public int DeliveryCount { get ; internal set; }
        /// <summary>
        /// Gets a value indicating how long this message will be locked on the server
        /// </summary>
        public DateTime LockedUntil { get; internal set; }
    }
}

﻿/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// EventIds used for <see cref="MessagingException"/>.
    /// </summary>
    public static class EventIds
    {
        private const string EventIdName = "MUG";
        /// <summary>
        /// Generic receive error.
        /// </summary>
        public static EventId Receive = new EventId(1, EventIdName);
        /// <summary>
        /// Generic send error.
        /// </summary>
        public static EventId Send = new EventId(2, EventIdName);
        /// <summary>
        /// Unable to determine the name of the queue we should be listening on.
        /// </summary>
        public static EventId QueueNameEmptyEventId = new EventId(3, EventIdName);
        /// <summary>
        /// Cannot find communication details for sender.
        /// </summary>
        public static EventId SenderMissingInAddressRegistryEventId = new EventId(4, EventIdName);
        
        /// <summary>
        /// Generic error with remote certificate.
        /// </summary>
        public static EventId RemoteCertificate = new EventId(10, EventIdName);
        /// <summary>
        /// Remote certificate has invalid start date.
        /// </summary>
        public static EventId RemoteCertificateStartDate = new EventId(11, EventIdName);
        /// <summary>
        /// Remote certificate has invalid end date.
        /// </summary>
        public static EventId RemoteCertificateEndDate = new EventId(12, EventIdName);
        /// <summary>
        /// Remote certificate has been revoked.
        /// </summary>
        public static EventId RemoteCertificateRevocation = new EventId(13, EventIdName);
        /// <summary>
        /// Remote certificate has invalid usage.
        /// </summary>
        public static EventId RemoteCertificateUsage = new EventId(14, EventIdName);

        /// <summary>
        /// Generic error with local certificate.
        /// </summary>
        public static EventId LocalCertificate = new EventId(15, EventIdName);
        /// <summary>
        /// Local certificate has invalid start date.
        /// </summary>
        public static EventId LocalCertificateStartDate = new EventId(16, EventIdName);
        /// <summary>
        /// Local certificate has invalid end date.
        /// </summary>
        public static EventId LocalCertificateEndDate = new EventId(17, EventIdName);
        /// <summary>
        /// Local certificate has been revoked.
        /// </summary>
        public static EventId LocalCertificateRevocation = new EventId(18, EventIdName);
        /// <summary>
        /// Local certificate has invalid usage.
        /// </summary>
        public static EventId LocalCertificateUsage = new EventId(19, EventIdName);

        /// <summary>
        /// Received message is not XML.
        /// </summary>
        public static EventId NotXml = new EventId(20, EventIdName);
        /// <summary>
        /// Missing fields from header.
        /// </summary>
        public static EventId MissingField = new EventId(21, EventIdName);
        /// <summary>
        /// Information in header does not match information in payload.
        /// </summary>
        public static EventId DataMismatch = new EventId(22, EventIdName);
        /// <summary>
        /// Error reported by application layer.
        /// </summary>
        public static EventId ApplicationReported = new EventId(23, EventIdName);
        /// <summary>
        /// A synchronous call timed out.
        /// </summary>
        public static EventId SynchronousCallTimeout = new EventId(30, EventIdName);
        /// <summary>
        /// We received a reply to a synchronous call after it timed out.
        /// </summary>
        public static EventId SynchronousCallDelayed = new EventId(31, EventIdName);

        /// <summary>
        /// Tried to send a message with a function that is invalid.
        /// </summary>
        public static EventId InvalidMessageFunction = new EventId(33, EventIdName);
        /// <summary>
        /// We received an error on our error queue.
        /// </summary>
        public static EventId ExternalReportedError = new EventId(34, EventIdName);
        /// <summary>
        /// An unknwown error has occured.
        /// </summary>
        public static EventId UnknownError = new EventId(35, EventIdName);
        /// <summary>
        /// The Messaging Entity Cache failed to close an entity.
        /// </summary>
        public static EventId MessagingEntityCacheFailedToCloseEntity = new EventId(36, EventIdName);
        /// <summary>
        /// Non-successful release of message.
        /// </summary>
        public static EventId MessageReleaseFailed = new EventId(37, EventIdName);
        /// <summary>
        /// Non-successful authentication or connection attempt to one ore more of the web services on start-up.
        /// </summary>
        public static EventId ConnectionToWebServiceFailed = new EventId(38, EventIdName);
        /// <summary>
        /// Non-successful authentication or connection attempt to the message broker.
        /// </summary>
        public static EventId ConnectionToMessageBrokerFailed = new EventId(39, EventIdName);

        /// <summary>
        /// Event Id used for informational purposes when starting/ending the Receive process.
        /// </summary>
        public static EventId ServiceBusReceive = new EventId(1001, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes when starting/ending the Send process.
        /// </summary>
        public static EventId ServiceBusSend = new EventId(1002, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes when removing the message from queue.
        /// </summary>
        public static EventId RemoveMessageFromQueue = new EventId(1003, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes when creating, updating, trimming, 
        /// releasing or closing an cached entity in the Messaging Entity Cache.
        /// </summary>
        public static EventId MessagingEntityCacheProcessor = new EventId(1004, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes before and after we notify a third-party 
        /// through the Helsenorge.Messaging notification handler.
        /// </summary>
        public static EventId NotificationHandler = new EventId(1005, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes before and after we validate the certificates.
        /// </summary>
        public static EventId CertificateValidation = new EventId(1006, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes before and after we encrypt the payload.
        /// </summary>
        public static EventId EncryptPayload = new EventId(1007, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes when retrieving an IMessagingFactory 
        /// from the pool and creating an empty message.
        /// </summary>
        public static EventId FactoryPoolCreateEmptyMessage = new EventId(1008, EventIdName);
        /// <summary>
        /// Event Id used for informational purposes when a retry operation is in progress.
        /// </summary>
        public static EventId RetryOperation = new EventId(1009, EventIdName);
    }

    /// <summary>
    /// Generic exception for Service bus related errors
    /// </summary>
    [Serializable]
    [ExcludeFromCodeCoverage] // default set of constructurs not used in our code
    public class MessagingException : Exception
    {
        /// <summary>
        /// The event id to use when logging this exception
        /// </summary>
        public EventId EventId { get; set; }
        /// <summary>
        /// COnstructor
        /// </summary>
        public MessagingException() : base()
        {
            
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public MessagingException(string message) : base(message){}
        /// <summary>
        /// Constructor
        /// </summary>
        public MessagingException(string message, Exception inner) : base(message, inner){}
        /// <summary>
        /// Constructor
        /// </summary>
        protected MessagingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context){}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        // ReSharper disable once RedundantOverridenMember
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // code analysis trigger if this is not present
            base.GetObjectData(info, context);
        }
    }
}

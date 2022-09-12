/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// Provides a number of functions used by both the Gateway (sending) and message listeners
    /// </summary>
    public class ServiceBusCore
    {
        /// <summary>
        /// Label used when error message contains a SOAP fault
        /// </summary>
        public const string SoapFaultLabel = "AMQP_SOAP_FAULT";
        /// <summary>
        /// Header key for FromHerId
        /// </summary>
        public const string FromHerIdHeaderKey = "fromHerId";
        /// <summary>
        /// Header key for ToHerId
        /// </summary>
        public const string ToHerIdHeaderKey = "toHerId";
        /// <summary>
        /// Header key for CpaId
        /// </summary>
        public const string CpaIdHeaderKey = "cpaId";
        /// <summary>
        /// Header key for ApplicationTimestamp
        /// </summary>
        public const string ApplicationTimestampHeaderKey = "applicationTimeStamp";

        private const string OriginalMessageIdHeaderKey = "originalMessageId";
        private const string ReceiverTimestampHeaderKey = "receiverTimeStamp";
        private const string ErrorConditionHeaderKey = "errorCondition";
        private const string ErrorDescriptionHeaderKey = "errorDescription";
        private const string ErrorConditionDataHeaderKey = "errorConditionData";

        private string _hostnameAndPath;

        //convencience properties
        internal ServiceBusSettings Settings => Core.Settings.ServiceBus;
        internal IAddressRegistry AddressRegistry => Core.AddressRegistry;
        internal ICollaborationProtocolRegistry CollaborationProtocolRegistry => Core.CollaborationProtocolRegistry;
        internal ICertificateValidator CertificateValidator => Core.CertificateValidator;
        internal IMessageProtection MessageProtection => Core.MessageProtection;
        internal bool LogPayload => Core.Settings.LogPayload;
        internal ICertificateStore CertificateStore => Core.CertificateStore;

        internal string HostnameAndPath
        {
            get
            {
                if (_hostnameAndPath == null)
                {
                    var connectionString = Core?.Settings?.ServiceBus?.ConnectionString ?? string.Empty;
                    var startIndex = connectionString.IndexOf("@", StringComparison.InvariantCulture);
                    if (startIndex > -1)
                        _hostnameAndPath = connectionString.Substring(startIndex + 1);
                }

                return _hostnameAndPath;
            }
        }

        /// <summary>
        /// Reference to the core messaging system
        /// </summary>
        private MessagingCore Core { get; }

        internal IServiceBusFactoryPool FactoryPool { get; }
        internal ServiceBusSenderPool SenderPool { get; }
        internal ServiceBusReceiverPool ReceiverPool { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core">A reference to the core messaging system</param>
        /// <exception cref="ArgumentNullException"></exception>
        internal ServiceBusCore(MessagingCore core)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            
            var connectionString = core.Settings.ServiceBus.ConnectionString;
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }
            else
            {
                FactoryPool = new ServiceBusFactoryPool(core.Settings.ServiceBus, core.Settings.ApplicationProperties);
            }
            
            SenderPool = new ServiceBusSenderPool(core.Settings.ServiceBus, FactoryPool);
            ReceiverPool = new ServiceBusReceiverPool(core.Settings.ServiceBus, FactoryPool);
        }

        /// <summary>
        /// Sends an outgoing message
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="outgoingMessage">Information about the message to send</param>
        /// <param name="queueType">The type of queue that should be used</param>
        /// <param name="replyTo">An optional ReplyTo queue that should be used. Only relevant in synchronous messaging</param>
        /// <param name="correlationId">The correlation id to use when sending the message. Only relevant in synchronous messaging</param>
        /// <returns></returns>
        internal async Task Send(ILogger logger, OutgoingMessage outgoingMessage, QueueType queueType, string replyTo = null, string correlationId = null)
        {
            if (outgoingMessage == null) throw new ArgumentNullException(nameof(outgoingMessage));
            if (string.IsNullOrEmpty(outgoingMessage.MessageId)) throw new ArgumentNullException(nameof(outgoingMessage.MessageId));
            if (outgoingMessage.Payload == null) throw new ArgumentNullException(nameof(outgoingMessage.Payload));

            // when we are replying to a synchronous message, we need to use the replyto of the original message
            var queueName =
                (queueType == QueueType.SynchronousReply) ?
                    replyTo :
                    await ConstructQueueName(logger, outgoingMessage.ToHerId, queueType).ConfigureAwait(false);

            logger.LogStartSend(queueType, outgoingMessage.MessageFunction, Core.Settings.MyHerId, outgoingMessage.ToHerId, outgoingMessage.MessageId, $"Sending message using host and queue: '{HostnameAndPath}/{queueName}'", outgoingMessage.Payload);

            var hasAgreement = true;
            // first we try and find an agreement
            var profile = await CollaborationProtocolRegistry.FindAgreementForCounterpartyAsync(logger, outgoingMessage.ToHerId).ConfigureAwait(false);
            if (profile == null)
            {
                hasAgreement = false; // if we don't have an agreement, we try to find the specific profile
                profile = await CollaborationProtocolRegistry.FindProtocolForCounterpartyAsync(logger, outgoingMessage.ToHerId).ConfigureAwait(false);
            }

            var stopwatch = new Stopwatch();
            var contentType = Core.MessageProtection.ContentType;
            if (contentType.Equals(ContentType.SignedAndEnveloped, StringComparison.OrdinalIgnoreCase))
            {
                var validator = Core.CertificateValidator;
                logger.LogBeforeValidatingCertificate(outgoingMessage.MessageFunction, profile.EncryptionCertificate.Thumbprint, profile.EncryptionCertificate.Subject, "KeyEncipherment", outgoingMessage.ToHerId, outgoingMessage.MessageId);
                stopwatch.Start();
                // Validate external part's encryption certificate
                var encryptionStatus = validator == null
                    ? CertificateErrors.None
                    : validator.Validate(profile.EncryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
                stopwatch.Stop();
                logger.LogAfterValidatingCertificate(outgoingMessage.MessageFunction, profile.EncryptionCertificate.Thumbprint, "KeyEncipherment", outgoingMessage.ToHerId, outgoingMessage.MessageId, stopwatch.ElapsedMilliseconds.ToString());

                logger.LogBeforeValidatingCertificate(outgoingMessage.MessageFunction, Core.MessageProtection.SigningCertificate.Thumbprint, Core.MessageProtection.SigningCertificate.Subject, "NonRepudiation", Core.Settings.MyHerId, outgoingMessage.MessageId);
                stopwatch.Restart();
                // Validate "our" own signature certificate
                var signatureStatus = validator == null
                    ? CertificateErrors.None
                    : validator.Validate(Core.MessageProtection.SigningCertificate, X509KeyUsageFlags.NonRepudiation);
                stopwatch.Stop();
                logger.LogAfterValidatingCertificate(outgoingMessage.MessageFunction, Core.MessageProtection.SigningCertificate.Thumbprint, "NonRepudiation", Core.Settings.MyHerId, outgoingMessage.MessageId, stopwatch.ElapsedMilliseconds.ToString());

                // this is the other parties certificate that may be out of date, not something we can fix
                if (encryptionStatus != CertificateErrors.None)
                {
                    if (Core.Settings.IgnoreCertificateErrorOnSend)
                    {
                        logger.LogError(EventIds.RemoteCertificate, $"Remote encryption certificate {profile.EncryptionCertificate?.SerialNumber} for {outgoingMessage.ToHerId.ToString()} is not valid.{Environment.NewLine}" +
                            $"Certificate error(s): {encryptionStatus}.");
                    }
                    else
                    {
                        throw new MessagingException($"Remote encryption certificate {profile.EncryptionCertificate?.SerialNumber} for {outgoingMessage.ToHerId.ToString()} is not valid.{Environment.NewLine}" +
                            $"Certificate error(s): {encryptionStatus}.")
                        {
                            EventId = EventIds.RemoteCertificate
                        };
                    }
                }
                // this is our certificate, something we can fix 
                if (signatureStatus != CertificateErrors.None)
                {
                    if (Core.Settings.IgnoreCertificateErrorOnSend)
                    {
                        logger.LogError(EventIds.LocalCertificate, $"Locally installed signing certificate {Core.MessageProtection.SigningCertificate?.SerialNumber} is not valid.{Environment.NewLine}" +
                            $"Serial Number: {Core.MessageProtection.SigningCertificate?.SerialNumber}{Environment.NewLine}" +
                            $"Thumbprint: {Core.MessageProtection.SigningCertificate?.Thumbprint}.{Environment.NewLine}" +
                            $"Certificate error(s): {signatureStatus}.");
                    }
                    else
                    {
                        throw new MessagingException($"Locally installed signing certificate {Core.MessageProtection.SigningCertificate?.SerialNumber} is not valid.{Environment.NewLine}" +
                            $"Serial Number: {Core.MessageProtection.SigningCertificate?.SerialNumber}{Environment.NewLine}" +
                            $"Thumbprint: {Core.MessageProtection.SigningCertificate?.Thumbprint}{Environment.NewLine}" +
                            $"Certificate error(s): {signatureStatus}.")
                        {
                            EventId = EventIds.LocalCertificate
                        };
                    }
                }
            }
            logger.LogBeforeEncryptingPayload(outgoingMessage.MessageFunction, Core.MessageProtection.SigningCertificate.Thumbprint, profile?.EncryptionCertificate.Thumbprint, Core.Settings.MyHerId, outgoingMessage.ToHerId, outgoingMessage.MessageId);
            stopwatch.Restart();
            // Encrypt the payload
            var stream = Core.MessageProtection.Protect(outgoingMessage.Payload?.ToStream(), profile?.EncryptionCertificate);
            stopwatch.Stop();
            logger.LogAfterEncryptingPayload(outgoingMessage.MessageFunction, Core.Settings.MyHerId, outgoingMessage.ToHerId, outgoingMessage.MessageId, stopwatch.ElapsedMilliseconds.ToString());

            logger.LogBeforeFactoryPoolCreateMessage(outgoingMessage.MessageFunction, Core.Settings.MyHerId, outgoingMessage.ToHerId, outgoingMessage.MessageId);
            // Create an empty message
            var messagingMessage = await FactoryPool.CreateMessage(logger, stream).ConfigureAwait(false);
            logger.LogAfterFactoryPoolCreateMessage(outgoingMessage.MessageFunction, Core.Settings.MyHerId, outgoingMessage.ToHerId, outgoingMessage.MessageId);

            if (queueType != QueueType.SynchronousReply)
                messagingMessage.ReplyTo = queueName;
            messagingMessage.ContentType = Core.MessageProtection.ContentType;
            messagingMessage.MessageId = outgoingMessage.MessageId;
            messagingMessage.To = queueName;
            messagingMessage.MessageFunction = outgoingMessage.MessageFunction;
            messagingMessage.CorrelationId = correlationId ?? outgoingMessage.MessageId;
            messagingMessage.TimeToLive = (queueType == QueueType.Asynchronous)
                ? Settings.Asynchronous.TimeToLive
                : Settings.Synchronous.TimeToLive;
            messagingMessage.ScheduledEnqueueTimeUtc = outgoingMessage.ScheduledSendTimeUtc;
            messagingMessage.FromHerId = Core.Settings.MyHerId;
            messagingMessage.ToHerId = outgoingMessage.ToHerId;
            messagingMessage.ApplicationTimestamp = DateTime.Now;

            if (hasAgreement)
            {
                messagingMessage.CpaId = profile.CpaId.ToString("D");
            }
            await Send(logger, messagingMessage).ConfigureAwait(false);

            logger.LogEndSend(queueType, messagingMessage.MessageFunction, messagingMessage.FromHerId, messagingMessage.ToHerId, messagingMessage.MessageId);
        }

        /// <summary>
        /// Sends a prepared message 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message">The prepared message</param>
        /// <returns></returns>
        private async Task Send(ILogger logger, IMessagingMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            IMessagingSender messageSender = null;

            try
            {
                messageSender = await SenderPool.CreateCachedMessageSender(logger, message.To).ConfigureAwait(false);
                await messageSender.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogException("An error occurred during Send operation.", ex);

                throw new MessagingException(ex.Message)
                {
                    EventId = EventIds.Send
                };
            }
            finally
            {
                if (messageSender != null)
                {
                    await SenderPool.ReleaseCachedMessageSender(logger, message.To).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sends an error message
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="originalMessage">The original message the error is in response to</param>
        /// <param name="errorCode">The error code to report</param>
        /// <param name="errorDescription">The error description to report</param>
        /// <param name="additionalData">Additional information to include</param>
        /// <returns></returns>
        private async Task SendError(ILogger logger, IMessagingMessage originalMessage, string errorCode, string errorDescription, IEnumerable<string> additionalData) //TODO: Sjekk at SendError fungerer med Http-meldinger
        {            
            if (originalMessage == null) throw new ArgumentNullException(nameof(originalMessage));
            if (string.IsNullOrEmpty(errorCode)) throw new ArgumentNullException(nameof(errorCode));
            if (string.IsNullOrEmpty(errorDescription)) throw new ArgumentNullException(nameof(errorDescription));

            if (originalMessage.FromHerId <= 0)
            {
                logger.LogWarning(EventIds.MissingField, "FromHerId is missing. No idea where to send the error");
                return;
            }
            
            // Clones original message, but leaves out the payload
            var clonedMessage = originalMessage.Clone(false);
            // update some properties on the cloned message
            clonedMessage.To = await ConstructQueueName(logger, originalMessage.FromHerId, QueueType.Error).ConfigureAwait(false); // change target 
            clonedMessage.TimeToLive = Settings.Error.TimeToLive;
            clonedMessage.FromHerId = originalMessage.ToHerId;
            clonedMessage.ToHerId = originalMessage.FromHerId;
            
            if (clonedMessage.Properties.ContainsKey(OriginalMessageIdHeaderKey) == false)
            {
                clonedMessage.SetApplicationProperty(OriginalMessageIdHeaderKey, originalMessage.MessageId);
            }
            if (clonedMessage.Properties.ContainsKey(ReceiverTimestampHeaderKey) == false)
            {
                clonedMessage.SetApplicationProperty(ReceiverTimestampHeaderKey, DateTime.Now.ToString(DateTimeFormatInfo.InvariantInfo));
            }
            if (clonedMessage.Properties.ContainsKey(ErrorConditionHeaderKey) == false)
            {
                clonedMessage.SetApplicationProperty(ErrorConditionHeaderKey, errorCode);
            }
            if (clonedMessage.Properties.ContainsKey(ErrorDescriptionHeaderKey) == false)
            {
                clonedMessage.SetApplicationProperty(ErrorDescriptionHeaderKey, errorDescription);
            }

            var additionDataValue = "None";
            if (additionalData != null)
            {
                var sb = new StringBuilder();

                foreach (var item in additionalData)
                {
                    if (string.IsNullOrEmpty(item) == false)
                    {
                        sb.Append($"{item};");
                    }
                }
                additionDataValue = sb.ToString();

                if (clonedMessage.Properties.ContainsKey(ErrorConditionDataHeaderKey) == false)
                {
                    clonedMessage.SetApplicationProperty(ErrorConditionDataHeaderKey, additionDataValue);
                }
            }
            logger.LogWarning("Reporting error to sender. FromHerId: {0} ToHerId: {1} ErrorCode: {2} ErrorDescription: {3} AdditionalData: {4}",  originalMessage.FromHerId, originalMessage.ToHerId, errorCode, errorDescription, additionDataValue);
            await Send(logger, clonedMessage).ConfigureAwait(false);
        }
        /// <summary>
        /// Gets the queue name that we can use on messages from a more extensive name
        /// </summary>
        /// <param name="queueAddress">The full name</param>
        /// <returns>The short name</returns>
        internal string ExtractQueueName(string queueAddress)
        {
            // the information stored in the address service includes the full address for the service
            // sb.test.nhn.no/DigitalDialog/91468_async
            // we only want the last part

            if (string.IsNullOrEmpty(queueAddress)) throw new ArgumentNullException(nameof(queueAddress), $"Queue address null or empty string. Verify that the Communication Party is set up with a queue address in the Address Registry. Parameter name: {nameof(queueAddress)}");

            var i = queueAddress.LastIndexOf('/');
            return queueAddress.Substring(i + 1);
        }

        private async Task<string> ConstructQueueName(ILogger logger, int herId, QueueType type)
        {
            var details = await Core.AddressRegistry.FindCommunicationPartyDetailsAsync(logger, herId).ConfigureAwait(false);
            if (details == null)
            {
                throw new MessagingException("Could not find sender in address registry")
                {
                    EventId = EventIds.SenderMissingInAddressRegistryEventId
                };
            }

            return type switch
            {
                QueueType.Asynchronous => ExtractQueueName(details.AsynchronousQueueName),
                QueueType.Synchronous => ExtractQueueName(details.SynchronousQueueName),
                QueueType.Error => ExtractQueueName(details.ErrorQueueName),
                _ => throw new InvalidOperationException($"Queue type '{type}' is not supported"),
            };
        }

        /// <summary>
        /// Sends a message to the remote sender with information about what is wrong.
        /// Loggs information to our logs.
        /// Removes message from processing queue since there is no point in processing it again.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id">The event id that error should be logged with</param>
        /// <param name="originalMessage"></param>
        /// <param name="errorCode"></param>
        /// <param name="description"></param>
        /// <param name="additionalData"></param>
        /// <param name="ex"></param>
        internal async Task ReportErrorToExternalSender(
            ILogger logger,
            EventId id,
            IMessagingMessage originalMessage,
            string errorCode,
            string description,
            IEnumerable<string> additionalData,
            Exception ex = null)
        {
            logger.LogWarning(id, ex, description);
            await SendError(logger, originalMessage, errorCode, description, additionalData).ConfigureAwait(false);
            RemoveMessageFromQueueAfterError(logger, originalMessage);
        }

        /// <summary>
        /// Removes the message from the queue as part of an error. 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        internal static void RemoveMessageFromQueueAfterError(ILogger logger, IMessagingMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            logger.LogRemoveMessageFromQueueError(message.MessageId);
            message.Complete();
        }

        /// <summary>
        /// Removes the message from the queue as part of normal operation
        /// </summary>
        /// <param name="message"></param>
        internal static void RemoveProcessedMessageFromQueue(IMessagingMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.Complete();
        }
        /// <summary>
        /// Registers an alternate messaging factory
        /// </summary>
        /// <param name="factory"></param>
        public void RegisterAlternateMessagingFactory(IMessagingFactory factory) => FactoryPool.RegisterAlternateMessagingFactory(factory);
    }
}

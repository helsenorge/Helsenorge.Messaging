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
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    /// <summary>
    /// Base class for all listeners, not just the ones processing xml messages
    /// </summary>
    public abstract class MessageListener
    {
        private CommunicationPartyDetails _myDetails;
        private IMessagingReceiver _messageReceiver;
        private bool _listenerEstablishedConfirmed = false;

        public Action<string> SetCorrelationIdAction { get; set; }

        /// <summary>
        /// The timeout used for reading messages from queue
        /// </summary>
        protected TimeSpan ReadTimeout { get; set; }
        /// <summary>
        /// Specifies the name of the queue this listener is reading from
        /// </summary>
        protected abstract string QueueName { get; }
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected abstract QueueType QueueType { get; }
        /// <summary>
        /// Reference to service bus related setting
        /// </summary>
        protected ServiceBusCore Core { get; }

        /// <summary>
        /// Gets a reference to the server
        /// </summary>
        protected IMessagingNotification MessagingNotification { get; }
        /// <summary>
        /// Gets the logger used for diagnostics purposes
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core">Reference to core service bus infrastructure</param>
        /// <param name="logger">Logger used for diagnostics information</param>
        /// <param name="messagingNotification">A reference to the messaging notification system</param>
        protected MessageListener(
            ServiceBusCore core,
            ILogger logger,
            IMessagingNotification messagingNotification)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Core = core ?? throw new ArgumentNullException(nameof(core));
            MessagingNotification = messagingNotification;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected abstract Task NotifyMessageProcessingStarted(MessageListener listener, IncomingMessage message);
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected abstract Task NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message);
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected abstract Task NotifyMessageProcessingCompleted(IncomingMessage message);
        /// <summary>
        /// Starts the listener
        /// </summary>
        /// <param name="cancellation">Cancellation token that signals when we should stop</param>
        public async Task Start(CancellationToken cancellation)
        {
            Logger.LogInformation("Starting listener on queue {0}", QueueName);

            while (cancellation.IsCancellationRequested == false)
            {
                try
                {
                    await ReadAndProcessMessage().ConfigureAwait(false);

                    if (!_listenerEstablishedConfirmed)
                    {
                        Logger.LogInformation("Listener established on queue {0}", QueueName);
                        _listenerEstablishedConfirmed = true;
                    }
                }
                catch (Exception ex) // protect the main message pump
                {
                    Logger.LogException("Generic service bus error", ex);
                    // if there are problems with the message bus, we don't get interval of the ReadTimeout
                    // pause a bit so that we don't take over the whole system
                    await Task.Delay(5000, cancellation).ConfigureAwait(false);
                }
            }
        }
        /// <summary>
        /// Reads and process a message from the queue
        /// If message processing doesn't fail, the message is removed from the queue before result is returned. 
        /// For sync messages, leaving the message on the queue will block further processing. Not good. 
        /// For async messages, the notification system handles processing before the message is actually removed.
        /// <param name="alwaysRemoveMessage">Used for synchronous handling to force the removal on exceptions</param>
        /// </summary>
        public async Task<IncomingMessage> ReadAndProcessMessage(bool alwaysRemoveMessage = false)
        {
            await SetUpReceiver().ConfigureAwait(false);
            IMessagingMessage message;
            try
            {
                message = await _messageReceiver.ReceiveAsync(ReadTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new MessagingException("Reading message from service bus", ex)
                {
                    EventId = EventIds.Receive
                };
            }
            return await HandleRawMessage(message, alwaysRemoveMessage).ConfigureAwait(false);
        }
        private async Task<IncomingMessage> HandleRawMessage(IMessagingMessage message, bool alwaysRemoveMessage)
        {
            if (message == null) return null;
            Stream bodyStream = null;

            try
            {
                if(message.LockedUntil.ToUniversalTime() <= DateTime.UtcNow)
                {
                    Logger.LogInformation($"MessageListener::ReadAndProcessMessage - Ignoring message, lock expired at: {message.LockedUntil.ToUniversalTime()}");
                    return null;
                }

                var incomingMessage = new IncomingMessage()
                {
                    MessageFunction = message.MessageFunction,
                    FromHerId = message.FromHerId,
                    ToHerId = message.ToHerId,
                    MessageId = message.MessageId,
                    CorrelationId = message.CorrelationId,
                    EnqueuedTimeUtc = message.EnqueuedTimeUtc,
                    RenewLock = message.RenewLock,
                    Complete = message.Complete,
                    DeliveryCount = message.DeliveryCount,
                    LockedUntil = message.LockedUntil,
                };
                await NotifyMessageProcessingStarted(this, incomingMessage).ConfigureAwait(false);
                
                SetCorrelationIdAction?.Invoke(incomingMessage.MessageId);
                Logger.LogStartReceive(QueueType, incomingMessage);

                // we cannot dispose of the stream before we have potentially cloned the message for error use
                bodyStream = message.GetBody();

                ValidateMessageHeader(message);
                // we need the certificates for decryption and certificate use
                incomingMessage.CollaborationAgreement = await ResolveProfile(message).ConfigureAwait(false);

                var payload = HandlePayload(message, bodyStream, message.ContentType, incomingMessage, out bool contentWasSigned);
                incomingMessage.ContentWasSigned = contentWasSigned;
                if (payload != null)
                {
                    if (Core.LogPayload)
                    {
                        Logger.LogDebug(payload.ToString());
                    }
                    incomingMessage.Payload = payload;
                }
                await NotifyMessageProcessingReady(message, incomingMessage).ConfigureAwait(false);
                ServiceBusCore.RemoveProcessedMessageFromQueue(message);
                Logger.LogRemoveMessageFromQueueNormal(message, QueueName);
                await NotifyMessageProcessingCompleted(incomingMessage).ConfigureAwait(false);
                Logger.LogEndReceive(QueueType, incomingMessage);
                return incomingMessage;
            }
            catch (CertificateException ex)
            {
                Logger.LogWarning($"{ex.Description}. MessageFunction: {message.MessageFunction} " +
                  $"FromHerId: {message.FromHerId} ToHerId: {message.ToHerId} CpaId: {message.CpaId} " +
                  $"CorrelationId: {message.CorrelationId} Certificate thumbprint: {ex.AdditionalInformation}");

                Core.ReportErrorToExternalSender(Logger, ex.EventId, message, ex.ErrorCode, ex.Description, ex.AdditionalInformation);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (SecurityException ex)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.RemoteCertificate, message, "transport:invalid-certificate", ex.Message, null, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (HeaderValidationException ex)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.MissingField, message, "transport:invalid-field-value", ex.Message, ex.Fields);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (XmlSchemaValidationException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.NotXml, message, "transport:not-well-formed-xml", ex.Message, null, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (ReceivedDataMismatchException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.DataMismatch, message, "transport:invalid-field-value", ex.Message, new[] { ex.ExpectedValue, ex.ReceivedValue }, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (NotifySenderException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.ApplicationReported, message, "transport:internal-error", ex.Message, null, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (SenderHerIdMismatchException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.DataMismatch, message, "abuse:spoofing-attack", ex.Message, null, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (PayloadDeserializationException ex) // from parsing to XML, reportable exception
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.ApplicationReported, message, "transport:not-well-formed-xml", ex.Message, null, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (AggregateException ex) when (ex.InnerException is MessagingException && ((MessagingException)ex.InnerException).EventId.Id == EventIds.Send.Id)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.ApplicationReported, message, "transport:invalid-field-value", "Invalid value in field: 'ReplyTo'", null, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (UnsupportedMessageException ex)  // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.ApplicationReported, message, "transport:unsupported-message", ex.Message, null, ex);
                await MessagingNotification.NotifyHandledException(message, ex).ConfigureAwait(false);
            }
            catch (Exception ex) // unknown error
            {
                message.AddDetailsToException(ex);
                Logger.LogError(EventIds.UnknownError, null, $"Message processing failed. Keeping lock until it times out and we can try again. Message expires at UTC {message.ExpiresAtUtc}");
                Logger.LogException("Unknown error", ex);
                // if something unknown goes wrong, we want to retry the message after a delay
                // we don't call Complete() or Abandon() since that will cause the message to be availble again
                // chances are that the failure may still be around
                // the Defer() method requires us to store the sequence id, but we don't have a place to store it 
                // the option then is to let the lock time-out. This will happen after a couple of minutes and the
                // message becomes available again. 10 retries = 10 timeouts before it gets added to DLQ

                if (alwaysRemoveMessage)
                {
                    ServiceBusCore.RemoveMessageFromQueueAfterError(Logger, message);
                }
                await MessagingNotification.NotifyUnhandledException(message, ex).ConfigureAwait(false);

                // Start a thread which will await until we reach LockedUntilUtc
                // before releasing the message.
                RunMessageReleaseThread(message);
            }
            finally
            {
                bodyStream?.Dispose();
                message.Dispose();
            }
            return null;
        }

        private void RunMessageReleaseThread(IMessagingMessage message)
        {
            var messageReleaseThread = new Thread(MessageReleaseThread.AwaitRelease);
            messageReleaseThread.Start(new MessageReleaseThread.ThreadData { Message = message, Logger = Logger });
        }

        private async Task<CollaborationProtocolProfile> ResolveProfile(IMessagingMessage message)
        {

            // if we receive an error message then CPA isn't needed because we're not decrypting the message and then the CPA info isn't needed
            if (QueueType == QueueType.Error) return null;
            if (Guid.TryParse(message.CpaId, out Guid id) && (id != Guid.Empty))
            {
                return await Core.CollaborationProtocolRegistry.FindAgreementByIdAsync(Logger, id).ConfigureAwait(false);
            }
            return
                // try first to find an agreement
                await Core.CollaborationProtocolRegistry.FindAgreementForCounterpartyAsync(Logger, message.FromHerId).ConfigureAwait(false) ??
                // if we cannot find that, we fallback to protocol (which may return a dummy protocol if things are really missing in AR)
                await Core.CollaborationProtocolRegistry.FindProtocolForCounterpartyAsync(Logger, message.FromHerId).ConfigureAwait(false);
        }

        private XDocument HandlePayload(IMessagingMessage originalMessage, Stream bodyStream, string contentType, IncomingMessage incomingMessage, out bool contentWasSigned)
        {
            XDocument payload;

            if (contentType.Equals(ContentType.Text, StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals(ContentType.Soap, StringComparison.OrdinalIgnoreCase))
            {
                contentWasSigned = false;
                // no certificates to validate
                payload = new NoMessageProtection().Unprotect(bodyStream, null)?.ToXDocument();
            }
            else
            {
                contentWasSigned = true;
                // if we receive enrypted messages on the error queue, we have no idea what to do with them
                // Since this can be message we sent, it's encrypted with their certificate and we don't have that private key
                if (QueueType == QueueType.Error) return null;

                // TODO: The whole part of validating the local certificates below should probably be move into 
                // the IMessageProtection implementation, but since there are some constraints to properties on 
                // IMessagingMessage we'll keep it here for now

                // in receive mode, we try to decrypt and validate content even if the certificates are invalid
                // invalid certificates are flagged to the application layer processing the decrypted message.
                // with the decrypted content, they may have a chance to figure out who sent it

                var validator = Core.CertificateValidator;
                // validate the local encryption certificate and, if present, the local legacy encryption certificate 
                incomingMessage.DecryptionError = validator == null
                    ? CertificateErrors.None
                    : validator.Validate(Core.MessageProtection.EncryptionCertificate, X509KeyUsageFlags.DataEncipherment);
                // in earlier versions of Helsenorge.Messaging we removed the message, but we should rather 
                // want it to be dead lettered since this is a temp issue that should be fixed locally.
                ReportErrorOnLocalCertificate(originalMessage, Core.MessageProtection.EncryptionCertificate, incomingMessage.DecryptionError);
                if(Core.MessageProtection.LegacyEncryptionCertificate != null)
                {
                    // this is optional information that should only be in effect durin a short transition period
                    incomingMessage.LegacyDecryptionError = validator == null
                        ? CertificateErrors.None
                        : validator.Validate(Core.MessageProtection.LegacyEncryptionCertificate, X509KeyUsageFlags.DataEncipherment);
                    // if someone forgets to remove the legacy configuration, we log an error message but don't remove it
                    ReportErrorOnLocalCertificate(originalMessage, Core.MessageProtection.LegacyEncryptionCertificate, incomingMessage.LegacyDecryptionError);
                }
                // validate remote signature certificate
                var signature = incomingMessage.CollaborationAgreement?.SignatureCertificate;
                incomingMessage.SignatureError = validator == null
                    ? CertificateErrors.None
                    : validator.Validate(signature, X509KeyUsageFlags.NonRepudiation);
                ReportErrorOnRemoteCertificate(originalMessage, signature, incomingMessage.SignatureError);

                // decrypt the message and validate the signatureS
                payload = Core.MessageProtection.Unprotect(bodyStream, signature)?.ToXDocument();
            }
            return payload;
        }

        private void ReportErrorOnRemoteCertificate(IMessagingMessage originalMessage, X509Certificate2 certificate,
            CertificateErrors error)
        {
            switch (error)
            {
                case CertificateErrors.None:
                    // no error
                    return;
                case CertificateErrors.Missing:
                    throw new CertificateException(error, "transport:missing-certificate", "Certificate is missing",
                        EventIds.RemoteCertificateStartDate, AdditionalInformation(certificate));
                case CertificateErrors.StartDate:
                    throw new CertificateException(error, "transport:expired-certificate", "Invalid start date", 
                        EventIds.RemoteCertificateStartDate, AdditionalInformation(certificate));
                case CertificateErrors.EndDate:
                    throw new CertificateException(error, "transport:expired-certificate", "Invalid end date",
                        EventIds.RemoteCertificateEndDate, AdditionalInformation(certificate));
                case CertificateErrors.Usage:
                    throw new CertificateException(error, "transport:invalid-certificate", "Invalid usage",
                        EventIds.RemoteCertificateUsage, AdditionalInformation(certificate));
                case CertificateErrors.Revoked:
                    throw new CertificateException(error, "transport:revoked-certificate", "Certificate has been revoked",
                        EventIds.RemoteCertificateRevocation, AdditionalInformation(certificate));
                case CertificateErrors.RevokedUnknown:
                    throw new CertificateException(error, "transport:revoked-certificate", "Unable to determine revocation status",
                        EventIds.RemoteCertificateRevocation, AdditionalInformation(certificate));
                default: // since the value is bitcoded
                    throw new CertificateException(error, "transport:invalid-certificate", "More than one error with certificate",
                        EventIds.RemoteCertificate, AdditionalInformation(certificate));
            }
        }

        private static string[] AdditionalInformation(X509Certificate2 certificate)
        {
            return certificate != null ? new[] {certificate.Subject, certificate.Thumbprint} : new string[] { };
        }

        private void ReportErrorOnLocalCertificate(IMessagingMessage originalMessage, X509Certificate2 certificate, CertificateErrors error)
        {
            string description;
            EventId id;
            switch (error)
            {
                case CertificateErrors.None:
                    return; // no error
                case CertificateErrors.StartDate:
                    description = "Invalid start date";
                    id = EventIds.LocalCertificateStartDate;
                    break;
                case CertificateErrors.EndDate:
                    description = "Invalid end date";
                    id = EventIds.LocalCertificateEndDate;
                    break;
                case CertificateErrors.Usage:
                    description = "Invalid usage";
                    id = EventIds.LocalCertificateUsage;
                    break;
                case CertificateErrors.Revoked:
                    description = "Certificate has been revoked";
                    id = EventIds.LocalCertificateRevocation;
                    break;
                case CertificateErrors.RevokedUnknown:
                    description = "Unable to determine revocation status";
                    id = EventIds.LocalCertificateRevocation;
                    break;
                case CertificateErrors.Missing:
                    description = "Certificate is missing";
                    id = EventIds.LocalCertificate;
                    break;
                default: // since the value is bitcoded
                    description = "More than one error with certificate";
                    id = EventIds.LocalCertificate;
                    break;
            }
            Logger.LogError(id, null, "Description: {Description} Subject: {Subject} Thumbprint: {Thumbprint}",
                description, certificate?.Subject, certificate?.Thumbprint);
        }

        private void ValidateMessageHeader(IMessagingMessage message)
        {
            Logger.LogDebug("Validating message header");
            var missingFields = new List<string>();

            // the FromHerId is not checked by design. If this validation fails, we need to send a message back to the sender
            // but we have no idea who the sender is because the information is missing
            if (message.ToHerId == 0)
            {
                missingFields.Add(ServiceBusCore.ToHerIdHeaderKey);
            }
            if(message.FromHerId == 0)
            {
                missingFields.Add(ServiceBusCore.FromHerIdHeaderKey);
            }
            if (string.IsNullOrEmpty(message.MessageFunction))
            {
                missingFields.Add("Label");
            }
            if (message.ApplicationTimestamp == DateTime.MinValue)
            {
                missingFields.Add(ServiceBusCore.ApplicationTimestampHeaderKey);
            }
            if (string.IsNullOrEmpty(message.ContentType))
            {
                missingFields.Add("ContentType");
            }

            if (missingFields.Count > 0)
                throw new HeaderValidationException("One or more fields are missing")
                {
                    Fields = missingFields
                };
        }


        /// <summary>
        /// Utilty method that helps us determine the name of specific queue
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        protected string ResolveQueueName(Func<CommunicationPartyDetails, string> action)
        {
            if (_myDetails == null)
            {
                try
                {
                    _myDetails = Core.AddressRegistry.FindCommunicationPartyDetailsAsync(Logger, Core.Settings.MyHerId).Result;
                }
                catch (Exception ex)
                {
                    Logger.LogException("Fetching my details from address registry", ex);
                }
            }
            if (_myDetails == null) return null;

            var queueName = action(_myDetails);
            return string.IsNullOrEmpty(queueName) == false ? Core.ExtractQueueName(queueName) : null;
        }

        private async Task SetUpReceiver()
        {
            if (string.IsNullOrEmpty(QueueName))
            {
                throw new MessagingException("Queue name is empty. This could be due to connection issue with Address Registry")
                {
                    EventId = EventIds.QueueNameEmptyEventId
                };
            }
            if ((_messageReceiver != null) && _messageReceiver.IsClosed)
            {
                _messageReceiver = null;
            }
            if (_messageReceiver == null)
            {
                _messageReceiver = await Core.ReceiverPool.CreateCachedMessageReceiver(Logger, QueueName).ConfigureAwait(false);
            }
        }
    }
}

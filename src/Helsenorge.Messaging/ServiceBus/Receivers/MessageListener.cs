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
            if (core == null) throw new ArgumentNullException(nameof(core));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            Logger = logger;
            Core = core;
            MessagingNotification = messagingNotification;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected abstract void NotifyMessageProcessingStarted(IncomingMessage message);
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected abstract void NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message);
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected abstract void NotifyMessageProcessingCompleted(IncomingMessage message);
        /// <summary>
        /// Starts the listener
        /// </summary>
        /// <param name="cancellation">Cancellation token that signals when we should stop</param>
        public void Start(CancellationToken cancellation)
        {
            Logger.LogInformation("Starting listener on queue {0}", QueueName);

            while (cancellation.IsCancellationRequested == false)
            {
                try
                {
                    Task.WaitAll(ReadAndProcessMessage());
                }
                catch (Exception ex) // protect the main message pump
                {
                    Logger.LogException("Generic service bus error", ex);
                    // if there are problems with the message bus, we don't get interval of the ReadTimeout
                    // pause a bit so that we don't take over the whole system
                    Thread.Sleep(5000);
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
            SetUpReceiver();
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
                var incomingMessage = new IncomingMessage()
                {
                    MessageFunction = message.MessageFunction,
                    FromHerId = message.FromHerId,
                    ToHerId = message.ToHerId,
                    MessageId = message.MessageId,
                    CorrelationId = message.CorrelationId,
                    EnqueuedTimeUtc = message.EnqueuedTimeUtc,
                };
                NotifyMessageProcessingStarted(incomingMessage);
                Logger.LogStartReceive(QueueType, incomingMessage);

                // we cannot dispose of the stream before we have potentially cloned the message for error use
                bodyStream = message.GetBody();

                ValidateMessageHeader(message);
                // we need the certificates for decryption and certificate use
                incomingMessage.CollaborationAgreement = await ResolveProfile(message).ConfigureAwait(false);

                bool contentWasSigned;
                var payload = HandlePayload(message, bodyStream, message.ContentType, incomingMessage, out contentWasSigned);
                incomingMessage.ContentWasSigned = contentWasSigned;
                if (payload != null)
                {
                    if (Core.LogPayload)
                    {
                        Logger.LogDebug(payload.ToString());
                    }
                    incomingMessage.Payload = payload;
                }
                NotifyMessageProcessingReady(message, incomingMessage);
                ServiceBusCore.RemoveProcessedMessageFromQueue(Logger, message);
                NotifyMessageProcessingCompleted(incomingMessage);

                Logger.LogEndReceive(QueueType, incomingMessage);
                return incomingMessage;
            }
            catch (SecurityException ex)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.RemoteCertificate, message, "transport:invalid-certificate", ex.Message, null, ex);
                MessagingNotification.NotifyHandledException(message, ex);
            }
            catch (HeaderValidationException ex)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.MissingField, message, "transport:invalid-field-value", ex.Message, ex.Fields);
                MessagingNotification.NotifyHandledException(message, ex);
            }
            catch (XmlSchemaValidationException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.NotXml, message, "transport:not-well-formed-xml", ex.Message, null, ex);
                MessagingNotification.NotifyHandledException(message, ex);
            }
            catch (ReceivedDataMismatchException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.DataMismatch, message, "transport:invalid-field-value", ex.Message, new[] { ex.ExpectedValue, ex.ReceivedValue }, ex);
                MessagingNotification.NotifyHandledException(message, ex);
            }
            catch (NotifySenderException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.ApplicationReported, message, "transport:internal-error", ex.Message, null, ex);
                MessagingNotification.NotifyHandledException(message, ex);
            }
            catch (SenderHerIdMismatchException ex) // reportable error from message handler (application)
            {
                Core.ReportErrorToExternalSender(Logger, EventIds.DataMismatch, message, "abuse:spoofing-attack", ex.Message, null, ex);
                MessagingNotification.NotifyHandledException(message, ex);
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
                MessagingNotification.NotifyUnhandledException(message, ex);
            }
            finally
            {
                bodyStream?.Dispose();
                message.Dispose();
            }
            return null;
        }

        private async Task<CollaborationProtocolProfile> ResolveProfile(IMessagingMessage message)
        {
            Guid id;

            if (Guid.TryParse(message.CpaId, out id) && (id != Guid.Empty))
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
                payload = new NoMessageProtection().Unprotect(bodyStream, null, null, null);
            }
            else
            {
                contentWasSigned = true;
                // if we receive enrypted messages on the error queue, we have no idea what to do with them
                // Since this can be message we sent, it's encrypted with their certificate and we don't have that private key
                if (QueueType == QueueType.Error) return null;

                // in receive mode, we try to decrypt and validate content even if the certificates are invalid
                // invalid certificates are flagged to the application layer processing the decrypted message.
                // with the decrypted content, they may have a chance to figure out who sent it
                var decryption = Core.Settings.DecryptionCertificate.Certificate;
                var signature = incomingMessage.CollaborationAgreement?.SignatureCertificate;
                var legacyDecryption = Core.Settings.LegacyDecryptionCertificate?.Certificate;

                incomingMessage.DecryptionError = Core.DefaultCertificateValidator.Validate(decryption, X509KeyUsageFlags.DataEncipherment);
                ReportErrorOnLocalCertificate(originalMessage, decryption, incomingMessage.DecryptionError, true);

                incomingMessage.SignatureError = Core.DefaultCertificateValidator.Validate(signature, X509KeyUsageFlags.NonRepudiation);
                ReportErrorOnRemoteCertificate(originalMessage, signature, incomingMessage.SignatureError);

                if (legacyDecryption != null)
                {
                    // this is optional information that should only be in effect durin a short transition period
                    incomingMessage.LegacyDecryptionError = Core.DefaultCertificateValidator.Validate(legacyDecryption, X509KeyUsageFlags.DataEncipherment);
                    // if someone forgets to remove the legacy configuration, we log an error message but don't remove it
                    ReportErrorOnLocalCertificate(originalMessage, legacyDecryption, incomingMessage.LegacyDecryptionError, false);
                }

                payload = Core.DefaultMessageProtection.Unprotect(bodyStream, decryption, signature, legacyDecryption);
            }
            return payload;
        }

        private void ReportErrorOnRemoteCertificate(IMessagingMessage originalMessage, X509Certificate2 certificate,
            CertificateErrors error)
        {
            string errorCode;
            string description;
            EventId id;

            switch (error)
            {
                case CertificateErrors.None:
                // no error
                case CertificateErrors.Missing:
                    // if the certificate is missing, it's because we don't know where it came from
                    // and have no idea where to send an error message
                    return;
                case CertificateErrors.StartDate:
                    errorCode = "transport:expired-certificate";
                    description = "Invalid start date";
                    id = EventIds.RemoteCertificateStartDate;
                    break;
                case CertificateErrors.EndDate:
                    errorCode = "transport:expired-certificate";
                    description = "Invalid end date";
                    id = EventIds.RemoteCertificateEndDate;
                    break;
                case CertificateErrors.Usage:
                    errorCode = "transport:invalid-certificate";
                    description = "Invalid usage";
                    id = EventIds.RemoteCertificateUsage;
                    break;
                case CertificateErrors.Revoked:
                    errorCode = "transport:revoked-certificate";
                    description = "Certificate has been revoked";
                    id = EventIds.RemoteCertificateRevocation;
                    break;
                case CertificateErrors.RevokedUnknown:
                    errorCode = "transport:revoked-certificate";
                    description = "Unable to determine revocation status";
                    id = EventIds.RemoteCertificateRevocation;
                    break;
                default: // since the value is bitcoded
                    errorCode = "transport:invalid-certificate";
                    description = "More than one error with certificate";
                    id = EventIds.RemoteCertificate;
                    break;
            }
            var additionalInformation =
                (error != CertificateErrors.Missing) || (error != CertificateErrors.None) ?
                new[] { certificate.Subject, certificate.Thumbprint } :
                new string[] { };

            Core.ReportErrorToExternalSender(Logger, id, originalMessage, errorCode, description, additionalInformation);
        }

        private void ReportErrorOnLocalCertificate(IMessagingMessage originalMessage, X509Certificate2 certificate, CertificateErrors error, bool removeMessage)
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
                default: // since the value is bitcoded
                    description = "More than one error with certificate";
                    id = EventIds.LocalCertificate;
                    break;
            }
            Logger.LogError(id, null, "Description: {Description} Subject: {Subject} Thumbprint: {Thumbprint}",
                description, certificate.Subject, certificate.Thumbprint);

            if (removeMessage)
            {
                ServiceBusCore.RemoveMessageFromQueueAfterError(Logger, originalMessage);
            }
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
        private void SetUpReceiver()
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
                _messageReceiver = Core.ReceiverPool.CreateCachedMessageReceiver(Logger, QueueName);
            }
        }
    }
}

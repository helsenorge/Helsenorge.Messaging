/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Amqp.Exceptions;
using Helsenorge.Registries;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Amqp.Receivers
{
    /// <summary>
    /// Base class for all listeners, not just the ones processing xml messages
    /// </summary>
    public abstract class MessageListener
    {
        private CommunicationPartyDetails _myDetails;
        private IAmqpReceiver _messageReceiver;
        private bool _listenerEstablishedConfirmed;
        /// <summary>
        /// The available queues for this instance.
        /// </summary>
        protected QueueNames _queueNames;

        /// <summary>
        /// A helper method to set the Correlation Id from the callee code.
        /// </summary>
        public Action<string> SetCorrelationIdAction { get; set; }

        /// <summary>
        /// Returns the Last Read Time from the queue in UTC format.
        /// </summary>
        public DateTime LastReadTimeUtc { get; private set; }

        /// <summary>
        /// The timeout used for reading messages from queue
        /// </summary>
        protected TimeSpan ReadTimeout { get; set; }

        /// <summary>
        /// Specifies the name of the queue this listener is reading from
        /// </summary>
        protected virtual string GetQueueName()
        {
            if (_queueNames == null)
                throw new QueueNameNotSetException("QueueName has not been set. If this is intentional GetQueueName() must be overriden in your derived class.");

            return QueueType switch
            {
                QueueType.Asynchronous => _queueNames.Async,
                QueueType.Synchronous => _queueNames.Sync,
                QueueType.Error => _queueNames.Error,
                _ => throw new UnknownQueueTypeException(QueueType)
            };
        }
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected abstract QueueType QueueType { get; }
        /// <summary>
        /// Reference to service bus related setting
        /// </summary>
        protected AmqpCore AmqpCore { get; }

        /// <summary>
        /// Gets a reference to the server
        /// </summary>
        protected IMessagingNotification MessagingNotification { get; }
        /// <summary>
        /// Gets the logger used for diagnostics purposes
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageListener"/> class.
        /// </summary>
        /// <param name="amqpCore">An instance of <see cref="AmqpCore"/> which has the common infrastructure to talk to the Message Bus.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/>, used to log diagnostics information.</param>
        /// <param name="messagingNotification">An instance of <see cref="IMessagingNotification"/> which holds reference to callbacks back to the client that owns this instance of the <see cref="MessageListener"/>.</param>
        /// <param name="queueNames">The Queue Names associated with the client.</param>
        protected MessageListener(
            AmqpCore amqpCore,
            ILogger logger,
            IMessagingNotification messagingNotification,
            QueueNames queueNames = null)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AmqpCore = amqpCore ?? throw new ArgumentNullException(nameof(amqpCore));
            MessagingNotification = messagingNotification;
            _queueNames = queueNames;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected abstract Task NotifyMessageProcessingStartedAsync(MessageListener listener, IncomingMessage message);
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected abstract Task NotifyMessageProcessingReadyAsync(IAmqpMessage rawMessage, IncomingMessage message);
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected abstract Task NotifyMessageProcessingCompletedAsync(IncomingMessage message);
        /// <summary>
        /// Starts the listener
        /// </summary>
        /// <param name="cancellation">Cancellation token that signals when we should stop</param>
        public async Task StartAsync(CancellationToken cancellation)
        {
            var queueName = AmqpCore.ExtractQueueName(GetQueueName());
            Logger.LogInformation($"Starting listener on host and queue '{AmqpCore.HostnameAndPath}/{queueName}'");

            while (cancellation.IsCancellationRequested == false)
            {
                try
                {
                    await ReadAndProcessMessageAsync().ConfigureAwait(false);

                    if (!_listenerEstablishedConfirmed)
                    {
                        Logger.LogInformation($"Listener established on host and queue '{AmqpCore.HostnameAndPath}/{queueName}'");
                        _listenerEstablishedConfirmed = true;
                    }

                    LastReadTimeUtc = DateTime.UtcNow;
                }
                catch (Exception ex) // protect the main message pump
                {
                    Logger.LogException($"Generic service bus error at '{AmqpCore.HostnameAndPath}/{queueName}'", ex);
                    // if there are problems with the message bus, we don't get interval of the ReadTimeout
                    // pause a bit so that we don't take over the whole system
                    await Task.Delay(5000, cancellation)
                        .ContinueWith(task => task.Exception == default)
                        .ConfigureAwait(false);
                }
                finally
                {
                    if (AmqpCore.Settings.LogReadTime)
                        Logger.LogInformation($"Last Read Time UTC: '{LastReadTimeUtc.ToString(StringFormatConstants.IsoDateTime, DateTimeFormatInfo.InvariantInfo)}' on host and queue: '{AmqpCore.HostnameAndPath}/{queueName}'");
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
        public async Task<IncomingMessage> ReadAndProcessMessageAsync(bool alwaysRemoveMessage = false)
        {
            var queueName = AmqpCore.ExtractQueueName(GetQueueName());
            await SetUpReceiverAsync(queueName).ConfigureAwait(false);

            IAmqpMessage message;
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
            return await HandleRawMessageAsync(message, alwaysRemoveMessage).ConfigureAwait(false);
        }
        private async Task<IncomingMessage> HandleRawMessageAsync(IAmqpMessage message, bool alwaysRemoveMessage)
        {
            if (message == null) return null;
            var queueName = AmqpCore.ExtractQueueName(GetQueueName());
            Stream bodyStream = null;
            bool disposeMessage = true;

            try
            {
                var stopwatch = Stopwatch.StartNew();
                if(message.LockedUntil.ToUniversalTime() != DateTime.MinValue && message.LockedUntil.ToUniversalTime() <= DateTime.UtcNow)
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
                    Complete = message.Complete,
                    CompleteAsync = message.CompleteAsync,
                    Release = message.Release,
                    ReleaseAsync = message.RelaseAsync,
                    // FIXME: This will be moved to the interface IMessagingMessage in version 5.0
                    FirstAquirer = (message as AmqpMessage)?.FirstAcquirer ?? false,
                    DeliveryCount = message.DeliveryCount,
                    LockedUntil = message.LockedUntil,
                };
                await NotifyMessageProcessingStartedAsync(this, incomingMessage).ConfigureAwait(false);

                SetCorrelationIdAction?.Invoke(incomingMessage.MessageId);

                Logger.LogStartReceive(QueueType, incomingMessage, $"Message received from host and queue: {AmqpCore.HostnameAndPath}/{queueName}");

                // we cannot dispose of the stream before we have potentially cloned the message for error use
                bodyStream = message.GetBody();

                ValidateMessageHeader(message);
                // we need the certificates for decryption and certificate use
                incomingMessage.CollaborationAgreement = await ResolveProfileAsync(message).ConfigureAwait(false);

                var payload = HandlePayload(message, bodyStream, message.ContentType, incomingMessage, out bool contentWasSigned);
                incomingMessage.ContentWasSigned = contentWasSigned;
                if (payload != null)
                {
                    if (AmqpCore.LogPayload)
                    {
                        Logger.LogDebug("Raw payload: " + payload.ToString().Replace("\"\"","\""));
                    }
                    incomingMessage.Payload = payload;
                }
                await NotifyMessageProcessingReadyAsync(message, incomingMessage).ConfigureAwait(false);
                AmqpCore.RemoveProcessedMessageFromQueue(message);
                Logger.LogRemoveMessageFromQueueNormal(message, queueName);
                await NotifyMessageProcessingCompletedAsync(incomingMessage).ConfigureAwait(false);
                Logger.LogEndReceive(QueueType, incomingMessage, stopwatch.ElapsedMilliseconds);
                stopwatch.Stop();
                return incomingMessage;
            }
            catch (CertificateException ex)
            {
                Logger.LogWarning($"{ex.Description}. MessageFunction: {message.MessageFunction} " +
                  $"FromHerId: {message.FromHerId} ToHerId: {message.ToHerId} CpaId: {message.CpaId} " +
                  $"CorrelationId: {message.CorrelationId} Certificate thumbprint: {ex.AdditionalInformation}");

                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, ex.EventId, message, ex.ErrorCode, ex.Description, ex.AdditionalInformation).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (SecurityException ex)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.RemoteCertificate, message, "transport:invalid-certificate", ex.Message, null, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (HeaderValidationException ex)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.MissingField, message, "transport:invalid-field-value", ex.Message, ex.Fields).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (XmlSchemaValidationException ex) // reportable error from message handler (application)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.NotXml, message, "transport:not-well-formed-xml", ex.Message, null, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (ReceivedDataMismatchException ex) // reportable error from message handler (application)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.DataMismatch, message, "transport:invalid-field-value", ex.Message, new[] { ex.ExpectedValue, ex.ReceivedValue }, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (NotifySenderException ex) // reportable error from message handler (application)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.ApplicationReported, message, "transport:internal-error", ex.Message, null, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (SenderHerIdMismatchException ex) // reportable error from message handler (application)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.DataMismatch, message, "abuse:spoofing-attack", ex.Message, null, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (PayloadDeserializationException ex) // from parsing to XML, reportable exception
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.ApplicationReported, message, "transport:not-well-formed-xml", ex.Message, null, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (AggregateException ex) when (ex.InnerException is MessagingException && ((MessagingException)ex.InnerException).EventId.Id == EventIds.Send.Id)
            {
                Logger.LogError(EventIds.Send, ex, $"Send operation failed when processing message with MessageId: {message.MessageId} MessageFunction: {message.MessageFunction}");
                await MessagingNotification.NotifyUnhandledExceptionAsync(message, ex);
            }
            catch (UnsupportedMessageException ex)  // reportable error from message handler (application)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.ApplicationReported, message, "transport:unsupported-message", ex.Message, null, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (InvalidHerIdException ex)
            {
                await AmqpCore.ReportErrorToExternalSenderAsync(Logger, EventIds.InvalidHerId, message, "transport:invalid-field-value", ex.Message, new [] { "FromHerId", $"{ex.HerId}" }, ex).ConfigureAwait(false);
                await MessagingNotification.NotifyHandledExceptionAsync(message, ex).ConfigureAwait(false);
            }
            catch (Exception ex) // unknown error
            {
                message.AddDetailsToException(ex);
                Logger.LogError(EventIds.UnknownError, null, $"Message processing failed. Keeping lock until it times out and we can try again. Message expires at UTC {message.ExpiresAtUtc}");
                Logger.LogException("Unknown error", ex);
                // if something unknown goes wrong, we want to retry the message after a delay
                // we don't call Complete() or Abandon() since that will cause the message to be available again
                // chances are that the failure may still be around
                // the Defer() method requires us to store the sequence id, but we don't have a place to store it
                // the option then is to let the lock time-out. This will happen after a couple of minutes and the
                // message becomes available again. 10 retries = 10 timeouts before it gets added to DLQ

                if (alwaysRemoveMessage)
                {
                    AmqpCore.RemoveMessageFromQueueAfterError(Logger, message);
                }
                await MessagingNotification.NotifyUnhandledExceptionAsync(message, ex).ConfigureAwait(false);

                // Start a thread which will await until we reach LockedUntilUtc
                // before releasing the message.
                RunMessageReleaseThread(message);
                disposeMessage = false;
            }
            finally
            {
                bodyStream?.Dispose();
                if(disposeMessage)
                    message.Dispose();
            }
            return null;
        }

        private void RunMessageReleaseThread(IAmqpMessage message)
        {
            var messageReleaseThread = new Thread(MessageReleaseThread.AwaitRelease);
            messageReleaseThread.Start(new MessageReleaseThread.ThreadData { Message = message, Logger = Logger });
        }

        private async Task<CollaborationProtocolProfile> ResolveProfileAsync(IAmqpMessage message)
        {
            if(AmqpCore.MessagingSettings.MessageFunctionsExcludedFromCpaResolve.Contains(message.MessageFunction))
            {
                // MessageFunction is defined in exception list, return a dummy CollaborationProtocolProfile
                return await DummyCollaborationProtocolProfileFactory.CreateAsync(AmqpCore.AddressRegistry, Logger, message.FromHerId, message.MessageFunction);
            }

            // if we receive an error message then CPA isn't needed because we're not decrypting the message and then the CPA info isn't needed
            if (QueueType == QueueType.Error) return null;
            if (Guid.TryParse(message.CpaId, out Guid id) && (id != Guid.Empty))
            {
                try
                {
                    return await AmqpCore.CollaborationProtocolRegistry.FindAgreementByIdAsync(id, message.ToHerId).ConfigureAwait(false);
                }
                //Continue if not able to find CPA by Id
                catch (RegistriesException ex)
                {
                    Logger.LogInformation($"Tried to fetch Cpa from CpaID, continuing as if there wasn't a CpaId. Error message: {ex.Message}");
                }
            }
            return
                // try first to find an agreement
                await AmqpCore.CollaborationProtocolRegistry.FindAgreementForCounterpartyAsync(message.ToHerId, message.FromHerId).ConfigureAwait(false) ??
                // if we cannot find that, we fallback to protocol (which may return a dummy protocol if things are really missing in AR)
                await AmqpCore.CollaborationProtocolRegistry.FindProtocolForCounterpartyAsync(message.FromHerId).ConfigureAwait(false);
        }

        private XDocument HandlePayload(IAmqpMessage originalMessage, Stream bodyStream, string contentType, IncomingMessage incomingMessage, out bool contentWasSigned)
        {
            XDocument payload;

            bool isPlainText;
            if ((isPlainText = contentType.Equals(ContentType.Text, StringComparison.OrdinalIgnoreCase))
                            || contentType.Equals(ContentType.Soap, StringComparison.OrdinalIgnoreCase))
            {
                contentWasSigned = false;
                // no certificates to validate
                payload = new NoMessageProtection().Unprotect(bodyStream, null)?.ToXDocument();

                // Log a warning if the message is in plain text.
                if (AmqpCore.Settings.LogMessagesNotSignedAndEnvelopedAsWarning && isPlainText)
                    Logger.LogWarning($"ContentType of message is '{contentType}'.");
            }
            else
            {
                contentWasSigned = true;
                // if we receive encrypted messages on the error queue, we have no idea what to do with them
                // Since this can be message we sent, it's encrypted with their certificate and we don't have that private key
                if (QueueType == QueueType.Error) return null;

                // TODO: The whole part of validating the local certificates below should probably be move into
                // the IMessageProtection implementation, but since there are some constraints to properties on
                // IMessagingMessage we'll keep it here for now

                // in receive mode, we try to decrypt and validate content even if the certificates are invalid
                // invalid certificates are flagged to the application layer processing the decrypted message.
                // with the decrypted content, they may have a chance to figure out who sent it

                var validator = AmqpCore.CertificateValidator;
                var stopwatch = new Stopwatch();
                Logger.LogBeforeValidatingCertificate(originalMessage.MessageFunction, AmqpCore.MessageProtection.EncryptionCertificate.Thumbprint, AmqpCore.MessageProtection.EncryptionCertificate.Subject, "KeyEncipherment", originalMessage.ToHerId, originalMessage.MessageId);
                stopwatch.Start();
                // validate the local encryption certificate and, if present, the local legacy encryption certificate
                incomingMessage.DecryptionError = validator == null
                    ? CertificateErrors.None
                    : validator.Validate(AmqpCore.MessageProtection.EncryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
                stopwatch.Stop();
                Logger.LogAfterValidatingCertificate(originalMessage.MessageFunction, AmqpCore.MessageProtection.EncryptionCertificate.Thumbprint, "KeyEncipherment", originalMessage.ToHerId, originalMessage.MessageId, stopwatch.ElapsedMilliseconds.ToString());
                // in earlier versions of Helsenorge.Messaging we removed the message, but we should rather
                // want it to be dead lettered since this is a temp issue that should be fixed locally.
                ReportErrorOnLocalCertificate(AmqpCore.MessageProtection.EncryptionCertificate, incomingMessage.DecryptionError);

                if(AmqpCore.MessageProtection.LegacyEncryptionCertificate != null)
                {
                    Logger.LogBeforeValidatingCertificate(originalMessage.MessageFunction, AmqpCore.MessageProtection.LegacyEncryptionCertificate.Thumbprint, AmqpCore.MessageProtection.LegacyEncryptionCertificate.Subject, "KeyEncipherment", originalMessage.ToHerId, originalMessage.MessageId);
                    stopwatch.Restart();
                    // this is optional information that should only be in effect during a short transition period
                    incomingMessage.LegacyDecryptionError = validator == null
                        ? CertificateErrors.None
                        : validator.Validate(AmqpCore.MessageProtection.LegacyEncryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
                    stopwatch.Stop();
                    Logger.LogAfterValidatingCertificate(originalMessage.MessageFunction, AmqpCore.MessageProtection.LegacyEncryptionCertificate.Thumbprint, "KeyEncipherment", originalMessage.ToHerId, originalMessage.MessageId, stopwatch.ElapsedMilliseconds.ToString());

                    // if someone forgets to remove the legacy configuration, we log an error message but don't remove it
                    ReportErrorOnLocalCertificate(AmqpCore.MessageProtection.LegacyEncryptionCertificate, incomingMessage.LegacyDecryptionError);
                }

                var signature = incomingMessage.CollaborationAgreement?.SignatureCertificate;
                Logger.LogBeforeValidatingCertificate(originalMessage.MessageFunction, signature?.Thumbprint, signature?.Subject, "NonRepudiation", originalMessage.ToHerId, originalMessage.MessageId);
                stopwatch.Restart();
                // validate remote signature certificate
                incomingMessage.SignatureError = validator == null
                    ? CertificateErrors.None
                    : validator.Validate(signature, X509KeyUsageFlags.NonRepudiation);
                stopwatch.Stop();
                Logger.LogAfterValidatingCertificate(originalMessage.MessageFunction, signature?.Thumbprint, "NonRepudiation", originalMessage.ToHerId, originalMessage.MessageId, stopwatch.ElapsedMilliseconds.ToString());

                ReportErrorOnRemoteCertificate(signature, incomingMessage.SignatureError);

                Logger.LogBeforeDecryptingPayload(originalMessage.MessageFunction, signature?.Thumbprint, AmqpCore.MessageProtection.EncryptionCertificate.Thumbprint, originalMessage.FromHerId, originalMessage.ToHerId, originalMessage.MessageId);
                stopwatch.Restart();
                // decrypt the message and validate the signatureS
                payload = AmqpCore.MessageProtection.Unprotect(bodyStream, signature)?.ToXDocument();
                Logger.LogAfterDecryptingPayload(originalMessage.MessageFunction, originalMessage.FromHerId, originalMessage.ToHerId, originalMessage.MessageId, stopwatch.ElapsedMilliseconds.ToString());
                stopwatch.Stop();
            }
            return payload;
        }

        private void ReportErrorOnRemoteCertificate(X509Certificate2 certificate,
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
                default: // since the value is bit-coded
                    throw new CertificateException(error, "transport:invalid-certificate", "More than one error with certificate",
                        EventIds.RemoteCertificate, AdditionalInformation(certificate));
            }
        }

        private static string[] AdditionalInformation(X509Certificate2 certificate)
        {
            return certificate != null ? new[] {certificate.Subject, certificate.Thumbprint} : new string[] { };
        }

        private void ReportErrorOnLocalCertificate(X509Certificate2 certificate, CertificateErrors error)
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
                default: // since the value is bit-coded
                    description = "More than one error with certificate";
                    id = EventIds.LocalCertificate;
                    break;
            }
            Logger.LogError(id, null, "Description: {Description} Subject: {Subject} Thumbprint: {Thumbprint}",
                description, certificate?.Subject, certificate?.Thumbprint);
        }

        private void ValidateMessageHeader(IAmqpMessage message)
        {
            Logger.LogDebug("Validating message header");
            var missingFields = new List<string>();

            // the FromHerId is not checked by design. If this validation fails, we need to send a message back to the sender
            // but we have no idea who the sender is because the information is missing
            if (message.ToHerId == 0)
            {
                missingFields.Add(AmqpCore.ToHerIdHeaderKey);
            }
            if(message.FromHerId == 0)
            {
                missingFields.Add(AmqpCore.FromHerIdHeaderKey);
            }
            if (string.IsNullOrEmpty(message.MessageFunction))
            {
                missingFields.Add("Label");
            }
            if (message.ApplicationTimestamp == DateTime.MinValue)
            {
                missingFields.Add(AmqpCore.ApplicationTimestampHeaderKey);
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
        /// Utility method that helps us determine the name of specific queue
        /// </summary>
        /// <param name="myHerId"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        protected string ResolveQueueName(int myHerId, Func<CommunicationPartyDetails, string> action)
        {
            if (_myDetails == null)
            {
                try
                {
                    _myDetails = AmqpCore.AddressRegistry.FindCommunicationPartyDetailsAsync(myHerId).Result;
                }
                catch (Exception ex)
                {
                    Logger.LogException("Fetching my details from address registry", ex);
                }
            }
            if (_myDetails == null) return null;

            var queueName = action(_myDetails);
            return string.IsNullOrEmpty(queueName) == false ? AmqpCore.ExtractQueueName(queueName) : null;
        }

        private async Task SetUpReceiverAsync(string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
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
                _messageReceiver = await AmqpCore.ReceiverPool.CreateCachedMessageReceiverAsync(Logger, queueName).ConfigureAwait(false);
            }
        }
    }
}

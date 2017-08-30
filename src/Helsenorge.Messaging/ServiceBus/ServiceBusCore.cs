using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;
using Helsenorge.Messaging.Http;

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
		

		//convencience properties
		internal ServiceBusSettings Settings => Core.Settings.ServiceBus;
		internal IAddressRegistry AddressRegistry => Core.AddressRegistry;
		internal ICollaborationProtocolRegistry CollaborationProtocolRegistry => Core.CollaborationProtocolRegistry;
		internal ICertificateValidator DefaultCertificateValidator => Core.DefaultCertificateValidator;
		internal IMessageProtection DefaultMessageProtection => Core.DefaultMessageProtection;
		internal bool LogPayload => Core.Settings.LogPayload;
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
			if (core == null) throw new ArgumentNullException(nameof(core));
			
			Core = core;
            
            var connectionString = core.Settings.ServiceBus.ConnectionString;
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }
            if (connectionString.StartsWith("http://") || connectionString.StartsWith("https://"))
            {
                FactoryPool = new HttpServiceBusFactoryPool(core.Settings.ServiceBus);
            }
            else
            {
                FactoryPool = new ServiceBusFactoryPool(core.Settings.ServiceBus);
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
            logger.LogDebug($"Start-ServiceBusCore::Send QueueType: {queueType} replyTo: {replyTo} correlationId: {correlationId}");

			if (outgoingMessage == null) throw new ArgumentNullException(nameof(outgoingMessage));
			if (string.IsNullOrEmpty(outgoingMessage.MessageId)) throw new ArgumentNullException(nameof(outgoingMessage.MessageId));
			if (outgoingMessage.Payload == null) throw new ArgumentNullException(nameof(outgoingMessage.Payload));

			var hasAgreement = true;
			// first we try and find an agreement
			var profile = await CollaborationProtocolRegistry.FindAgreementForCounterpartyAsync(logger, outgoingMessage.ToHerId).ConfigureAwait(false);
			if (profile == null)
			{
				hasAgreement = false; // if we don't have an agreement, we try to find the specific profile
				profile = await CollaborationProtocolRegistry.FindProtocolForCounterpartyAsync(logger, outgoingMessage.ToHerId).ConfigureAwait(false);
			}

            logger.LogDebug($"ServiceBusCore::Send - Start retrieving and valiating certificates - correlationId: {correlationId}");
			var signature = Settings.SigningCertificate.Certificate;
			var encryption = profile.EncryptionCertificate;

			var validator = Core.DefaultCertificateValidator;
			var encryptionStatus = validator.Validate(encryption, X509KeyUsageFlags.DataEncipherment);
			var signatureStatus = validator.Validate(signature, X509KeyUsageFlags.NonRepudiation);

			// this is the other parties certificate that may be out of date, not something we can fix
			if (encryptionStatus != CertificateErrors.None)
			{
				if (Core.Settings.IgnoreCertificateErrorOnSend)
				{
					logger.LogError(EventIds.RemoteCertificate, "Remote encryption certificate is not valid");
				}
				else
				{
					throw new MessagingException("Remote encryption certificate is not valid")
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
                    logger.LogError(EventIds.LocalCertificate, "Locally installed signing certificate is not valid");
                }
                else
                {
                    throw new MessagingException("Locally installed signing certificate is not valid")
                    {
                        EventId = EventIds.LocalCertificate
                    };
                }
			}
            logger.LogDebug($"ServiceBusCore::Send - End retrieving and valiating certificates - correlationId: {correlationId}");

            logger.LogDebug($"ServiceBusCore::Send - Start encrypting message - correlationId: {correlationId}");
            var protection = Core.DefaultMessageProtection;
			var stream = protection.Protect(outgoingMessage.Payload, encryption, signature);
            logger.LogDebug($"ServiceBusCore::Send - End encrypting message - correlationId: {correlationId}");

            logger.LogDebug($"ServiceBusCore::Send - Start Create and Initialize message - correlationId: {correlationId}");
            var messagingMessage = FactoryPool.CreateMessage(logger, stream, outgoingMessage);
			
			if (queueType != QueueType.SynchronousReply)
			{
				messagingMessage.ReplyTo = 
					replyTo ?? await ConstructQueueName(logger, Core.Settings.MyHerId, queueType).ConfigureAwait(false);
			}

            messagingMessage.ContentType = protection.ContentType;
			messagingMessage.MessageId = outgoingMessage.MessageId;
			// when we are replying to a synchronous message, we need to use the replyto of the original message
			messagingMessage.To = 
				(queueType == QueueType.SynchronousReply) ? 
				replyTo : 
				await ConstructQueueName(logger, outgoingMessage.ToHerId, queueType).ConfigureAwait(false);
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
            logger.LogDebug($"ServiceBusCore::Send - End Create and Initialize message - correlationId: {correlationId}");

            await Send(logger, messagingMessage, queueType, outgoingMessage.PersonalId, (LogPayload) ? outgoingMessage.Payload : null).ConfigureAwait(false);

            logger.LogDebug($"End-ServiceBusCore::Send QueueType: {queueType} replyTo: {replyTo} correlationId: {correlationId}");
        }

		/// <summary>
		/// Sends a prepared message 
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="message">The prepared message</param>
		/// <param name="queueType">The type of queue to use</param>
		/// <param name="userId"></param>
		/// <param name="xml">Optional xml content. This will be logged depending on the logging level.</param>
		/// <returns></returns>
		private async Task Send(ILogger logger, IMessagingMessage message, QueueType queueType, string userId = "99999999999", XDocument xml = null)
		{
			if (message == null) throw new ArgumentNullException(nameof(message));

			logger.LogStartSend(queueType, message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId, userId, xml);
			IMessagingSender messageSender = null;

			try
			{
                logger.LogDebug("ServiceBusCore::Send - Start-SenderPool::CreateCachedMessageSender");
				messageSender = SenderPool.CreateCachedMessageSender(logger, message.To);
                logger.LogDebug("ServiceBusCore::Send - End-SenderPool::CreateCachedMessageSender");

                logger.LogDebug("ServiceBusCore::Send - Start-MessageSender::SendAsync");
                await messageSender.SendAsync(message).ConfigureAwait(false);
                logger.LogDebug("ServiceBusCore::Send - End-MessageSender::SendAsync");
            }
			catch (Exception ex)
			{
				throw new MessagingException(ex.Message)
				{
					EventId = EventIds.Send
				};
			}
			finally
			{
				if (messageSender != null)
				{
					SenderPool.ReleaseCachedMessageSender(logger, message.To);
				}
			}

			logger.LogEndSend(queueType, message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId, userId);
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
				logger.LogError(EventIds.MissingField, "FromHerId is missing. No idea where to send the error");
				return;
			}
			
			var clonedMessage = originalMessage.Clone();
			// update some properties on the cloned message
			clonedMessage.To = await ConstructQueueName(logger, originalMessage.FromHerId, QueueType.Error); // change target 
			clonedMessage.TimeToLive = Settings.Error.TimeToLive;
			clonedMessage.FromHerId = originalMessage.ToHerId;
			clonedMessage.ToHerId = originalMessage.FromHerId;
			
			if (clonedMessage.Properties.ContainsKey(OriginalMessageIdHeaderKey) == false)
			{
				clonedMessage.Properties.Add(OriginalMessageIdHeaderKey, originalMessage.MessageId);
			}
			if (clonedMessage.Properties.ContainsKey(ReceiverTimestampHeaderKey) == false)
			{
				clonedMessage.Properties.Add(ReceiverTimestampHeaderKey, DateTime.Now.ToString(DateTimeFormatInfo.InvariantInfo));
			}
			if (clonedMessage.Properties.ContainsKey(ErrorConditionHeaderKey) == false)
			{
				clonedMessage.Properties.Add(ErrorConditionHeaderKey, errorCode);
			}
			if (clonedMessage.Properties.ContainsKey(ErrorDescriptionHeaderKey) == false)
			{
				clonedMessage.Properties.Add(ErrorDescriptionHeaderKey, errorDescription);
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
					clonedMessage.Properties.Add(ErrorConditionDataHeaderKey, additionDataValue);
				}
			}
			logger.LogError("Reporting error to sender. ErrorCode: {0} ErrorDescription: {1} AdditionalData: {2}", errorCode, errorDescription, additionDataValue);
			await Send(logger, clonedMessage, QueueType.Error).ConfigureAwait(false);
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

			if (string.IsNullOrEmpty(queueAddress)) throw new ArgumentNullException(nameof(queueAddress));

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
			
			switch (type)
			{
				case QueueType.Asynchronous:
					return ExtractQueueName(details.AsynchronousQueueName);
				case QueueType.Synchronous:
					return ExtractQueueName(details.SynchronousQueueName);
				case QueueType.Error:
					return ExtractQueueName(details.ErrorQueueName);
				default:
					throw new InvalidOperationException("QueueType not supported");
			}
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
		internal void ReportErrorToExternalSender(
			ILogger logger,
			EventId id,
			IMessagingMessage originalMessage,
			string errorCode,
			string description,
			IEnumerable<string> additionalData,
			Exception ex = null)
		{
			logger.LogError(id, ex, description);
			Task.WaitAll(SendError(logger, originalMessage, errorCode, description, additionalData));
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
		/// <param name="logger"></param>
		/// <param name="message"></param>
		internal static void RemoveProcessedMessageFromQueue(ILogger logger, IMessagingMessage message)
		{
			if (message == null) throw new ArgumentNullException(nameof(message));

			logger.LogRemoveMessageFromQueueNormal(message.MessageId);
			message.Complete();
		}
		/// <summary>
		/// Registers an alternate messaging factory
		/// </summary>
		/// <param name="factory"></param>
		public void RegisterAlternateMessagingFactory(IMessagingFactory factory) => FactoryPool.RegisterAlternateMessagingFactory(factory);
	}
}

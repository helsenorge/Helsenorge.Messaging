using Microsoft.Extensions.Logging;
using System;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.ServiceBus
{
	internal static class ServiceBusLoggingExtensions
	{
		private static readonly Action<ILogger, QueueType, string, int, int, string, Exception> StartReceive;
		private static readonly Action<ILogger, QueueType, string, int, int, string, Exception> EndReceive;
		private static readonly Action<ILogger, QueueType, string, int, int, string, string, Exception> StartSend;
		private static readonly Action<ILogger, QueueType, string, int, int, string, string, Exception> EndSend;

		private static readonly Action<ILogger, string, Exception> ExternalReportedError;
		private static readonly Action<ILogger, string, Exception> RemoveMessageFromQueueNormal;
		private static readonly Action<ILogger, string, Exception> RemoveMessageFromQueueError;

		public static void LogStartReceive(this ILogger logger, QueueType queueType, IncomingMessage message)
		{
			StartReceive(logger, queueType, message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId, null);
		}
		public static void LogEndReceive(this ILogger logger, QueueType queueType, IncomingMessage message)
		{
			EndReceive(logger, queueType, message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId, null);
		}
		public static void LogStartSend(this ILogger logger, QueueType queueType, string function, int fromHerId, int toHerId, string messageId, string userId, XDocument xml)
		{
			StartSend(logger, queueType, function, fromHerId, toHerId, messageId, userId, null);
            logger.LogDebug($"Start-ServiceBusLogExtensions::LogStartSend QueueType: {queueType} function: {function} fromHerId: {fromHerId} toHerId: {toHerId} messageId: {messageId} userId: {userId} xml: {xml?.ToString()}");
			if (xml != null)
			{
				logger.LogDebug(xml.ToString());
			}
            logger.LogDebug($"End-ServiceBusLogExtensions::LogStartSend QueueType: {queueType} function: {function} fromHerId: {fromHerId} toHerId: {toHerId} messageId: {messageId} userId: {userId} xml: {xml?.ToString()}");
        }
		public static void LogEndSend(this ILogger logger, QueueType queueType, string function, int fromHerId, int toHerId, string messageId, string userId)
		{
            logger.LogDebug($"Start-ServiceBusLogExtensions::LogEndSend QueueType: {queueType} function: {function} fromHerId: {fromHerId} toHerId: {toHerId} messageId: {messageId} userId: {userId}");
            EndSend(logger, queueType, function, fromHerId, toHerId, messageId, userId, null);
            logger.LogDebug($"End-ServiceBusLogExtensions::LogEndSend QueueType: {queueType} function: {function} fromHerId: {fromHerId} toHerId: {toHerId} messageId: {messageId} userId: {userId}");
        }

		public static void LogExternalReportedError(this ILogger logger, string message)
		{
			ExternalReportedError(logger, message, null);
		}
		public static void LogRemoveMessageFromQueueNormal(this ILogger logger, string id)
		{
			RemoveMessageFromQueueNormal(logger, id, null);
		}
		public static void LogRemoveMessageFromQueueError(this ILogger logger, string id)
		{
			RemoveMessageFromQueueError(logger, id, null);
		}
		static ServiceBusLoggingExtensions()
		{
			StartReceive = LoggerMessage.Define<QueueType, string, int, int, string>(
				LogLevel.Information, 
				EventIds.ServiceBusReceive,
				"Start-ServiceBusReceive{QueueType}: {MessageFunction} From: {FromHerId} To: {ToHerId} Id: {MessageId}");

			EndReceive = LoggerMessage.Define<QueueType, string, int, int, string>(
				LogLevel.Information,
                EventIds.ServiceBusReceive,
				"End-ServiceBusReceive{QueueType}: {MessageFunction} From: {FromHerId} To: {ToHerId} Id: {MessageId}");

			StartSend = LoggerMessage.Define<QueueType, string, int, int, string, string>(
				LogLevel.Information,
                EventIds.ServiceBusSend,
				"Start-ServiceBusSend{QueueType}: {MessageFunction} From: {FromHerId} To: {ToHerId} Id: {MessageId} UserId: {UserId}");

			EndSend = LoggerMessage.Define<QueueType, string, int, int, string, string>(
				LogLevel.Information,
                EventIds.ServiceBusSend,
				"End-ServiceBusSend{QueueType}: {MessageFunction} From: {FromHerId} To: {ToHerId} Id: {MessageId} UserId: {UserId}");

			ExternalReportedError = LoggerMessage.Define<string>(
				LogLevel.Error,
				EventIds.ExternalReportedError,
				"{Message}");

			RemoveMessageFromQueueNormal = LoggerMessage.Define<string>(
				LogLevel.Information,
                EventIds.RemoveMessageFromQueue,
				"Removing processed message {MessageId} from queue");

			RemoveMessageFromQueueError = LoggerMessage.Define<string>(
				LogLevel.Information,
                EventIds.RemoveMessageFromQueue,
				"Removing message {MessageId} from queue after reporting error");
		}

		public static void LogException(this ILogger logger, string message,  Exception ex)
		{
			var sbe = ex as MessagingException;

			logger.LogCritical(sbe?.EventId ?? 0, ex, message);
		}
	}
}

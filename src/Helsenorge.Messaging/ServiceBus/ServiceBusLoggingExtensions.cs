/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

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
        private static readonly Action<ILogger, QueueType, string, int, int, string, Exception> StartSend;
        private static readonly Action<ILogger, QueueType, string, int, int, string, Exception> EndSend;
        private static readonly Action<ILogger, string, int, int, string, string, Exception> ResponseTime;
        private static readonly Action<ILogger, string, string, int, Exception> LogTimeout;

        private static readonly Action<ILogger, string, string, int, int, string, Exception> BeforeNotificationHandler;
        private static readonly Action<ILogger, string, string, int, int, string, Exception> AfterNotificationHandler;

        private static readonly Action<ILogger, string, string, string, string, int, string, Exception> BeforeValidatingCertificate;
        private static readonly Action<ILogger, string, string, string, int, string, string, Exception> AfterValidatingCertificate;

        private static readonly Action<ILogger, string, string, string, int, int, string, Exception> BeforeEncryptingPayload;
        private static readonly Action<ILogger, string, int, int, string, string, Exception> AfterEncryptingPayload;

        private static readonly Action<ILogger, string, string, string, int, int, string, Exception> BeforeDecryptingPayload;
        private static readonly Action<ILogger, string, int, int, string, string, Exception> AfterDecryptingPayload;

        private static readonly Action<ILogger, string, int, int, string, Exception> BeforeFactoryPoolCreateMessage;
        private static readonly Action<ILogger, string, int, int, string, Exception> AfterFactoryPoolCreateMessage;

        private static readonly Action<ILogger, string, Exception> ExternalReportedError;
        private static readonly Action<ILogger, string, int, string, string, Exception> RemoveMessageFromQueueNormal;
        private static readonly Action<ILogger, string, Exception> RemoveMessageFromQueueError;

        private static readonly Action<ILogger, string, Exception> RetryOperationInProgress;

        public static void LogStartReceive(this ILogger logger, QueueType queueType, IncomingMessage message)
        {
            StartReceive(logger, queueType, message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId, null);
        }
        public static void LogEndReceive(this ILogger logger, QueueType queueType, IncomingMessage message)
        {
            EndReceive(logger, queueType, message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId, null);
        }

        public static void LogStartSend(this ILogger logger, QueueType queueType, string function, int fromHerId, int toHerId, string messageId, XDocument xml)
        {
            StartSend(logger, queueType, function, fromHerId, toHerId, messageId,  null);
            if (xml != null)
            {
                logger.LogDebug(xml.ToString());
            }
        }
        public static void LogEndSend(this ILogger logger, QueueType queueType, string function, int fromHerId, int toHerId, string messageId)
        {
            EndSend(logger, queueType, function, fromHerId, toHerId, messageId, null);
        }

        public static void LogResponseTime(this ILogger logger, string messageFunction, int fromHerId, int toHerId, string messageId, string responseTimeMs)
        {
            ResponseTime(logger, messageFunction, fromHerId, toHerId, messageId, responseTimeMs, null);
        }

        public static void LogBeforeNotificationHandler(this ILogger logger, string notificationHandler, string messageFunction, int fromHerId, int toHerId, string messageId)
        {
            BeforeNotificationHandler(logger, notificationHandler, messageFunction, fromHerId, toHerId, messageId, null);
        }
        public static void LogAfterNotificationHandler(this ILogger logger, string notificationHandler, string messageFunction, int fromHerId, int toHerId, string messageId)
        {
            AfterNotificationHandler(logger, notificationHandler, messageFunction, fromHerId, toHerId, messageId, null);
        }

        public static void LogBeforeValidatingCertificate(this ILogger logger, string messageFunction, string thumbprint, string subject, string keyUsage, int ownerHerId, string messageId)
        {
            BeforeValidatingCertificate(logger, messageFunction, thumbprint, subject, keyUsage, ownerHerId, messageId, null);
        }
        public static void LogAfterValidatingCertificate(this ILogger logger, string messageFunction, string thumbprint, string keyUsage, int ownerHerId, string messageId, string responseTimeMs)
        {
            AfterValidatingCertificate(logger, messageFunction, thumbprint, keyUsage, ownerHerId, messageId, responseTimeMs, null);
        }

        public static void LogBeforeEncryptingPayload(this ILogger logger, string messageFunction, string signingThumbprint, string encryptionThumbprint, int fromHerId, int toHerId, string messageId)
        {
            BeforeEncryptingPayload(logger, messageFunction, signingThumbprint, encryptionThumbprint, fromHerId, toHerId, messageId, null);
        }
        public static void LogAfterEncryptingPayload(this ILogger logger, string messageFunction, int fromHerId, int toHerId, string messageId, string responseTimeMs)
        {
            AfterEncryptingPayload(logger, messageFunction, fromHerId, toHerId, messageId, responseTimeMs, null);
        }

        public static void LogBeforeDecryptingPayload(this ILogger logger, string messageFunction, string signingThumbprint, string encryptionThumbprint, int fromHerId, int toHerId, string messageId)
        {
            BeforeDecryptingPayload(logger, messageFunction, signingThumbprint, encryptionThumbprint, fromHerId, toHerId, messageId, null);
        }
        public static void LogAfterDecryptingPayload(this ILogger logger, string messageFunction, int fromHerId, int toHerId, string messageId, string responseTimeMs)
        {
            AfterDecryptingPayload(logger, messageFunction, fromHerId, toHerId, messageId, responseTimeMs, null);
        }

        public static void LogBeforeFactoryPoolCreateMessage(this ILogger logger, string messageFunction, int fromHerId, int toHerId, string messageId)
        {
            BeforeFactoryPoolCreateMessage(logger, messageFunction, fromHerId, toHerId, messageId, null);
        }
        public static void LogAfterFactoryPoolCreateMessage(this ILogger logger, string messageFunction, int fromHerId, int toHerId, string messageId)
        {
            AfterFactoryPoolCreateMessage(logger, messageFunction, fromHerId, toHerId, messageId, null);
        }

        public static void LogExternalReportedError(this ILogger logger, string message)
        {
            ExternalReportedError(logger, message, null);
        }

        public static void LogRemoveMessageFromQueueNormal(this ILogger logger, IMessagingMessage message, string queueName)
        {
            RemoveMessageFromQueueNormal(logger, message.MessageId, message.FromHerId, queueName, message.CorrelationId, null);
        }
        public static void LogRemoveMessageFromQueueError(this ILogger logger, string id)
        {
            RemoveMessageFromQueueError(logger, id, null);
        }

        public static void LogTimeoutError(this ILogger logger, string messageFunction, string messageId, int toHerId)
        {
            LogTimeout(logger, messageFunction, messageId, toHerId, null);
        }

        public static void LogRetryOperationInProgress(this ILogger logger, string message)
        {
            RetryOperationInProgress(logger, message, null);
        }

        static ServiceBusLoggingExtensions()
        {
            StartReceive = LoggerMessage.Define<QueueType, string, int, int, string>(
                LogLevel.Information,
                EventIds.ServiceBusReceive,
                "Start-ServiceBusReceive{QueueType}: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId}");

            EndReceive = LoggerMessage.Define<QueueType, string, int, int, string>(
                LogLevel.Information,
                EventIds.ServiceBusReceive,
                "End-ServiceBusReceive{QueueType}: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId}");

            StartSend = LoggerMessage.Define<QueueType, string, int, int, string>(
                LogLevel.Information,
                EventIds.ServiceBusSend,
                "Start-ServiceBusSend{QueueType}: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId}");

            EndSend = LoggerMessage.Define<QueueType, string, int, int, string>(
                LogLevel.Information,
                EventIds.ServiceBusSend,
                "End-ServiceBusSend{QueueType}: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId}");

            ResponseTime = LoggerMessage.Define<string, int, int, string, string>(
               LogLevel.Information,
               EventIds.NotificationHandler,
               "ResponseTime {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId} ResponseTime: {ResponseTimeMs} ms");

            LogTimeout = LoggerMessage.Define<string, string, int>(
                LogLevel.Error,
                EventIds.SynchronousCallTimeout,
                "MUG-000030 Error Synchronous call {MessageFunction} {messageId} timed out against HerId: {toHerId}.");

            ExternalReportedError = LoggerMessage.Define<string>(
                LogLevel.Error,
                EventIds.ExternalReportedError,
                "{Message}");

            RemoveMessageFromQueueNormal = LoggerMessage.Define<string, int, string, string>(
                LogLevel.Information,
                EventIds.RemoveMessageFromQueue,
                "Removing processed message {MessageId} from Herid {herId} from queue {queueName}. Correlation = {correlationId}");

            RemoveMessageFromQueueError = LoggerMessage.Define<string>(
                LogLevel.Information,
                EventIds.RemoveMessageFromQueue,
                "Removing message {MessageId} from queue after reporting error");

            BeforeNotificationHandler = LoggerMessage.Define<string, string, int, int, string>(
                LogLevel.Information,
                EventIds.NotificationHandler,
                "Begin-{NotificationHandler}: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId}");

            AfterNotificationHandler = LoggerMessage.Define<string, string, int, int, string>(
                LogLevel.Information,
                EventIds.NotificationHandler,
                "After-{NotificationHandler}: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId}");

            BeforeValidatingCertificate = LoggerMessage.Define<string, string, string, string, int, string>(
                LogLevel.Information,
                EventIds.CertificateValidation,
                "Before-CertificateValidation: {MessageFunction} Thumbprint: {Thumbprint} Subject: {Subject} Key Usage: {KeyUsage} Owner HerId: {OwnerHerId} MessageId: {MessageId}");
            AfterValidatingCertificate = LoggerMessage.Define<string, string, string, int, string, string>(
                LogLevel.Information,
                EventIds.CertificateValidation,
                "After-CertificateValidation: {MessageFunction} Thumbprint: {Thumbprint} Key Usage: {KeyUsage} Owner HerId: {OwnerHerId} MessageId: {MessageId} ResponseTime: {ResponseTimeMs} ms");

            BeforeEncryptingPayload = LoggerMessage.Define<string, string, string, int, int, string>(
                LogLevel.Information,
                EventIds.EncryptPayload,
                "Before-EncryptingPayload: {MessageFunction} SigningThumbprint: {SigningThumbprint} EncryptionThumbprint: {EncryptionThumbprint} FromHerId: {FromHerId} ToHerId: {ToHerId}  MessageId: {MessageId}");
            AfterEncryptingPayload = LoggerMessage.Define<string, int, int, string, string>(
                LogLevel.Information,
                EventIds.EncryptPayload,
                "After-EncryptingPayload: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId} Responsetime {ResponseTime} ms");

            BeforeDecryptingPayload = LoggerMessage.Define<string, string, string, int, int, string>(
                LogLevel.Information,
                EventIds.EncryptPayload,
                "Before-DecryptingPayload: {MessageFunction} SigningThumbprint: {SigningThumbprint} EncryptionThumbprint: {EncryptionThumbprint} FromHerId: {FromHerId} ToHerId: {ToHerId}  MessageId: {MessageId}");
            AfterDecryptingPayload = LoggerMessage.Define<string, int, int, string, string>(
                LogLevel.Information,
                EventIds.EncryptPayload,
                "After-DecryptingPayload: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId} Responsetime {ResponseTime} ms");

            BeforeFactoryPoolCreateMessage = LoggerMessage.Define<string, int, int, string>(
                LogLevel.Information,
                EventIds.FactoryPoolCreateEmptyMessage,
                "Before-FactoryPoolCreateMessage: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId}  MessageId: {MessageId}");
            AfterFactoryPoolCreateMessage = LoggerMessage.Define<string, int, int, string>(
                LogLevel.Information,
                EventIds.FactoryPoolCreateEmptyMessage,
                "After-FactoryPoolCreateMessage: {MessageFunction} FromHerId: {FromHerId} ToHerId: {ToHerId} MessageId: {MessageId}");

            RetryOperationInProgress = LoggerMessage.Define<string>(
                LogLevel.Warning,
                EventIds.RetryOperation,
                "ServiceBusOperation-Retry: {Message}");
        }

        public static void LogException(this ILogger logger, string message, Exception ex)
        {
            var sbe = ex as MessagingException;

            logger.LogCritical(sbe?.EventId ?? 0, ex, message);
        }
    }
}

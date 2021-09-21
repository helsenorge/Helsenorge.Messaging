/* 
 * Copyright (c) 2021, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// The AwaitRelease method in this class is spun up on a thread and will
    /// wait until we are close to Message.LockedUntil before we release the 
    /// message. As soon as we release the message is immediately available on 
    /// the queue.
    /// </summary>
    internal static class MessageReleaseThread
    {
        internal struct ThreadData
        {
            public IMessagingMessage Message;
            public ILogger Logger;
        }

        internal static void AwaitRelease(object data)
        {
            if (!(data is ThreadData threadData)) return;

            var message = threadData.Message;
            var logger = threadData.Logger;
            var messageId = message.MessageId;
            var messageFunction = message.MessageFunction;
            try
            {
                logger.LogInformation($"Start-MessageReleaseThread-AwaitRelease: MessageId: {messageId} MessageFunction: {messageFunction} DeliveryCount: {message.DeliveryCount}");

                var lockedUntil = message.LockedUntil;
                var millisecondsBuffer = 300;
                var millisecondsDelay = (int)lockedUntil.Subtract(DateTime.UtcNow).TotalMilliseconds - millisecondsBuffer;

                logger.LogDebug($"MessageReleaseThread-AwaitRelease: MessageId: {messageId}. Awaiting {millisecondsDelay} before releasing message.");

                if (millisecondsDelay > 0)
                {
                    Thread.Sleep(millisecondsDelay);
                }

                message.Modify(deliveryFailed: true);

                logger.LogDebug($"MessageReleaseThread-AwaitRelease: MessageId: {messageId}. Message has been released.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIds.MessageReleaseFailed, ex, $"MessageReleaseThread-AwaitRelease: MessageId: {messageId} Exception: {ex.Message}");
            }
            finally
            {
                message?.Dispose();
                logger.LogInformation($"End-MessageReleaseThread-AwaitRelease: MessageId: {messageId} MessageFunction: {messageFunction}");
            }
        }
    }
}

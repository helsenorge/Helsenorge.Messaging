/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Receivers;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus.Senders
{
    /// <summary>
    /// Handles synchronous sending
    /// </summary>
    internal class SynchronousSender : IMessagingNotification
    {
        private readonly ConcurrentDictionary<string, MessageEntry> _pendingSynchronousRequests = new ConcurrentDictionary<string, MessageEntry>();

        private class MessageEntry
        {
            public XDocument Payload { get; set; }
            /// <summary>
            /// time it was added to our list
            /// </summary>
            public DateTime AddedUtc { get; set; }
            public bool TimedOut { get; set; }
            public int ToHerId { get; set; }
            /// <summary>
            /// Time the reply was added to the queue
            /// </summary>
            public DateTime ReplyEnqueuedTimeUtc { get; set; }
            /// <summary>
            /// Time when the message should be sent
            /// </summary>
            public DateTime ScheduledSendTimeUtc { get; internal set; }
            /// <summary>
            /// The logical id of the message we are sending
            /// </summary>
            public string MessageId { get; internal set; }
        }
        private readonly ServiceBusCore _core;
    
        public SynchronousSender(ServiceBusCore core)
        {
            _core = core;
        }

        public Func<IncomingMessage, Task> OnSynchronousReplyMessageReceived { get; set; }

        public async Task<XDocument> SendAsync(ILogger logger, OutgoingMessage message)
        {
            await _core.Send(logger, message, QueueType.Synchronous, _core.Settings.Synchronous.FindReplyQueueForMe()).ConfigureAwait(false);
                        
            var listener = new SynchronousReplyListener(_core, logger, this);
            //stopwatch used to measure response time
            var stopwatch = Stopwatch.StartNew();
            var start = DateTime.UtcNow;
            var correlationId = message.MessageId;

            // the name of the game is to get messages off the reply queue as fast as possible since multiple threads may be waiting
            // therefore all threads take part in the processing. If we read a message from a queue and the process dies,
            // it's not a big deal since the caller gets an error message; we can then store retrieved messages in an in-memory cache

            // 1. check if reply message is in in-memory cache
            // 2. read message and validate it
            // 3. If nobody is no longer waiting for it, discard it
            // 4. If message is not the one we are waiting for, place it in the in-memory cache so another thread can read it

            _pendingSynchronousRequests.TryAdd(correlationId, new MessageEntry()
            {
                Payload = null,
                AddedUtc = DateTime.UtcNow,
                ToHerId = message.ToHerId,
                TimedOut = false,
                ScheduledSendTimeUtc = message.ScheduledSendTimeUtc,
                MessageId = message.MessageId
            });

            RemoveExpiredSynchronousEntries(logger);

            while (true)
            {
                var payload = CheckPendingEntries(correlationId);
                if (payload != null)
                {
                    // logs the response time in ms
                    stopwatch.Stop();
                    logger.LogResponseTime(message.MessageFunction, message.ToHerId, _core.Settings.MyHerId, message.MessageId, stopwatch.ElapsedMilliseconds.ToString());

                    return payload;
                }

                var incomingMessage = await listener.ReadAndProcessMessage(true).ConfigureAwait(false);
                if (incomingMessage != null)
                {
                    // see if this is a message we are waiting for
                    // if that is not the case, the information in incoming message enters the great beyond
                    MessageEntry messageEntry;
                    if (_pendingSynchronousRequests.TryGetValue(incomingMessage.CorrelationId, out messageEntry))
                    {
                        // if we receive a message a bit late, log it. 
                        if (messageEntry.TimedOut)
                        {
                            // enqueue time should be later than when we added it
                            logger.LogWarning(EventIds.SynchronousCallDelayed,
                                $"MessageId: {incomingMessage.CorrelationId} " +
                                $"was received after {(incomingMessage.EnqueuedTimeUtc - messageEntry.AddedUtc).TotalSeconds} seconds " +
                                $"from HerId: {messageEntry.ToHerId}. " +
                                $"Added at {messageEntry.AddedUtc} Enqueued at: {incomingMessage.EnqueuedTimeUtc}. " +
                                $"Sent message Scheduled Send Time at {messageEntry.ScheduledSendTimeUtc}");

                            _pendingSynchronousRequests.TryRemove(incomingMessage.CorrelationId, out messageEntry);
                        }
                        else
                        {
                            if (messageEntry.ToHerId != incomingMessage.FromHerId)
                            {
                                logger.LogCritical($"HerId of the sender of the reply message differ from the HerId the message was sent to.{Environment.NewLine}" +
                                    $"Message sent to HerId {messageEntry.ToHerId}, reply was from {incomingMessage.FromHerId}.{Environment.NewLine}" +
                                    $"MessageId, sent: {messageEntry.MessageId} reply: {incomingMessage.MessageId} " +
                                    $"CorrelationId: {incomingMessage.CorrelationId}");
                            }
                            // update information for existing entry
                            messageEntry.Payload = incomingMessage.Payload;
                            messageEntry.ReplyEnqueuedTimeUtc = incomingMessage.EnqueuedTimeUtc;
                        }
                    }
                }

                if (DateTime.UtcNow <= (start + _core.Settings.Synchronous.CallTimeout)) continue;

                // if the request times out, we mark it. That way we can report if the message is received right after a timeout
                if (_pendingSynchronousRequests.TryGetValue(correlationId, out MessageEntry entry))
                {
                    entry.TimedOut = true;
                }
                var error = $"Synchronous call {message.MessageId} timed out against HerId: {message.ToHerId}.";
                logger.LogTimeoutError(message.MessageFunction, message.MessageId, message.ToHerId);
                // logs the response time before throwing exception
                stopwatch.Stop();
                logger.LogResponseTime(message.MessageFunction, message.ToHerId, 0, message.MessageId, stopwatch.ElapsedMilliseconds.ToString());

                throw new MessagingException(error)
                {
                    EventId = EventIds.SynchronousCallTimeout
                };
            }
        }
        private XDocument CheckPendingEntries(string correlationId)
        {
            if (_pendingSynchronousRequests.TryGetValue(correlationId, out MessageEntry entry) != true) return null;
            if (entry.Payload == null) return null;

            var response = entry.Payload;
            // since we have data, remove the entry
            _pendingSynchronousRequests.TryRemove(correlationId, out entry);
            return response;
        }
        private void RemoveExpiredSynchronousEntries(ILogger logger)
        {
            var limit = DateTime.UtcNow.AddSeconds(-10) - _core.Settings.Synchronous.CallTimeout;

            var array = (
                from kvp in _pendingSynchronousRequests
                where kvp.Value.AddedUtc < limit
                select kvp.Key).ToArray();

            foreach (var item in array)
            {
                if (_pendingSynchronousRequests.TryRemove(item, out MessageEntry entry) != true) continue;
                if (entry.TimedOut != true) continue;
                
                // if we never received a reply, we cannot calculate the time it took
                // enqueued time should be later than when we added it
                if (entry.ReplyEnqueuedTimeUtc > DateTime.MinValue)
                {
                    logger.LogWarning(EventIds.SynchronousCallDelayed, 
                        "MessageId: {1} was received after {2} seconds from HerId: {3}. Added at {4} Enqueued at: {5}",
                        item,
                        (entry.ReplyEnqueuedTimeUtc - entry.AddedUtc).TotalSeconds,
                        entry.ToHerId,
                        entry.AddedUtc,
                        entry.ReplyEnqueuedTimeUtc);
                }
            }
        }

        public Task NotifyAsynchronousMessageReceived(IncomingMessage message)
        {
            return Task.CompletedTask;
        }

        public Task NotifyAsynchronousMessageReceivedStarting(IncomingMessage message)
        {
            return Task.CompletedTask;
        }

        public Task NotifyAsynchronousMessageReceivedCompleted(IncomingMessage message)
        {
            return Task.CompletedTask;
        }

        public Task NotifyErrorMessageReceived(IMessagingMessage message)
        {
            return Task.CompletedTask;
        }

        public Task NotifyErrorMessageReceivedStarting(IncomingMessage message)
        {
            return Task.CompletedTask;
        }

        public async Task<XDocument> NotifySynchronousMessageReceived(IncomingMessage message)
        {
            if (OnSynchronousReplyMessageReceived != null)
            {
                await OnSynchronousReplyMessageReceived.Invoke(message).ConfigureAwait(false);
            }
            return message.Payload;
        }

        public Task NotifySynchronousMessageReceivedCompleted(IncomingMessage message)
        {
            return Task.CompletedTask;
        }

        public Task NotifySynchronousMessageReceivedStarting(IncomingMessage message)
        {
            return Task.CompletedTask;
        }

        public Task NotifyUnhandledException(IMessagingMessage message, Exception ex)
        {
            return Task.CompletedTask;
        }

        public Task NotifyHandledException(IMessagingMessage message, Exception ex)
        {
            return Task.CompletedTask;
        }
    }
}

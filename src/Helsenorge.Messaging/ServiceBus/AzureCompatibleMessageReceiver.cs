using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Types;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// Fault-tolerant Azure-compatible message receiving mechanism.
    /// </summary>
    internal class AzureCompatibleMessageReceiver
    {
        private readonly ReceiverLink _receiver;
        //private readonly RequestResponseAmqpLink _requestResponseAmqpLink;

        public AzureCompatibleMessageReceiver(Session session, string id)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _receiver = new ReceiverLink(session, $"receiver-link-name-{Guid.NewGuid()}", id);
            //_requestResponseAmqpLink = new RequestResponseAmqpLink(session.Connection, id);
        }

        public async Task<ServiceBusMessage> ReceiveAsync(TimeSpan timeout)
        {
            var message = await _receiver.ReceiveAsync(timeout);
            return message != null ? new ServiceBusMessage(message) : null;
        }

        public Task CompleteAsync(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _receiver.Accept(message);
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _receiver.Reject(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Performs Azure-specific Renew Lock operation (see https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-request-response?redirectedfrom=MSDN).
        /// See also https://msdn.microsoft.com/en-us/library/mt727956.aspx.
        /// </summary>
        /// <param name="lockTokenGuid">Message delivery tag GUID</param>
        /// <returns>New locked until time (UTC)</returns>
        public async Task<DateTime> RenewLockAsync(Guid lockTokenGuid, string partitionKey = null, TimeSpan? serverTimeout = null)
        {
            // FIXME: on-premise SB uses WCF for management commands
            throw new NotSupportedException("Renew lock not supported");

            //var renewLockMessage = new Message
            //{
            //    ApplicationProperties = new ApplicationProperties
            //    {
            //        ["operation"] = "com.microsoft:renew-lock",
            //        ["com.microsoft:server-timeout"] = (serverTimeout ?? TimeSpan.FromMinutes(1)).TotalMilliseconds // default operation timeout in Microsoft.Azure.ServiceBus library. TODO: get it from connection string?
            //    },
            //    BodySection = new AmqpValue { Value = new Map { { "lock-tokens", new[] { lockTokenGuid } } } }
            //};

            //if (!string.IsNullOrEmpty(partitionKey))
            //{
            //    renewLockMessage.MessageAnnotations[ServiceBusMessage.PartitionKeySymbol] = partitionKey;
            //}

            //var response = await _requestResponseAmqpLink.SendAsync(renewLockMessage);
            //var statusCode = (int?)response?.ApplicationProperties?["statusCode"];
            //if (!statusCode.HasValue || statusCode.Value != (int)HttpStatusCode.OK)
            //{
            //    return DateTime.MinValue;
            //}

            //var body = response.GetBody<Dictionary<string, DateTime[]>>();
            //return body.Count > 0 ? body["expirations"][0] : DateTime.MinValue;
        }

        public async Task CloseAsync()
        {
            await _receiver.CloseAsync();
            //await _requestResponseAmqpLink.CloseAsync();
        }
    }
}

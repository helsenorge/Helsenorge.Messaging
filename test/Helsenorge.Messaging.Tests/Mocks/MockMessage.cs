/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using System.IO;
using System.Xml.Linq;
using System.Globalization;

namespace Helsenorge.Messaging.Tests.Mocks
{
    class MockMessage : IAmqpMessage
    {
        private Stream _stream;
        private int _deliveryCount = 0;

        public MockMessage(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            _stream = stream;
            Properties = new Dictionary<string, object>();
        }

        public MockMessage(XDocument doc)
        {
            var ms = new MemoryStream();
            doc.Save(ms);
            ms.Position = 0;
            _stream = ms;
            Properties = new Dictionary<string, object>();
        }

        public int FromHerId { get; set; }
        public int ToHerId { get; set; }
        public DateTime ApplicationTimestamp { get; set; }
        public string CpaId { get; set; }
        public DateTime EnqueuedTimeUtc { get; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; } = DateTime.Now.AddMinutes(5);
        public IDictionary<string, object> Properties { get; private set; }
        public long Size => _stream.Length;
        public string ContentType { get; set; }
        public string CorrelationId { get; set; }
        public string MessageFunction { get; set; }
        public string MessageId { get; set; }
        public string GroupId { get; set; }
        public string ReplyTo { get; set; }
        public TimeSpan TimeToLive { get; set; }
        public string To { get; set; }

        public Guid LockToken => Guid.Empty;
        public void Complete()
        {
            Queue.Remove(this);
        }

        public Task CompleteAsync()
        {
            Queue.Remove(this);
            return Task.CompletedTask;
        }


        public void Release()
        {
        }

        public Task RelaseAsync()
        {
            return Task.CompletedTask;
        }

        public void Reject()
        {
        }

        public Task RejectAsync()
        {
            return Task.CompletedTask;
        }

        public void DeadLetter()
        {
            Queue.Remove(this);
            DeadLetterQueue.Add(this);
        }

        public Task DeadLetterAsync()
        {
            DeadLetter();
            return Task.CompletedTask;
        }

        public void Modify(bool deliveryFailed, bool undeliverableHere = false)
        {
            if(deliveryFailed)
            {
                _deliveryCount++;
            }
        }

        public Task ModifyAsync(bool deliveryFailed, bool undeliverableHere = false)
        {
            return Task.CompletedTask;
        }

        public List<IAmqpMessage> Queue { get; set; }

        public List<IAmqpMessage> DeadLetterQueue { get; set; }

        public IAmqpMessage Clone(bool includePayload = true)
        {

            return new MockMessage(_stream)
            {
                MessageFunction = MessageFunction,
                MessageId =  MessageId,
                ToHerId = ToHerId,
                ContentType =  ContentType,
                ReplyTo = ReplyTo,
                CorrelationId = CorrelationId,
                Queue = Queue,
                DeadLetterQueue = DeadLetterQueue,
                ApplicationTimestamp = ApplicationTimestamp,
                CpaId = CpaId,
                FromHerId = FromHerId,
                Properties = Properties,
                TimeToLive = TimeToLive,
                To = To
            };
        }

        public object OriginalObject { get; }

        public int DeliveryCount => _deliveryCount;

        public DateTime LockedUntil => DateTime.UtcNow.AddSeconds(60);

        public Stream GetBody()
        {
            return _stream;
        }

        public void SetBody(Stream stream)
        {  
            _stream = stream;  
        }

        public void Dispose()
        {
        }

        public void AddDetailsToException(Exception ex)
        {
        
        }

        public void SetApplicationPropertyValue(string key, string value)
        {
            Properties[key] = value;
        }

        public void SetApplicationPropertyValue(string key, DateTime value)
        {
            Properties[key] = value.ToString(StringFormatConstants.IsoDateTime, DateTimeFormatInfo.InvariantInfo);
        }

        public void SetApplicationPropertyValue(string key, int value)
        {
            Properties[key] = value.ToString(CultureInfo.InvariantCulture);
        }
    }
}

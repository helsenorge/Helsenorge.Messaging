/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// AMPQNetLite-based implementation which is trying to mimic Microsoft Azure ServiceBus specifics
    /// described here https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-protocol-guide
    /// and here https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-request-response.
    /// The original implementation was removed because it fails to support .NET Standard library.
    /// (Microsoft.Azure.ServiceBus does by fact support it but it doesn't allow to authenticate using plain password).
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ServiceBusMessage : IMessagingMessage
    {
        private readonly Message _implementation;

        public static readonly Symbol EnqueuedTimeSymbol = new Symbol("x-opt-enqueued-time");
        public static readonly Symbol ScheduledEnqueueTimeSymbol = new Symbol("x-opt-scheduled-enqueue-time");
        public static readonly Symbol LockedUntilSymbol = new Symbol("x-opt-locked-until");
        public static readonly Symbol PartitionKeySymbol = new Symbol("x-opt-partition-key");
        public static readonly Symbol SequenceNumberSymbol = new Symbol("x-opt-sequence-number");

        public Func<Task> CompleteAction { get; set; }
        public Func<Task> RejectAction { get; set; }
        public Func<Task> ReleaseAction { get; set; }
        public Func<Task> DeadLetterAction { get; set; }
        public Func<Task<DateTime>> RenewLockAction { get; set; }

        public ServiceBusMessage(Message implementation)
        {
            _implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
        }

        public int FromHerId
        {
            [DebuggerStepThrough]
            get => GetValue(ServiceBusCore.FromHerIdHeaderKey, 0);
            [DebuggerStepThrough]
            set => SetApplicationProperty(ServiceBusCore.FromHerIdHeaderKey, value);
        }
        public int ToHerId
        {
            [DebuggerStepThrough]
            get => GetValue(ServiceBusCore.ToHerIdHeaderKey, 0);
            [DebuggerStepThrough]
            set => SetApplicationProperty(ServiceBusCore.ToHerIdHeaderKey, value);
        }
        public DateTime ApplicationTimestamp
        {
            get => GetValue(ServiceBusCore.ApplicationTimestampHeaderKey, DateTime.MinValue);
            [DebuggerStepThrough]
            set => SetApplicationProperty(ServiceBusCore.ApplicationTimestampHeaderKey, value);
        }
        public string CpaId
        {
            [DebuggerStepThrough]
            get => GetValue(ServiceBusCore.CpaIdHeaderKey, string.Empty);
            [DebuggerStepThrough]
            set => SetApplicationProperty(ServiceBusCore.CpaIdHeaderKey, value);
        }
        public object OriginalObject => _implementation;

        public DateTime EnqueuedTimeUtc => _implementation.MessageAnnotations?.Map.ContainsKey(EnqueuedTimeSymbol) == true
        ? (DateTime)_implementation.MessageAnnotations[EnqueuedTimeSymbol]
        : DateTime.MaxValue;

        public DateTime ExpiresAtUtc
        {
            get
            {
                if (TimeToLive >= DateTime.MaxValue.Subtract(EnqueuedTimeUtc))
                {
                    return DateTime.MaxValue;
                }
                return EnqueuedTimeUtc.Add(TimeToLive);
            }
        }

        public IDictionary<string, object> Properties
        {
            get
            {
                var applicationProperties = GetApplicationProperties();
                return applicationProperties.Map.Keys
                    .ToDictionary(key => key.ToString(),
                        key => applicationProperties[key]);
            }
            set
            {
                var applicationProperties = GetApplicationProperties();
                foreach (var entry in value)
                {
                    applicationProperties[entry.Key] = entry.Value;
                }
            }
        }

        public long Size
        {
            get
            {
                if (_implementation.BodySection is Data data)
                {
                    return data.Binary.Length;
                }
                return 0;
            }
        }

        private Properties GetMessageProperties()
        {
            if (_implementation.Properties == null)
            {
                _implementation.Properties = new Properties();
            }
            return _implementation.Properties;
        }

        private ApplicationProperties GetApplicationProperties()
        {
            if (_implementation.ApplicationProperties == null)
            {
                _implementation.ApplicationProperties = new ApplicationProperties();
            }
            return _implementation.ApplicationProperties;
        }

        public string ContentType
        {
            [DebuggerStepThrough]
            get => GetMessageProperties().ContentType;
            [DebuggerStepThrough]
            set => GetMessageProperties().ContentType = value;
        }
        public string CorrelationId
        {
            [DebuggerStepThrough]
            get => ValidateIdentifier(GetMessageProperties().GetCorrelationId())?.ToString();
            [DebuggerStepThrough]
            set => GetMessageProperties().CorrelationId = value;
        }
        public string MessageFunction
        {
            [DebuggerStepThrough]
            get => GetMessageProperties().Subject;
            [DebuggerStepThrough]
            set => GetMessageProperties().Subject = value;
        }
        public string MessageId
        {
            [DebuggerStepThrough]
            get => ValidateIdentifier(GetMessageProperties().GetMessageId())?.ToString();

            [DebuggerStepThrough]
            set => GetMessageProperties().MessageId = value;
        }
        public string ReplyTo
        {
            [DebuggerStepThrough]
            get => GetMessageProperties().ReplyTo;
            [DebuggerStepThrough]
            set => GetMessageProperties().ReplyTo = value;
        }
        public DateTime ScheduledEnqueueTimeUtc
        {
            // FIXME: DateTime.Now should be DateTime.UtcNow
            [DebuggerStepThrough]
            get => _implementation.MessageAnnotations?.Map.ContainsKey(ScheduledEnqueueTimeSymbol) == true
                ? (DateTime)_implementation.MessageAnnotations[ScheduledEnqueueTimeSymbol]
            : DateTime.Now;

            [DebuggerStepThrough]
            set
            {
                if (_implementation.MessageAnnotations == null) _implementation.MessageAnnotations = new MessageAnnotations();
                _implementation.MessageAnnotations[ScheduledEnqueueTimeSymbol] = value;
            }
        }
        public TimeSpan TimeToLive
        {
            [DebuggerStepThrough]
            get => _implementation.Header == null ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(_implementation.Header.Ttl);
            [DebuggerStepThrough]
            set
            {
                if (value > TimeSpan.Zero)
                {
                    if (_implementation.Header == null) _implementation.Header = new Header();
                    _implementation.Header.Ttl = (uint)value.TotalMilliseconds;
                }
            }
        }
        public string To
        {
            [DebuggerStepThrough]
            get => GetMessageProperties().To;
            [DebuggerStepThrough]
            set => GetMessageProperties().To = value;
        }

        public int DeliveryCount
        {
            [DebuggerStepThrough]
            get => _implementation.Header == null ? (int)uint.MinValue : (int)_implementation.Header.DeliveryCount;
        }

        [DebuggerStepThrough]
        public IMessagingMessage Clone(bool includePayload = true)
        {
            var clone = new Message
            {
                Header = _implementation.Header,
                DeliveryAnnotations = _implementation.DeliveryAnnotations,
                MessageAnnotations = _implementation.MessageAnnotations,
                Properties = _implementation.Properties,
                ApplicationProperties = _implementation.ApplicationProperties,
                Footer = _implementation.Footer
            };

            if (includePayload && _implementation.Body is byte[])
            {
                clone.BodySection = new Data
                {
                    Binary = _implementation.GetBody<byte[]>().ToArray()
                };
            }
            else if (includePayload && _implementation.Body is Stream payloadStream)
            {
                using (var memoryStream = new MemoryStream())
                {
                    payloadStream.CopyTo(memoryStream);
                    clone.BodySection = new Data
                    {
                        Binary = memoryStream.ToArray()
                    };
                }
            }

            return new ServiceBusMessage(clone)
            {
                CompleteAction = CompleteAction,
                RejectAction = RejectAction,
                ReleaseAction = ReleaseAction,
                DeadLetterAction = DeadLetterAction,
                RenewLockAction = RenewLockAction
            };
        }

        public void Complete() => CompleteAction.Invoke().GetAwaiter().GetResult();

        public async Task CompleteAsync() => await CompleteAction.Invoke().ConfigureAwait(false);

        public void Reject() => ReleaseAction.Invoke().GetAwaiter().GetResult();

        public async Task RejectAsync() => await ReleaseAction.Invoke().ConfigureAwait(false);

        public void Release() => ReleaseAction.Invoke().GetAwaiter().GetResult();

        public async Task RelaseAsync() =>  await ReleaseAction.Invoke().ConfigureAwait(false);

        public void DeadLetter() => DeadLetterAction.Invoke().GetAwaiter().GetResult();

        public async Task DeadLetterAsync() => await DeadLetterAction.Invoke().ConfigureAwait(false);

        [DebuggerStepThrough]
        public void Dispose() => _implementation.Dispose();

        public Stream GetBody()
        {
            if (_implementation.Body is byte[])
                return new MemoryStream(_implementation.GetBody<byte[]>());
            else if (_implementation.Body is Stream)
                return _implementation.GetBody<Stream>();

            return null;
        }

        [DebuggerStepThrough]
        public override string ToString() => string.Format(CultureInfo.CurrentCulture, "{{MessageId:{0}}}", MessageId);

        public DateTime LockedUntil => (DateTime?)_implementation.MessageAnnotations?[LockedUntilSymbol]
                                       ?? DateTime.MinValue;

        public void RenewLock()
        {
            var task = RenewLockAction.Invoke();
            var lockedUntilUtc = task?.Result ?? DateTime.MinValue;
            _implementation.MessageAnnotations[LockedUntilSymbol] = lockedUntilUtc;
        }

        public void AddDetailsToException(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            var data = new Dictionary<string, object>
                {
                    { "BrokeredMessageId", MessageId },
                    { "CorrelationId", CorrelationId },
                    { "Label", MessageFunction },
                    { "To", To },
                    { "ReplyTo", ReplyTo },
                };
            foreach (var key in data.Keys)
            {
                try
                {
                    ex.Data.Add(key, data[key]);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch (ArgumentException) // ignore duplicate keys
                {
                }
            }
        }

        public void SetApplicationProperty(string key, string value) => GetApplicationProperties()[key] = value;
        public void SetApplicationProperty(string key, DateTime value) => GetApplicationProperties()[key] = value.ToString(StringFormatConstants.IsoDateTime, DateTimeFormatInfo.InvariantInfo);
        public void SetApplicationProperty(string key, int value) => GetApplicationProperties()[key] = value.ToString(CultureInfo.InvariantCulture);

        private string GetValue(string key, string value) => GetApplicationProperties()?.Map.ContainsKey(key) == true
            ? GetApplicationProperties()[key].ToString()
            : value;

        private int GetValue(string key, int value) => GetApplicationProperties()?.Map.ContainsKey(key) == true
            ? int.Parse(GetApplicationProperties()[key].ToString())
            : value;
        private DateTime GetValue(string key, DateTime value) => GetApplicationProperties()?.Map.ContainsKey(key) == true
            ? DateTime.Parse(GetApplicationProperties()[key].ToString(), CultureInfo.InvariantCulture)
            : value;
        
        static object ValidateIdentifier(object id)
        {
            if (id != null && !(id is string || id is Guid))
            {
                throw new UnexpectedMessageIdentifierTypeException($"And identifier of type {id.GetType().FullName} is not supported.");
            }

            return id;
        }
    }
}

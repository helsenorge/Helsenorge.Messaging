/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.Amqp.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Amqp
{
    [ExcludeFromCodeCoverage]
    internal class AmqpMessage : IAmqpMessage
    {
        private readonly Message _implementation;

        public static readonly Symbol LockedUntilSymbol = new Symbol("x-opt-locked-until");

        internal Action CompleteAction { get; set; }
        internal Func<Task> CompleteActionAsync { get; set; }
        internal Action RejectAction { get; set; }
        internal Func<Task> RejectActionAsync { get; set; }
        internal Action ReleaseAction { get; set; }
        internal Func<Task> ReleaseActionAsync { get; set; }
        internal Action DeadLetterAction { get; set; }
        internal Func<Task> DeadLetterActionAsync { get; set; }
        internal Action<bool, bool> ModifyAction { get; set; }
        internal Func<bool, bool, Task> ModifyActionAsync { get; set; }

        public AmqpMessage(Message implementation)
        {
            _implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
        }

        public int FromHerId
        {
            [DebuggerStepThrough]
            get => GetApplicationPropertyValue(AmqpCore.FromHerIdHeaderKey, 0);
            [DebuggerStepThrough]
            set => SetApplicationPropertyValue(AmqpCore.FromHerIdHeaderKey, value);
        }
        public int ToHerId
        {
            [DebuggerStepThrough]
            get => GetApplicationPropertyValue(AmqpCore.ToHerIdHeaderKey, 0);
            [DebuggerStepThrough]
            set => SetApplicationPropertyValue(AmqpCore.ToHerIdHeaderKey, value);
        }
        public DateTime ApplicationTimestamp
        {
            get => GetApplicationPropertyValue(AmqpCore.ApplicationTimestampHeaderKey, DateTime.MinValue);
            [DebuggerStepThrough]
            set => SetApplicationPropertyValue(AmqpCore.ApplicationTimestampHeaderKey, value);
        }
        public string CpaId
        {
            [DebuggerStepThrough]
            get => GetApplicationPropertyValue(AmqpCore.CpaIdHeaderKey, string.Empty);
            [DebuggerStepThrough]
            set => SetApplicationPropertyValue(AmqpCore.CpaIdHeaderKey, value);
        }
        public object OriginalObject => _implementation;

        public DateTime EnqueuedTimeUtc
        {
            [DebuggerStepThrough]
            get => GetApplicationPropertyValue(AmqpCore.EnqueuedTimeUtc, DateTime.MaxValue);
        }

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
        public string GroupId
        {
            [DebuggerStepThrough]
            get => GetMessageProperties().GroupId;

            [DebuggerStepThrough]
            set => GetMessageProperties().GroupId = value;
        }

        public string ReplyTo
        {
            [DebuggerStepThrough]
            get => GetMessageProperties().ReplyTo;
            [DebuggerStepThrough]
            set => GetMessageProperties().ReplyTo = value;
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

        public bool FirstAcquirer
        {
            [DebuggerStepThrough]
            get => _implementation.Header == null ? false : _implementation.Header.FirstAcquirer;
        }

        public int DeliveryCount
        {
            [DebuggerStepThrough]
            get => _implementation.Header == null ? (int)uint.MinValue : (int)_implementation.Header.DeliveryCount;
        }

        [DebuggerStepThrough]
        public IAmqpMessage Clone(bool includePayload = true)
        {
            var clone = new Message();

            if (_implementation.Header != null)
            {
                clone.Header = new Header
                {
                    DeliveryCount = _implementation.Header.DeliveryCount,
                    Durable = _implementation.Header.Durable,
                    FirstAcquirer = _implementation.Header.FirstAcquirer,
                    Priority = _implementation.Header.Priority,
                    Ttl = _implementation.Header.Ttl,
                };
            }

            if (_implementation.DeliveryAnnotations != null)
            {
                clone.DeliveryAnnotations = new DeliveryAnnotations();
                foreach (var key in _implementation.DeliveryAnnotations.Map.Keys)
                    clone.DeliveryAnnotations.Map.Add(key, _implementation.DeliveryAnnotations.Map[key]);
            }

            if (_implementation.MessageAnnotations != null)
            {
                clone.MessageAnnotations = new MessageAnnotations();
                foreach (var key in _implementation.MessageAnnotations.Map.Keys)
                    clone.MessageAnnotations.Map.Add(key, _implementation.MessageAnnotations.Map[key]);
            }

            if (_implementation.Properties != null)
            {
                clone.Properties = new Properties
                {
                    Subject = _implementation.Properties.Subject,
                    To = _implementation.Properties.To,
                    ContentEncoding = _implementation.Properties.ContentEncoding,
                    ContentType = _implementation.Properties.ContentType,
                    CorrelationId = _implementation.Properties.CorrelationId,
                    CreationTime = _implementation.Properties.CreationTime,
                    GroupId = _implementation.Properties.GroupId,
                    GroupSequence = _implementation.Properties.GroupSequence,
                    MessageId = _implementation.Properties.MessageId,
                    ReplyTo = _implementation.Properties.ReplyTo,
                    AbsoluteExpiryTime = _implementation.Properties.AbsoluteExpiryTime,
                    ReplyToGroupId = _implementation.Properties.ReplyToGroupId,
                };
            }

            if (_implementation.ApplicationProperties != null)
            {
                clone.ApplicationProperties = new ApplicationProperties();
                foreach (var key in _implementation.ApplicationProperties.Map.Keys)
                    clone.ApplicationProperties.Map.Add(key, _implementation.ApplicationProperties.Map[key]);
            }

            if (_implementation.Footer != null)
            {
                clone.Footer = new Footer();
                foreach (var key in _implementation.Footer.Map.Keys)
                    clone.Footer.Map.Add(key, _implementation.Footer.Map[key]);
            }

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

            return new AmqpMessage(clone)
            {
                CompleteAction = CompleteAction,
                CompleteActionAsync = CompleteActionAsync,
                RejectAction = RejectAction,
                RejectActionAsync = RejectActionAsync,
                ReleaseAction = ReleaseAction,
                ReleaseActionAsync = ReleaseActionAsync,
                DeadLetterAction = DeadLetterAction,
                DeadLetterActionAsync = DeadLetterActionAsync,
                ModifyAction = ModifyAction,
                ModifyActionAsync = ModifyActionAsync,
            };
        }

        public void Complete() => CompleteAction.Invoke();

        public async Task CompleteAsync() => await CompleteActionAsync.Invoke().ConfigureAwait(false);

        public void Reject() => RejectAction.Invoke();

        public async Task RejectAsync() => await RejectActionAsync.Invoke().ConfigureAwait(false);

        public void Release() => ReleaseAction.Invoke();

        public async Task RelaseAsync() =>  await ReleaseActionAsync.Invoke().ConfigureAwait(false);

        public void Modify(bool deliveryFailed, bool undeliverableHere = false) => ModifyAction.Invoke(deliveryFailed, undeliverableHere);

        public async Task ModifyAsync(bool deliveryFailed, bool undeliverableHere = false) => await ModifyActionAsync.Invoke(deliveryFailed, undeliverableHere).ConfigureAwait(false);

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

        public void SetApplicationPropertyValue(string key, string value) => GetApplicationProperties()[key] = value;
        public void SetApplicationPropertyValue(string key, DateTime value) => GetApplicationProperties()[key] = value.ToString(StringFormatConstants.IsoDateTime, DateTimeFormatInfo.InvariantInfo);
        public void SetApplicationPropertyValue(string key, int value) => GetApplicationProperties()[key] = value.ToString(CultureInfo.InvariantCulture);

        private string GetApplicationPropertyValue(string key, string value) => GetApplicationProperties()?.Map.ContainsKey(key) == true
            ? GetApplicationProperties()[key].ToString()
            : value;

        private int GetApplicationPropertyValue(string key, int value) => GetApplicationProperties()?.Map.ContainsKey(key) == true
            ? int.Parse(GetApplicationProperties()[key].ToString())
            : value;
        private DateTime GetApplicationPropertyValue(string key, DateTime value)
        {
            if (GetApplicationProperties()?.Map.ContainsKey(key) == false)
                return value;

            return DateTime.TryParse(GetApplicationProperties()[key].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime)
                    ? parsedDateTime
                    : value;
        }

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Microsoft.ServiceBus.Messaging;

namespace Helsenorge.Messaging.ServiceBus
{
	[ExcludeFromCodeCoverage] // Azure service bus implementation
	internal class ServiceBusMessage : IMessagingMessage
	{
		private readonly BrokeredMessage _implementation;

		public ServiceBusMessage(BrokeredMessage implementation)
		{
			if (implementation == null) throw new ArgumentNullException(nameof(implementation));
			_implementation = implementation;
		}
		public int FromHerId
		{
			[DebuggerStepThrough] get { return GetValue(ServiceBusCore.FromHerIdHeaderKey, 0); }
			[DebuggerStepThrough] set { SetValue(ServiceBusCore.FromHerIdHeaderKey, value); }
		}
		public int ToHerId
		{
			[DebuggerStepThrough] get { return GetValue(ServiceBusCore.ToHerIdHeaderKey, 0); }
			[DebuggerStepThrough] set { SetValue(ServiceBusCore.ToHerIdHeaderKey, value); }
		}
		public DateTime ApplicationTimestamp
		{
			[DebuggerStepThrough] get { return GetValue(ServiceBusCore.ApplicationTimestampHeaderKey, DateTime.MinValue); }
			[DebuggerStepThrough] set { SetValue(ServiceBusCore.ApplicationTimestampHeaderKey, value); }
		}
		public string CpaId
		{
			[DebuggerStepThrough] get { return GetValue(ServiceBusCore.CpaIdHeaderKey, string.Empty); }
			[DebuggerStepThrough] set { SetValue(ServiceBusCore.CpaIdHeaderKey, value); }
		}
		public object OriginalObject => _implementation;
		public DateTime EnqueuedTimeUtc => _implementation.EnqueuedTimeUtc;
		public DateTime ExpiresAtUtc => _implementation.ExpiresAtUtc;
		public IDictionary<string, object> Properties => _implementation.Properties;
		public long Size => _implementation.Size;
		public string ContentType 
		{
			[DebuggerStepThrough] get { return _implementation.ContentType; }
			[DebuggerStepThrough] set { _implementation.ContentType = value; } 
		}
		public string CorrelationId
		{
			[DebuggerStepThrough] get { return _implementation.CorrelationId; }
			[DebuggerStepThrough] set { _implementation.CorrelationId = value; }
		}
		public string MessageFunction
		{
			[DebuggerStepThrough] get { return _implementation.Label; }
			[DebuggerStepThrough] set { _implementation.Label = value; }
		}
		public string MessageId
		{
			[DebuggerStepThrough] get { return _implementation.MessageId; }
			[DebuggerStepThrough] set { _implementation.MessageId = value; }
		}
		public string ReplyTo
		{
			[DebuggerStepThrough] get { return _implementation.ReplyTo; }
			[DebuggerStepThrough] set { _implementation.ReplyTo = value; }
		}
		public DateTime ScheduledEnqueueTimeUtc
		{
			[DebuggerStepThrough] get { return _implementation.ScheduledEnqueueTimeUtc; }
			[DebuggerStepThrough] set { _implementation.ScheduledEnqueueTimeUtc = value; }
		}
		public TimeSpan TimeToLive
		{
			[DebuggerStepThrough] get { return _implementation.TimeToLive; }
            [DebuggerStepThrough] set { if(value > TimeSpan.Zero) _implementation.TimeToLive = value; }
		}
		public string To
		{
			[DebuggerStepThrough] get { return _implementation.To; }
			[DebuggerStepThrough] set { _implementation.To = value; }
		}
		[DebuggerStepThrough]
		public IMessagingMessage Clone() => new ServiceBusMessage(_implementation.Clone());
		[DebuggerStepThrough]
		public void Complete() => _implementation.Complete();
		[DebuggerStepThrough]
		public Task CompleteAsync() => _implementation.CompleteAsync();
		[DebuggerStepThrough]
		public void Dispose() => _implementation.Dispose();
		[DebuggerStepThrough]
		public Stream GetBody() => _implementation.GetBody<Stream>();
		[DebuggerStepThrough]
		public override string ToString() => _implementation.ToString();
        [DebuggerStepThrough]
        public void RenewLock() => _implementation.RenewLock();

        public void AddDetailsToException(Exception ex)
		{
			if (ex == null) throw new ArgumentNullException(nameof(ex));

			var data = new Dictionary<string, object>
				{
					{ "BrokeredMessageId", _implementation.MessageId },
					{ "CorrelationId", _implementation.CorrelationId },
					{ "Label", _implementation.Label },
					{ "To", _implementation.To },
					{ "ReplyTo", _implementation.ReplyTo },
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

		private void SetValue(string key, string value) => _implementation.Properties[key] = value;
		private void SetValue(string key, DateTime value) => _implementation.Properties[key] = value.ToString(StringFormatConstants.IsoDateTime, DateTimeFormatInfo.InvariantInfo);
		private void SetValue(string key, int value) => _implementation.Properties[key] = value.ToString(CultureInfo.InvariantCulture);

		private string GetValue(string key, string value) => _implementation.Properties.ContainsKey(key) ? _implementation.Properties[key].ToString() : value;
		private int GetValue(string key, int value) => _implementation.Properties.ContainsKey(key) ? int.Parse(_implementation.Properties[key].ToString()) : value;
		private DateTime GetValue(string key, DateTime value) => _implementation.Properties.ContainsKey(key) ? DateTime.Parse(_implementation.Properties[key].ToString(), CultureInfo.InvariantCulture) : value;
	}
}

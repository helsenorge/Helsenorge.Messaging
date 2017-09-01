using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Microsoft.ServiceBus.Messaging;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Helsenorge.Messaging.ServiceBus
{
	[ExcludeFromCodeCoverage] // Azure service bus implementation
	internal class ServiceBusSender : IMessagingSender
	{
		readonly MessageSender _implementation;
		public ServiceBusSender(MessageSender implementation)
		{
			if (implementation == null) throw new ArgumentNullException(nameof(implementation));
			_implementation = implementation;
		}
		public async Task SendAsync(IMessagingMessage message)
		{
			if (message == null) throw new ArgumentNullException(nameof(message));
            
			var brokeredMessage = message.OriginalObject as BrokeredMessage;
			if(brokeredMessage == null) throw new InvalidOperationException("OriginalObject is not a Brokered message");

			await _implementation.SendAsync(brokeredMessage).ConfigureAwait(false);
		}
		bool ICachedMessagingEntity.IsClosed => _implementation.IsClosed;
		void ICachedMessagingEntity.Close() => _implementation.Close();
	}
}

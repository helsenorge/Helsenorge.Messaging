using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Helsenorge.Messaging.Abstractions;
using Microsoft.ServiceBus.Messaging;

namespace Helsenorge.Messaging.ServiceBus
{
	[ExcludeFromCodeCoverage] // Azure service bus implementation
	internal class ServiceBusFactory : IMessagingFactory
	{
		private readonly MessagingFactory _implementation;

		public ServiceBusFactory(MessagingFactory implementation)
		{
			if (implementation == null) throw new ArgumentNullException(nameof(implementation));
			_implementation = implementation;
		}
		public IMessagingReceiver CreateMessageReceiver(string id)
		{
			return new ServiceBusReceiver(_implementation.CreateMessageReceiver(id, ReceiveMode.PeekLock));
		}
		public IMessagingSender CreateMessageSender(string id)
		{
			return new ServiceBusSender(_implementation.CreateMessageSender(id));
		}
		bool ICachedMessagingEntity.IsClosed => _implementation.IsClosed;
		void ICachedMessagingEntity.Close() => _implementation.Close();
		public IMessagingMessage CreteMessage(Stream stream, OutgoingMessage outgoingMessage) => new ServiceBusMessage(new BrokeredMessage(stream, true));
	}
}

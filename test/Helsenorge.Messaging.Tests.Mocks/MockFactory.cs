/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.Tests.Mocks
{
    public class MockFactory : IAmqpFactory
    {
        public const int HelsenorgeHerId = 93238;
        public const int HerIdWithOnlyCpp = 91462;

        public int OtherHerId { get; }

        public MockCommunicationParty Helsenorge { get; }
        public MockCommunicationParty OtherParty { get; }
        public MockCommunicationParty OtherPartyWithOnlyCpp { get; }

        public Dictionary<string, List<IAmqpMessage>> Qeueues { get; } = new Dictionary<string, List<IAmqpMessage>>();
        public List<IAmqpMessage> DeadLetterQueue { get; } = new List<IAmqpMessage>();

        public bool IsClosed => false;

        public MockFactory(int otherHerID)
        {
            OtherHerId = otherHerID;
            Helsenorge = new MockCommunicationParty(this, HelsenorgeHerId);
            OtherParty = new MockCommunicationParty(this, OtherHerId);
            OtherPartyWithOnlyCpp = new MockCommunicationParty(this, HerIdWithOnlyCpp);
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public IAmqpReceiver CreateMessageReceiver(string id, int credit)
        {
            return new MockReceiver(this, id);
        }

        public IAmqpSender CreateMessageSender(string id)
        {
            return new MockSender(this, id);
        }

        public Task<IAmqpMessage> CreateMessageAsync(Stream stream)
        {
            return Task.FromResult<IAmqpMessage>(new MockMessage(stream));
        }
    }

    public class MockCommunicationParty
    {
        public MockCommunicationParty(MockFactory factory, int herId)
        {
            Asynchronous = new MockQueue($"{herId}_async");
            Synchronous = new MockQueue($"{herId}_sync");
            Error = new MockQueue($"{herId}_error");
            SynchronousReply = new MockQueue($"{herId}_syncreply");
            DeadLetter = new MockQueue($"{herId}_dl");

            factory.Qeueues.Add(Asynchronous.Name, Asynchronous.Messages);
            factory.Qeueues.Add(Synchronous.Name, Synchronous.Messages);
            factory.Qeueues.Add(Error.Name, Error.Messages);
            factory.Qeueues.Add(SynchronousReply.Name, SynchronousReply.Messages);
            factory.Qeueues.Add(DeadLetter.Name, DeadLetter.Messages);
        }

        public MockQueue Asynchronous { get; }
        public MockQueue Synchronous { get; }
        public MockQueue Error { get; }
        public MockQueue SynchronousReply { get; }
        public MockQueue DeadLetter { get; }

    }

    public class MockQueue
    {
        public string Name { get; }
        public List<IAmqpMessage> Messages { get; }

        public MockQueue(string name)
        {
            Name = name;
            Messages = new List<IAmqpMessage>();
        }
    }
}

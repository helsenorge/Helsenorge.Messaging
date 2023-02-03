/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.Tests.Mocks
{
    internal class MockReceiver : IMessagingReceiver
    {
        private readonly MockFactory _factory;
        private readonly string _id;

        public MockReceiver(MockFactory factory, string id)
        {
            _factory = factory;
            _id = id;
        }

        public bool IsClosed => false;
        public Task CloseAsync() { return Task.CompletedTask; }

        public IMessagingMessage Receive()
            => Receive(TimeSpan.FromMilliseconds(BusSettings.DefaultTimeoutInMilliseconds));

        public IMessagingMessage Receive(TimeSpan serverWaitTime)
        {
            if (_factory.Qeueues.ContainsKey(_id))
            {
                var queue = _factory.Qeueues[_id];
                if (queue.Count > 0)
                {
                    return queue.First();
                }
            }

            return null;
        }

        public Task<IMessagingMessage> ReceiveAsync()
            => ReceiveAsync(TimeSpan.FromMilliseconds(BusSettings.DefaultTimeoutInMilliseconds));

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            if (_factory.Qeueues.ContainsKey(_id))
            {
                var queue = _factory.Qeueues[_id];
                if (queue.Count > 0)
                {
                    return await Task.FromResult(queue.First());
                }
            }

            return null;
        }
    }
}

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
        public void Close() {}

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
            //System.Threading.Thread.Sleep(serverWaitTime);
            return null;
        }
    }
}

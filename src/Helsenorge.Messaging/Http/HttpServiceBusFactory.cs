
using Helsenorge.Messaging.Abstractions;
using System;
using System.Diagnostics;
using System.IO;

namespace Helsenorge.Messaging.Http
{
    internal class HttpServiceBusFactory : IMessagingFactory
    {
        private string _baseurl;
        public HttpServiceBusFactory(string baseurl)
        {
            Debug.Assert(baseurl != null);
            _baseurl = baseurl;
        }
        public IMessagingReceiver CreateMessageReceiver(string id)
        {
            // id example '95218_async'
            return new HttpServiceBusReceiver(_baseurl, id);
        }
        public IMessagingSender CreateMessageSender(string id)
        {
            // id sample value '95218_async'
            return new HttpServiceBusSender(_baseurl, id);
        }
        bool ICachedMessagingEntity.IsClosed => false;
        void ICachedMessagingEntity.Close() {
        }
        public IMessagingMessage CreteMessage(Stream stream, OutgoingMessage outgoingMessage)
        {
            //TODO: Fix typo
            Debug.Assert(stream != null);
            Debug.Assert(outgoingMessage != null);

            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return new OutgoingHttpMessage { Payload = outgoingMessage.Payload, BinaryPayload = memoryStream.ToArray() };
        }
    }

}

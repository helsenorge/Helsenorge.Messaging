using Helsenorge.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Http
{
    public class IncomingHttpMessage : IMessagingMessage
    {
        public XElement AMQPMessage { get; set; }
        //TODO: Rename all GetAMQPxxx to GetValue from XMLAMQPMessage or similar
        private string GetAMQPMessageField(string name)
        {
            return GetAMQPMessageFieldElement(name).Value;
        }

        private XElement GetAMQPMessageFieldElement(string name) //TODO: Return single child
        {
            var element = AMQPMessage.Elements(name).SingleOrDefault();
            if (element == null)
            {
                throw new ArgumentException($"Cannot find message field named '{name}'");
            }
            return element;
        }

        public byte[] BinaryPayload { get; set; } //TODO: Remove

        /* IMessagingMessage implementation */

        public int FromHerId
        {
            get => int.Parse(GetAMQPMessageField("FromHerId"));
            set => throw new NotImplementedException();
        }

        public int ToHerId
        {
            get => int.Parse(GetAMQPMessageField("ToHerId"));
            set => throw new NotImplementedException();
        }


        public DateTime ApplicationTimestamp
        {
            get => XmlConvert.ToDateTime(GetAMQPMessageField("ApplicationTimestamp"), XmlDateTimeSerializationMode.Unspecified);
            set => throw new NotImplementedException();
        }

        public string CpaId
        {
            get => GetAMQPMessageField("CpaId");
            set => throw new NotImplementedException();
        }

        public DateTime EnqueuedTimeUtc
        {
            get => XmlConvert.ToDateTime(GetAMQPMessageField("EnqueuedTimeUtc"), XmlDateTimeSerializationMode.Unspecified);
            set => throw new NotImplementedException();
        }

        public DateTime ExpiresAtUtc //Get from XML?
        {
            get
            {
                //TODO: Is this reasonable?
                return DateTime.Now + TimeSpan.FromMinutes(5);
            }
        }

        public IDictionary<string, object> Properties { get; set; }

        public long Size
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ContentType
        {
            get => GetAMQPMessageField("ContentType");
            set => throw new NotImplementedException();
        }


        public string CorrelationId
        {
            get => GetAMQPMessageField("CorrelationId");
            set => throw new NotImplementedException();
        }

        public string MessageFunction
        {
            get => GetAMQPMessageField("MessageFunction");
            set => throw new NotImplementedException();
        }

        
        public string MessageId {
            get => GetAMQPMessageField("MessageId");
            set => throw new NotImplementedException();
        }

        
        public string ReplyTo
        {
            get => GetAMQPMessageField("ReplyTo");
            set => throw new NotImplementedException();
        }

        public DateTime ScheduledEnqueueTimeUtc
        {
            get => XmlConvert.ToDateTime(GetAMQPMessageField("ScheduledEnqueueTimeUtc"), XmlDateTimeSerializationMode.Unspecified);
            set => throw new NotImplementedException();
        }

        public TimeSpan TimeToLive
        {
            get => TimeSpan.Parse(GetAMQPMessageField("TimeToLive"));
            set => throw new NotImplementedException();
        }

        public string To
        {
            get => GetAMQPMessageField("To");
            set => throw new NotImplementedException();
        }

        public object OriginalObject
        {
            get => throw new NotImplementedException();
        }

        public void Complete()
        {
            
        }

        public Task CompleteAsync()
        {
            return Task.CompletedTask;
        }

        public IMessagingMessage Clone()
        {
            throw new NotImplementedException();
        }

        public Stream GetBody()
        {
            
            if (ContentType == "text/plain") // ... or SOAP
            {
                var reader = GetAMQPMessageFieldElement("Payload").CreateReader();
                reader.MoveToContent();

                var memoryStream = new MemoryStream();
                var writer = new StreamWriter(memoryStream);
                writer.Write(reader.ReadInnerXml());
                writer.Flush();
                memoryStream.Position = 0;
                return memoryStream;
            }
            else
            {
                return new MemoryStream(Convert.FromBase64String(GetAMQPMessageField("BinaryPayload")));
            }
        }

        public void AddDetailsToException(Exception ex)
        {
            // Do nothing
        }

        public void Dispose()
        {
            
        }
    }

}



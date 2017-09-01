using Helsenorge.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Http
{
    //TODO: Add creator and parser for these types (OHM -> XElement and XElement -> IHM)
    public class OutgoingHttpMessage : IMessagingMessage
    {
        public XDocument Payload { get; set; }
        public byte[] BinaryPayload { get; set; }

        /* IMessagingMessag implementation */

        public int FromHerId { get; set; }

        public int ToHerId { get; set; }

        public DateTime ApplicationTimestamp { get; set; }

        public string CpaId { get; set; }

        public DateTime EnqueuedTimeUtc { get; set; }

        public DateTime ExpiresAtUtc
        {
            get
            {
                throw new NotImplementedException();
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

        public string ContentType { get; set; }

        public string CorrelationId { get; set; }

        public string MessageFunction { get; set; }

        public string MessageId { get; set; }

        public string ReplyTo { get; set; }

        public DateTime ScheduledEnqueueTimeUtc { get; set; }

        public TimeSpan TimeToLive { get; set; }

        public string To { get; set; }

        public object OriginalObject
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Complete()
        {
            throw new NotImplementedException();
        }

        public Task CompleteAsync()
        {
            throw new NotImplementedException();
        }

        public IMessagingMessage Clone()
        {
            throw new NotImplementedException();
        }

        public Stream GetBody()
        {
            throw new NotImplementedException();
        }

        public void AddDetailsToException(Exception ex)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public XElement CreateHttpBody()
        {
            var payloadElement = new XElement("Payload");
            if (Payload != null && Payload.Root != null)
            {
                payloadElement.Add(Payload.Root);
            }
            else
            {
                payloadElement.Value = "#WARNING: No payload";
            }

            var binaryPayloadElement = new XElement("BinaryPayload");
            if (BinaryPayload != null)
            {
                binaryPayloadElement.Value = Convert.ToBase64String(BinaryPayload);
            }
            else
            {
                binaryPayloadElement.Value = "#WARNING: No binary payload";
            }

            return new XElement("AMQPMessage",
                payloadElement,
                binaryPayloadElement,
                new XElement("ApplicationTimestamp", ApplicationTimestamp),
                new XElement("ContentType", ContentType),
                new XElement("CorrelationId", CorrelationId),
                new XElement("CpaId", CpaId),
                new XElement("EnqueuedTimeUtc", EnqueuedTimeUtc),
                new XElement("ScheduledEnqueueTimeUtc", ScheduledEnqueueTimeUtc),
                new XElement("TimeToLive", TimeToLive.ToString()),
                new XElement("To", To),
                new XElement("ToHerId", ToHerId),
                new XElement("FromHerId", FromHerId),
                new XElement("MessageFunction", MessageFunction),
                new XElement("MessageId", MessageId),
                new XElement("ReplyTo", ReplyTo)
            );
        }

    }

}



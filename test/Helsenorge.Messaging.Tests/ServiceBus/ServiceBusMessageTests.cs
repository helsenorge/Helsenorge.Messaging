using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Helsenorge.Messaging.Tests.ServiceBus
{
    [TestClass]
    public class ServiceBusMessageTests
    {
        [TestMethod]
        public void Should_Return_TimeSpan_MaxValue_If_TimeToLive_Not_Set()
        {
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
                Assert.AreEqual(TimeSpan.MaxValue, message.TimeToLive);
        }

        [TestMethod]
        public void Should_Return_Set_TimeToLive_If_TimeToLive_Set()
        {
            TimeSpan ttl = TimeSpan.FromDays(4);
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
            {
                message.TimeToLive = ttl;
                Assert.AreEqual(ttl, message.TimeToLive);
            }
        }

        [TestMethod]
        public void Should_Return_Set_ScheduledEnqueueTimeUtc_If_ScheduledEnqueueTimeUtc_Set()
        {
            DateTime scheduledEnqueueTimeUtc = DateTime.UtcNow.AddDays(1);
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
            {
                message.ScheduledEnqueueTimeUtc = scheduledEnqueueTimeUtc;
                Assert.AreEqual(scheduledEnqueueTimeUtc, message.ScheduledEnqueueTimeUtc);
            }
        }

        [TestMethod]
        public void Should_Return_DeliveryCount_Zero_If_Message_Just_Initialized()
        {
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
                Assert.AreEqual(0, message.DeliveryCount);
        }

        [TestMethod]
        public void Should_Return_EnqueuedTimeUtc_DateTime_MaxValue_If_Message_Just_Initialized()
        {
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
                Assert.AreEqual(DateTime.MaxValue, message.EnqueuedTimeUtc);
        }

        [TestMethod]
        public void Should_Return_ExpiresAtUtc_DateTime_MaxValue_If_Message_Just_Initialized()
        {
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
                Assert.AreEqual(DateTime.MaxValue, message.ExpiresAtUtc);
        }

        [TestMethod]
        public void Should_Return_Same_Amqp_Message_On_OriginalObject_As_Passed_To_Constructor()
        {
            Message originalObject = new Message();
            using (ServiceBusMessage message = new ServiceBusMessage(originalObject))
                Assert.AreSame(originalObject, message.OriginalObject);
        }

        [TestMethod]
        public void Should_Return_Size_Zero_If_Payload_NotSet()
        {
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
                Assert.AreEqual(0, message.Size);
        }

        [TestMethod]
        public void Should_Return_All_Initialized_ApplicationProperties()
        {
            int toHerId = 123;
            int fromHerId = 456;
            DateTime utcNow = DateTime.UtcNow;
            DateTime applicationTimeStamp = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, utcNow.Second);
            string cpaId = "cpaId_value";
            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
            {
                message.ToHerId = toHerId;
                message.FromHerId = fromHerId;
                message.ApplicationTimestamp = applicationTimeStamp;
                message.CpaId = cpaId;

                Assert.AreEqual(toHerId, message.ToHerId);
                Assert.AreEqual(fromHerId, message.FromHerId);
                Assert.AreEqual(applicationTimeStamp, message.ApplicationTimestamp);
                Assert.AreEqual(cpaId, message.CpaId);
            }
        }

        [TestMethod]
        public void Should_Return_All_Initialized_Properties()
        {
            string contentType = "contentType_value";
            string correlationId = "correlationId_value";
            string messageFunction = "messageFunction_value";
            string messageId = "messageId_value";
            string replyTo = "replyTo_value";
            string to = "to_value";

            using (ServiceBusMessage message = new ServiceBusMessage(new Message()))
            {
                message.ContentType = contentType;
                message.CorrelationId = correlationId;
                message.MessageFunction = messageFunction;
                message.MessageId = messageId;
                message.ReplyTo = replyTo;
                message.To = to;

                Assert.AreEqual(contentType, message.ContentType);
                Assert.AreEqual(correlationId, message.CorrelationId);
                Assert.AreEqual(messageFunction, message.MessageFunction);
                Assert.AreEqual(messageId, message.MessageId);
                Assert.AreEqual(replyTo, message.ReplyTo);
                Assert.AreEqual(to, message.To);
            }
        }

        [TestMethod]
        public void Should_Return_Exact_Cloned_Message_With_Payload()
        {
            int toHerId = 123;
            int fromHerId = 456;
            DateTime utcNow = DateTime.UtcNow;
            DateTime applicationTimeStamp = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, utcNow.Second);
            string cpaId = "cpaId_value";
            string contentType = "contentType_value";
            string correlationId = "correlationId_value";
            string messageFunction = "messageFunction_value";
            string messageId = "messageId_value";
            string replyTo = "replyTo_value";
            string to = "to_value";
            TimeSpan ttl = TimeSpan.FromDays(4);
            DateTime scheduledEnqueueTimeUtc = DateTime.UtcNow;
            Data data = new Data
            {
                Binary = new byte[] { 255, 0, 255 }
            };

            using (ServiceBusMessage message = new ServiceBusMessage(new Message(data)))
            {
                message.ToHerId = toHerId;
                message.FromHerId = fromHerId;
                message.ApplicationTimestamp = applicationTimeStamp;
                message.CpaId = cpaId;
                message.ContentType = contentType;
                message.CorrelationId = correlationId;
                message.MessageFunction = messageFunction;
                message.MessageId = messageId;
                message.ReplyTo = replyTo;
                message.To = to;
                message.TimeToLive = ttl;
                message.ScheduledEnqueueTimeUtc = scheduledEnqueueTimeUtc;

                var clonedMessage = message.Clone();

                Assert.AreEqual(toHerId, clonedMessage.ToHerId);
                Assert.AreEqual(fromHerId, clonedMessage.FromHerId);
                Assert.AreEqual(applicationTimeStamp, clonedMessage.ApplicationTimestamp);
                Assert.AreEqual(cpaId, clonedMessage.CpaId);
                Assert.AreEqual(contentType, clonedMessage.ContentType);
                Assert.AreEqual(correlationId, clonedMessage.CorrelationId);
                Assert.AreEqual(messageFunction, clonedMessage.MessageFunction);
                Assert.AreEqual(messageId, clonedMessage.MessageId);
                Assert.AreEqual(replyTo, clonedMessage.ReplyTo);
                Assert.AreEqual(to, clonedMessage.To);
                Assert.AreEqual(ttl, clonedMessage.TimeToLive);
                Assert.AreEqual(scheduledEnqueueTimeUtc, clonedMessage.ScheduledEnqueueTimeUtc);

                Assert.IsNotNull(((Message)clonedMessage.OriginalObject).Body);
                Assert.IsInstanceOfType(((Message)clonedMessage.OriginalObject).BodySection, typeof(Data));
                Data dataClone = (Data)((Message)clonedMessage.OriginalObject).BodySection;
                Assert.AreEqual(data.Binary.Length, dataClone.Binary.Length);
                Assert.AreEqual(data.Binary[0], dataClone.Binary[0]);
                Assert.AreEqual(data.Binary[1], dataClone.Binary[1]);
                Assert.AreEqual(data.Binary[2], dataClone.Binary[2]);
            }
        }

        [TestMethod]
        public void Should_Return_Exact_Cloned_Message_Without_Payload()
        {
            int toHerId = 123;
            int fromHerId = 456;
            DateTime utcNow = DateTime.UtcNow;
            DateTime applicationTimeStamp = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, utcNow.Second);
            string cpaId = "cpaId_value";
            string contentType = "contentType_value";
            string correlationId = "correlationId_value";
            string messageFunction = "messageFunction_value";
            string messageId = "messageId_value";
            string replyTo = "replyTo_value";
            string to = "to_value";
            TimeSpan ttl = TimeSpan.FromDays(4);
            DateTime scheduledEnqueueTimeUtc = DateTime.UtcNow;

            Data data = new Data
            {
                Binary = new byte[] { 255, 0, 255 }
            };

            using (ServiceBusMessage message = new ServiceBusMessage(new Message(data)))
            {
                message.ToHerId = toHerId;
                message.FromHerId = fromHerId;
                message.ApplicationTimestamp = applicationTimeStamp;
                message.CpaId = cpaId;
                message.ContentType = contentType;
                message.CorrelationId = correlationId;
                message.MessageFunction = messageFunction;
                message.MessageId = messageId;
                message.ReplyTo = replyTo;
                message.To = to;
                message.TimeToLive = ttl;
                message.ScheduledEnqueueTimeUtc = scheduledEnqueueTimeUtc;

                var clonedMessage = message.Clone(includePayload: false);

                Assert.AreEqual(toHerId, clonedMessage.ToHerId);
                Assert.AreEqual(fromHerId, clonedMessage.FromHerId);
                Assert.AreEqual(applicationTimeStamp, clonedMessage.ApplicationTimestamp);
                Assert.AreEqual(cpaId, clonedMessage.CpaId);
                Assert.AreEqual(contentType, clonedMessage.ContentType);
                Assert.AreEqual(correlationId, clonedMessage.CorrelationId);
                Assert.AreEqual(messageFunction, clonedMessage.MessageFunction);
                Assert.AreEqual(messageId, clonedMessage.MessageId);
                Assert.AreEqual(replyTo, clonedMessage.ReplyTo);
                Assert.AreEqual(to, clonedMessage.To);
                Assert.AreEqual(ttl, clonedMessage.TimeToLive);
                Assert.AreEqual(scheduledEnqueueTimeUtc, clonedMessage.ScheduledEnqueueTimeUtc);

                Assert.IsNull(((Message)clonedMessage.OriginalObject).Body);
            }
        }
    }
}

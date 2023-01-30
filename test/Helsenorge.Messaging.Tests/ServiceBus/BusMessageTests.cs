/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using Amqp.Framing;
using Helsenorge.Messaging.Bus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;

namespace Helsenorge.Messaging.Tests.Bus
{
    [TestClass]
    public class BusMessageTests
    {
        [TestMethod]
        public void Should_Return_TimeSpan_MaxValue_If_TimeToLive_Not_Set()
        {
            using (BusMessage message = new BusMessage(new Message()))
                Assert.AreEqual(TimeSpan.MaxValue, message.TimeToLive);
        }

        [TestMethod]
        public void Should_Return_Set_TimeToLive_If_TimeToLive_Set()
        {
            TimeSpan ttl = TimeSpan.FromDays(4);
            using (BusMessage message = new BusMessage(new Message()))
            {
                message.TimeToLive = ttl;
                Assert.AreEqual(ttl, message.TimeToLive);
            }
        }

        [TestMethod]
        public void Should_Return_Set_ScheduledEnqueueTimeUtc_If_ScheduledEnqueueTimeUtc_Set()
        {
            DateTime scheduledEnqueueTimeUtc = DateTime.UtcNow.AddDays(1);
            using (BusMessage message = new BusMessage(new Message()))
            {
                message.ScheduledEnqueueTimeUtc = scheduledEnqueueTimeUtc;
                Assert.AreEqual(scheduledEnqueueTimeUtc, message.ScheduledEnqueueTimeUtc);
            }
        }

        [TestMethod]
        public void Should_Return_DeliveryCount_Zero_If_Message_Just_Initialized()
        {
            using (BusMessage message = new BusMessage(new Message()))
                Assert.AreEqual(0, message.DeliveryCount);
        }

        [TestMethod]
        public void Should_Return_EnqueuedTimeUtc_DateTime_MaxValue_If_Message_Just_Initialized()
        {
            using (BusMessage message = new BusMessage(new Message()))
                Assert.AreEqual(DateTime.MaxValue, message.EnqueuedTimeUtc);
        }

        [TestMethod]
        public void Should_Return_ExpiresAtUtc_DateTime_MaxValue_If_Message_Just_Initialized()
        {
            using (BusMessage message = new BusMessage(new Message()))
                Assert.AreEqual(DateTime.MaxValue, message.ExpiresAtUtc);
        }

        [TestMethod]
        public void Should_Return_Same_Amqp_Message_On_OriginalObject_As_Passed_To_Constructor()
        {
            Message originalObject = new Message();
            using (BusMessage message = new BusMessage(originalObject))
                Assert.AreSame(originalObject, message.OriginalObject);
        }

        [TestMethod]
        public void Should_Return_Size_Zero_If_Payload_NotSet()
        {
            using (BusMessage message = new BusMessage(new Message()))
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
            using (BusMessage message = new BusMessage(new Message()))
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
        public void Should_Return_Custom_ApplicationProperties()
        {
            int customProperty1 = 1;
            int customPropert1Change = 4;
            string customProperty2 = "Value 2";
            DateTime customProperty3 = DateTime.Now;

            using BusMessage message = new BusMessage(new Message());
            message.SetApplicationProperty("CustomProperty1", customProperty1);
            message.SetApplicationProperty("CustomProperty2", customProperty2);
            message.SetApplicationProperty("CustomProperty3", customProperty3);

            Assert.AreEqual(customProperty1.ToString(CultureInfo.InvariantCulture), message.Properties["CustomProperty1"]);
            Assert.AreEqual(customProperty2, message.Properties["CustomProperty2"]);
            Assert.AreEqual(customProperty3.ToString(StringFormatConstants.IsoDateTime, DateTimeFormatInfo.InvariantInfo), message.Properties["CustomProperty3"]);

            message.Properties["CustomProperty1"] = customPropert1Change;
            Assert.AreNotEqual(customPropert1Change, message.Properties["CustomProperty1"]);
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

            using (BusMessage message = new BusMessage(new Message()))
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
            byte[] data = new byte[] { 255, 0, 255 };

            using (BusMessage message = new BusMessage(new Message(data)))
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
                Assert.AreEqual(data.Length, dataClone.Binary.Length);
                Assert.AreEqual(data[0], dataClone.Binary[0]);
                Assert.AreEqual(data[1], dataClone.Binary[1]);
                Assert.AreEqual(data[2], dataClone.Binary[2]);
            }
        }

        [TestMethod]
        public void Should_Return_Cloned_Message_When_Payload_Is_Stream()
        {
            using MemoryStream data = new MemoryStream(new byte[] { 255, 0, 255 });
            using BusMessage message = new BusMessage(new Message(data));
            var clonedMessage = message.Clone();

            Assert.IsNotNull(((Message)clonedMessage.OriginalObject).Body);
            Assert.IsInstanceOfType(((Message)clonedMessage.OriginalObject).BodySection, typeof(Data));
            Data dataClone = (Data)((Message)clonedMessage.OriginalObject).BodySection;

            data.Position = 0;
            byte[] dataBinary = data.ToArray();

            Assert.AreEqual(dataBinary.Length, dataClone.Binary.Length);
            Assert.AreEqual(dataBinary[0], dataClone.Binary[0]);
            Assert.AreEqual(dataBinary[1], dataClone.Binary[1]);
            Assert.AreEqual(dataBinary[2], dataClone.Binary[2]);
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

            byte[] data =  new byte[] { 255, 0, 255 };

            using (BusMessage message = new BusMessage(new Message(data)))
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

        [TestMethod]
        public void Modifications_On_Cloned_Message_Should_Not_Affect_Original_Message()
        {
            var toHerId = 123;
            var fromHerId = 456;

            using var originalMessage = new BusMessage(new Message());
            originalMessage.ToHerId = toHerId;
            originalMessage.FromHerId = fromHerId;

            var clonedMessage = originalMessage.Clone(includePayload: false);

            clonedMessage.FromHerId = originalMessage.ToHerId;
            clonedMessage.ToHerId = originalMessage.FromHerId;

            // Assert that clonedMessage has its ToHerId and FromHerId switched.
            Assert.AreEqual(toHerId, clonedMessage.FromHerId);
            Assert.AreEqual(fromHerId, clonedMessage.ToHerId);
            // Assert that originalMessage has ToHerId and FromHerId set as it was originally.
            Assert.AreEqual(toHerId, originalMessage.ToHerId);
            Assert.AreEqual(fromHerId, originalMessage.FromHerId);
        }
    }
}

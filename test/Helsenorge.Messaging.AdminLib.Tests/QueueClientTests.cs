/*
 * Copyright (c) 2022-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
*/

using Helsenorge.Messaging.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.AdminLib.Tests;

[TestClass]
public class QueueClientTests
{
    [TestMethod]
    [DataRow(1234, QueueType.Asynchronous, "1234_async")]
    [DataRow(4567, QueueType.Synchronous, "4567_sync")]
    [DataRow(8901, QueueType.SynchronousReply, "8901_syncreply")]
    [DataRow(2345, QueueType.Error, "2345_error")]
    [DataRow(6789, QueueType.DeadLetter, "6789_dl")]
    public void Assert_QueueName_Is_Correctly_Constructed(int herId, QueueType queueType, string expectedQueueName)
    {
        var actualQueueName = QueueUtilities.ConstructQueueName(herId, queueType);

        Assert.AreEqual(expectedQueueName, actualQueueName);
    }
}

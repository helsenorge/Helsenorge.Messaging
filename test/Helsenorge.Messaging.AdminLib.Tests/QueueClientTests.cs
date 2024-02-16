/*
 * Copyright (c) 2022-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
*/

using Helsenorge.Messaging.Amqp;
using Xunit;

namespace Helsenorge.Messaging.AdminLib.Tests;

public class QueueClientTests
{
    [Theory]
    [InlineData(1234, QueueType.Asynchronous, "1234_async")]
    [InlineData(4567, QueueType.Synchronous, "4567_sync")]
    [InlineData(8901, QueueType.SynchronousReply, "8901_syncreply")]
    [InlineData(2345, QueueType.Error, "2345_error")]
    [InlineData(6789, QueueType.DeadLetter, "6789_dl")]
    public void Assert_QueueName_Is_Correctly_Constructed(int herId, QueueType queueType, string expectedQueueName)
    {
        var actualQueueName = QueueUtilities.ConstructQueueName(herId, queueType);

        Assert.Equal(expectedQueueName, actualQueueName);
    }
}

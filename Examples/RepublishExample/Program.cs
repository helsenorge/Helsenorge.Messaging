/*
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.AdminLib;
using Helsenorge.Messaging.AdminLib.RabbitMQ;
using Helsenorge.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace RepublishExample;

internal static class Program
{
    private static string HostName = "tb.test.nhn.no";
    private static int Port = 5671;
    private static string Exchange = "NHNTESTServiceBus";
    private static string UserName = "<username>";
    private static string Password = "<password>";

    private static int SourceHerId = 1234;

    private static void Main()
    {
        var loggerFactory = new LoggerFactory();
        var connectionString = new ConnectionString
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            Exchange = Exchange,
        };

        var client = new QueueClient(connectionString, loggerFactory.CreateLogger<QueueType>());

        client.RepublishMessageFromDeadLetterToOriginQueue(SourceHerId);
    }
}

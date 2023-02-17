/*
 * Copyright (c) 2022-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.AdminLib;
using Helsenorge.Messaging.Amqp;
using Microsoft.Extensions.DependencyInjection;
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
        var connectionString = GetConnectionParameters();
        var logger = CreateLogger();

        using var client = new QueueClient(connectionString, logger);

        var msgCountDlBefore = client.GetMessageCount(SourceHerId, QueueType.DeadLetter);
        logger.LogDebug($"MessageCount in DeadLetter for HerId '{SourceHerId}' before republish: {msgCountDlBefore}");

        client.RepublishMessageFromDeadLetterToOriginQueue(SourceHerId);

        var msgCountDlAfter= client.GetMessageCount(SourceHerId, QueueType.DeadLetter);
        logger.LogDebug($"MessageCount in DeadLetter for HerId '{SourceHerId}' after republish: {msgCountDlAfter}");
    }

    private static ConnectionString GetConnectionParameters()
    {
        return new ConnectionString
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            Exchange = Exchange,
        };
    }

    static ILogger CreateLogger()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(loggerConfiguration =>
        {
            loggerConfiguration.AddConsole();
            loggerConfiguration.AddDebug();
            loggerConfiguration.SetMinimumLevel(LogLevel.Debug);
        });
        var provider = serviceCollection.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<QueueType>();
        return logger;
    }
}

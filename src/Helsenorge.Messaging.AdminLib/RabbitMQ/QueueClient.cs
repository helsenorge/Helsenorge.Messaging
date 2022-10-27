/*
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Helsenorge.Messaging.ServiceBus;
using RabbitMQ.Client;

namespace Helsenorge.Messaging.AdminLib.RabbitMQ;

public class QueueClient : IDisposable, IAsyncDisposable
{
    private readonly ConnectionString _connectionString;
    private readonly ILogger _logger;
    private IConnectionFactory _connectionFactory;
    private IConnection _connection;
    private IModel _channel;

    public QueueClient(ConnectionString connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IConnectionFactory ConnectionFactory
    {
        get
        {
            return _connectionFactory ??= new ConnectionFactory
            {
                Uri = new Uri($"amqps://{_connectionString.HostName}"),
                UserName = _connectionString.UserName,
                Password = _connectionString.Password,
                VirtualHost = string.IsNullOrWhiteSpace(_connectionString.VirtualHost) ? "/" : _connectionString.VirtualHost,
                ClientProvidedName = _connectionString.ClientProvidedName,
            };
        }
    }

    private IConnection Connection
    {
        get { return _connection ??= ConnectionFactory.CreateConnection(); }
    }

    private IModel Channel
    {
        get { return _channel ??= Connection.CreateModel(); }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// A method to retrieve the number of messages currently on the queue by specifiying the HER-id and the queue type.
    /// </summary>
    /// <param name="herId">The HER-id of the queue.</param>
    /// <param name="queueType">The type of queue.</param>
    /// <returns>The number of messages currently on the queue.</returns>
    public uint GetMessageCount(int herId, QueueType queueType)
    {
        var queue = QueueUtilities.ConstructQueueName(herId, queueType);
        return GetMessageCount(queue);
    }

    /// <summary>
    /// A method to retrieve the number of messages currently on the queue by explicitly naming the queue.
    /// </summary>
    /// <param name="queue">The name of the queue.</param>
    /// <returns>The number of messages currently on the queue.</returns>
    public uint GetMessageCount(string queue)
    {
        return Channel.MessageCount(queue);
    }
}

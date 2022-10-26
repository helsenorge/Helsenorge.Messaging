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
}

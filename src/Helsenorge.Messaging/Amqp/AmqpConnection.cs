/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Amqp;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Amqp
{
    /// <summary>
    /// Represents the connection to a Message Broker.
    /// </summary>
    public class AmqpConnection
    {
        private ConnectionFactory _connectionFactory;
        private readonly Address _address;
        private IConnection _connection;

        /// <summary>Initializes a new instance of the <see cref="AmqpConnection" /> class with the given connection string and a <see cref="ILogger"/> object.</summary>
        /// <param name="connectionString">The connection used to connect to Message Broker.</param>
        public AmqpConnection(string connectionString)
            : this(connectionString, MessageBrokerDialect.RabbitMQ, AmqpSettings.DefaultMaxLinksPerSession, AmqpSettings.DefaultMaxSessions)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AmqpConnection" /> class with the given connection string and a <see cref="ILogger"/> object.</summary>
        /// <param name="connectionString">The connection used to connect to Message Broker.</param>
        /// <param name="messageBrokerDialect">A <see cref="MessageBrokerDialect"/> which tells BusConnection what kind of Message Broker we are communicating with.</param>
        public AmqpConnection(string connectionString, MessageBrokerDialect messageBrokerDialect)
            : this(connectionString, messageBrokerDialect, AmqpSettings.DefaultMaxLinksPerSession, AmqpSettings.DefaultMaxSessions)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AmqpConnection" /> class with the given connection string and a <see cref="ILogger"/> object.</summary>
        /// <param name="connectionString">The connection used to connect to Message Broker.</param>
        /// <param name="messageBrokerDialect">A <see cref="MessageBrokerDialect"/> which tells BusConnection what kind of Message Broker we are communicating with.</param>
        /// <param name="maxLinksPerSession">The max links that will be allowed per session.</param>
        /// <param name="maxSessionsPerConnection">The max sessions that will be allowed per connection.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public AmqpConnection(string connectionString, MessageBrokerDialect messageBrokerDialect, int maxLinksPerSession, ushort maxSessionsPerConnection)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            _address = new Address(connectionString);
            MessageBrokerDialect = messageBrokerDialect;

            if (!string.IsNullOrEmpty(_address.Path))
            {
                Namespace = _address.Path.Substring(1).TrimEnd('/');
                _address = new Address(connectionString.Substring(0, connectionString.Length - Namespace.Length));
            }

            _connectionFactory = new ConnectionFactory();
            if (maxLinksPerSession != AmqpSettings.DefaultMaxLinksPerSession)
            {
                _connectionFactory.AMQP.MaxLinksPerSession = maxLinksPerSession;
            }
            if (maxSessionsPerConnection != AmqpSettings.DefaultMaxSessions)
            {
                _connectionFactory.AMQP.MaxSessionsPerConnection = maxSessionsPerConnection;
            }
        }

        /// <summary>
        /// Returns the NameSpace part of the connection string.
        /// </summary>
        /// <remarks>This only works properly for Microsoft Service Bus.</remarks>
        internal string Namespace { get; }

        /// <summary>
        /// Returns what kind of Message Broker Dialect we should use.
        /// </summary>
        public MessageBrokerDialect MessageBrokerDialect { get; protected set; }

        /// <summary>
        /// Indicates if the RabbitMQ AMQP 1.0 Address format v2 should be used when constructing entity names.
        /// See https://www.rabbitmq.com/docs/amqp#addresses for more details.
        /// IMPORTANT: Address format v2 requires RabbitMQ 4.0 or later. Do NOT enable this against brokers
        /// running an earlier version. Default is false (address format v1).
        /// </summary>
        public bool UseAmqpAddressV2 { get; set; }

        /// <summary>Let's you get access to the internal <see cref="IConnection"/>.</summary>
        /// <returns>Returns a <see cref="IConnection"/>.</returns>
        internal IConnection GetInternalConnection()
        {
            EnsureConnection();
            return _connection;
        }

        /// <summary>Let's you get access to the internal <see cref="IConnection"/>.</summary>
        /// <returns>Returns a <see cref="IConnection"/>.</returns>
        internal async Task<IConnection> GetInternalConnectionAsync()
        {
            await EnsureConnectionAsync().ConfigureAwait(false);
            return _connection;
        }

        /// <summary>
        /// Ensures we are connected and will reconnect if we have been disconnected for externally reasons.
        /// Will auto-reconnect until Close() is called explicitly.
        /// </summary>
        /// <returns>Returns true if it is connected or has reconnected, otherwise returns false.</returns>
        public bool EnsureConnection()
        {
            if (IsClosedOrClosing)
            {
                throw new ObjectDisposedException("Connection is closed");
            }
            if (_connection == null || _connection.IsClosed)
            {
                _connection = _connectionFactory.CreateAsync(_address).GetAwaiter().GetResult();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ensures we are connected and will reconnect if we have been disconnected for externally reasons.
        /// Will auto-reconnect until Close() is called explicitly.
        /// </summary>
        /// <returns>Returns true if it is connected or has reconnected, otherwise returns false.</returns>
        public async Task<bool> EnsureConnectionAsync()
        {
            if (IsClosedOrClosing)
            {
                throw new ObjectDisposedException("Connection is closed");
            }
            if (_connection == null || _connection.IsClosed)
            {
                _connection = await _connectionFactory.CreateAsync(_address).ConfigureAwait(false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the connection is closing or has been closed, otherwise false.
        /// </summary>
        public bool IsClosedOrClosing { get; private set; }

        /// <summary>
        /// Closes the connection to the Message Broker. This is the preferred method of closing any open connection.
        /// </summary>
        public async Task CloseAsync()
        {
            if (IsClosedOrClosing)
            {
                return;
            }
            IsClosedOrClosing = true;
            if (_connection != null && !_connection.IsClosed)
            {
                await _connection.CloseAsync().ConfigureAwait(false);
            }
        }

        internal string GetEntityName(string id, LinkRole role)
        {
            return GetEntityName(id, Namespace, role);
        }

        private string GetEntityName(string id, string ns, LinkRole role)
        {
            string entityPath;
            if (MessageBrokerDialect == MessageBrokerDialect.RabbitMQ)
            {
                if (UseAmqpAddressV2)
                {
                    // RabbitMQ AMQP 1.0 Address format v2 (requires RabbitMQ 4.0 or later):
                    // https://www.rabbitmq.com/docs/amqp#target-address-v2
                    // Sender (target):   /exchanges/{exchange}/{routing-key} or /queues/{queue}
                    // Receiver (source): /queues/{queue}
                    // Exchange, routing key and queue names must be percent-encoded.
                    if (role == LinkRole.Sender)
                    {
                        entityPath = string.IsNullOrEmpty(ns)
                            ? $"/queues/{Uri.EscapeDataString(id)}"
                            : $"/exchanges/{Uri.EscapeDataString(ns)}/{Uri.EscapeDataString(id)}";
                    }
                    else
                    {
                        entityPath = $"/queues/{Uri.EscapeDataString(id)}";
                    }
                }
                else
                {
                    // For more information on Routing and Addressing in RabbitMQ see:
                    // https://github.com/rabbitmq/rabbitmq-server/tree/master/deps/rabbitmq_amqp1_0#routing-and-addressing
                    if (string.IsNullOrEmpty(ns))
                        entityPath = $"{id}";
                    else
                        entityPath = role == LinkRole.Sender ? $"/exchange/{ns}/{id}" : $"/amq/queue/{id}";
                }
            }
            else
            {
                entityPath = string.IsNullOrEmpty(ns) ? id : $"{ns}/{id}";
            }

            return entityPath;
        }
    }
}

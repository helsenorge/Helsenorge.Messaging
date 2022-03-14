﻿/*
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// Represents the connection to a Message Broker.
    /// </summary>
    public class ServiceBusConnection
    {
        private ConnectionFactory _connectionFactory;
        private readonly Address _address;
        private IConnection _connection;

        /// <summary>Initializes a new instance of the <see cref="ServiceBusConnection" /> class with the givem connection string and a <see cref="ILogger"/> object.</summary>
        /// <param name="connectionString">The connection used to connect to Message Broker.</param>
        /// <param name="logger">A <see cref="ILogger{LinkFactory}"/> which will be used to log errors and information.</param>
        public ServiceBusConnection(string connectionString, ILogger logger)
            : this(connectionString, MessageBrokerDialect.ServiceBus, ServiceBusSettings.DefaultMaxLinksPerSession, ServiceBusSettings.DefaultMaxSessions, logger)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ServiceBusConnection" /> class with the givem connection string and a <see cref="ILogger"/> object.</summary>
        /// <param name="connectionString">The connection used to connect to Message Broker.</param>
        /// <param name="messageBrokerDialect">A <see cref="MessageBrokerDialect"/> which tells ServiceBusConnection what kind of Message Broker we are communicating with.</param>
        /// <param name="logger">A <see cref="ILogger{LinkFactory}"/> which will be used to log errors and information.</param>
        public ServiceBusConnection(string connectionString, MessageBrokerDialect messageBrokerDialect, ILogger logger)
            : this(connectionString, messageBrokerDialect, ServiceBusSettings.DefaultMaxLinksPerSession, ServiceBusSettings.DefaultMaxSessions, logger)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ServiceBusConnection" /> class with the givem connection string and a <see cref="ILogger"/> object.</summary>
        /// <param name="connectionString">The connection used to connect to Message Broker.</param>
        /// <param name="messageBrokerDialect">A <see cref="MessageBrokerDialect"/> which tells ServiceBusConnection what kind of Message Broker we are communicating with.</param>
        /// <param name="maxLinksPerSession">The max links that will be allowed per session.</param>
        /// <param name="maxSessionsPerConnection">The max sessions that will be allowed per connection.</param>
        /// <param name="logger">A <see cref="ILogger{LinkFactory}"/> which will be used to log errors and information.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public ServiceBusConnection(string connectionString, MessageBrokerDialect messageBrokerDialect, int maxLinksPerSession, ushort maxSessionsPerConnection, ILogger logger)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException(nameof(connectionString));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _address = new Address(connectionString);
            MessageBrokerDialect = messageBrokerDialect;
            // 'ServicBusHttpClient' is only relevant for Microsoft ServiceBus. It is used to communicate with Microsoft ServiceBus specific operations.
            if (messageBrokerDialect == MessageBrokerDialect.ServiceBus)
            {
#pragma warning disable CS0618
                HttpClient = new ServiceBusHttpClient(_address, logger);
#pragma warning restore CS0618
            }

            if (!string.IsNullOrEmpty(_address.Path))
            {
                Namespace = _address.Path.Substring(1).TrimEnd('/');
                _address = new Address(connectionString.Substring(0, connectionString.Length - Namespace.Length));
            }

            _connectionFactory = new ConnectionFactory();
            if (maxLinksPerSession != ServiceBusSettings.DefaultMaxLinksPerSession)
            {
                _connectionFactory.AMQP.MaxLinksPerSession = maxLinksPerSession;
            }
            if (maxSessionsPerConnection != ServiceBusSettings.DefaultMaxSessions)
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

        [Obsolete("The property 'HttpClient' will be removed in the future. It can only be used to communicate with Microsoft ServiceBus specific operations.")]
        internal ServiceBusHttpClient HttpClient { get; }

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
                // For more information on Routing and Addressing in RabbitMQ see:
                // https://github.com/rabbitmq/rabbitmq-server/tree/master/deps/rabbitmq_amqp1_0#routing-and-addressing
                if (string.IsNullOrEmpty(ns))
                    entityPath = $"{id}";
                else
                    entityPath = role == LinkRole.Sender ? $"/exchange/{ns}/{id}" : $"/amq/queue/{id}";
            }
            else
            {
                entityPath = string.IsNullOrEmpty(ns) ? id : $"{ns}/{id}";
            }

            return entityPath;
        }
    }
}

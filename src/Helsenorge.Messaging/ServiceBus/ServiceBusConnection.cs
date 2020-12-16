/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
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
    internal class ServiceBusConnection
    {
        private ConnectionFactory _connectionFactory;
        private readonly Address _address;
        

        public ServiceBusHttpClient HttpClient { get; }

        private IConnection _connection;

        public string Namespace { get; }

        public async Task<IConnection> GetConnection()
        {
            await EnsureConnection().ConfigureAwait(false);
            return _connection;
        }

        public ServiceBusConnection(string connectionString, ILogger logger)
            : this(connectionString, ServiceBusSettings.DefaultMaxLinksPerSession, ServiceBusSettings.DefaultMaxSessions, logger)
        {

        }

        public ServiceBusConnection(string connectionString, int maxLinksPerSession, ushort maxSessionsPerConnection, ILogger logger)
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
            HttpClient = new ServiceBusHttpClient(_address, logger);

            if (!string.IsNullOrEmpty(_address.Path))
            {
                Namespace = _address.Path.Substring(1);
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
        /// Auto-reconnects until Close() is not called explicitly.
        /// </summary>
        /// <returns>Whether it's reconnected</returns>
        public async Task<bool> EnsureConnection()
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

        public bool IsClosedOrClosing { get; private set; }

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

        public string GetEntityName(string id)
        {
            return GetEntityName(id, Namespace);
        }

        internal static string GetEntityName(string id, string ns)
        {
            return !string.IsNullOrEmpty(ns) ? $"{ns}/{id}" : id;
        }
    }
}

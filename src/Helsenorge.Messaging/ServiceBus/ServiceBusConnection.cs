using Amqp;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class ServiceBusConnection
    {
        private readonly Address _address;

        public ServiceBusHttpClient HttpClient { get; }

        private Connection _connection;

        public string Namespace { get; }

        public Connection Connection
        {
            get
            {
                EnsureConnection();
                return _connection;
            }
        }

        public ServiceBusConnection(string connectionString, ILogger logger)
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
        }

        /// <summary>
        /// Auto-reconnects until Close() is not called explicitly.
        /// </summary>
        /// <returns>Whether it's reconnected</returns>
        public bool EnsureConnection()
        {
            if (IsClosedOrClosing)
            {
                throw new ObjectDisposedException("Connection is closed");
            }
            if (_connection == null || _connection.IsClosed)
            {
                _connection = new Connection(_address);
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
                await _connection.CloseAsync();
            }
        }

        public string GetEntityName(string id)
        {
            return GetEntityName(id, Namespace);
        }

        public static string GetEntityName(string id, string ns)
        {
            return !string.IsNullOrEmpty(ns) ? $"{ns}/{id}" : id;
        }
    }
}

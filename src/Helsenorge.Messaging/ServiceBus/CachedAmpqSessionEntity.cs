using System;
using Amqp;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.ServiceBus
{
    internal abstract class CachedAmpqSessionEntity : ICachedMessagingEntity
    {
        private readonly ServiceBusConnection _connection;
        private Session _session;

        protected CachedAmpqSessionEntity(ServiceBusConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Method is called every time it's needed to renew the link,
        /// e.g. when connection is closed or session is expired etc.
        /// </summary>
        protected abstract void OnSessionCreated(Session session, string ns);

        protected abstract void OnSessionClosing();

        protected void CheckNotClosed()
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException("Session is closed");
            }
        }

        protected void EnsureOpen()
        {
            CheckNotClosed();
            if (_connection.EnsureConnection() || _session == null || _session.IsClosed)
            {
                _session = new Session(_connection.Connection);
                OnSessionCreated(_session, _connection.Namespace);
            }
        }

        public bool IsClosed { get; private set; }

        public void Close()
        {
            if (IsClosed)
            {
                return;
            }
            IsClosed = true;
            if (_session != null && !_session.IsClosed)
            {
                OnSessionClosing();
                _session.Close();
            }
        }
    }
}

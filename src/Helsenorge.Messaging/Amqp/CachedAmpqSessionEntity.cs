/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

﻿using System;
 using System.Threading;
 using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.Amqp
{
    internal abstract class CachedAmpqSessionEntity<TLink> : ICachedAmqpEntity
        where TLink : Link
    {
        protected readonly AmqpConnection Connection;
        protected ISession _session;
        protected TLink _link;
        
        private readonly SemaphoreSlim _mySemaphoreSlim = new SemaphoreSlim(1);
        private readonly object _lockObject = new object();

        protected CachedAmpqSessionEntity(AmqpConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Method is called every time it's needed to renew the link,
        /// e.g. when connection is closed or session is expired etc.
        /// </summary>
        protected abstract TLink CreateLink(ISession session);

        protected ISession CreateSession(IConnection connection)
        {
            return connection.CreateSession();
        }

        protected async Task OnSessionClosingAsync()
        {
            if (_link == null || _link.IsClosed)
            {
                return;
            }
            await _link.CloseAsync().ConfigureAwait(false);
        }

        protected void CheckNotClosed()
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException("Session is closed");
            }
        }

        protected void EnsureOpen()
        {
            lock (_lockObject)
            {
                CheckNotClosed();
                if (Connection.EnsureConnection() || _session == null || _session.IsClosed || _link == null || _link.IsClosed)
                {
                    if (_link != null && !_link.IsClosed)
                    {
                        _link.Close();
                    }

                    if (_session != null && !_session.IsClosed)
                    {
                        _session.Close();
                    }

                    _session = CreateSession(Connection.GetInternalConnection());
                    _link = CreateLink(_session);
                }
            }
        }

        protected async Task EnsureOpenAsync()
        {
            await _mySemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                CheckNotClosed();
                if (await Connection.EnsureConnectionAsync().ConfigureAwait(false) || _session == null || _session.IsClosed || _link == null || _link.IsClosed)
                {
                    if (_link != null && !_link.IsClosed)
                    {
                        await _link.CloseAsync().ConfigureAwait(false);
                    }

                    if (_session != null && !_session.IsClosed)
                    {
                        await _session.CloseAsync().ConfigureAwait(false);
                    }

                    _session = CreateSession(await Connection.GetInternalConnectionAsync().ConfigureAwait(false));
                    _link = CreateLink(_session);
                }
            }
            finally
            {
                _mySemaphoreSlim.Release();
            }
        }

        public bool IsClosed { get; private set; }

        public async Task CloseAsync()
        {
            if (IsClosed)
            {
                return;
            }
            IsClosed = true;
            if (_session != null && !_session.IsClosed)
            {
                await OnSessionClosingAsync().ConfigureAwait(false);
                await _session.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}

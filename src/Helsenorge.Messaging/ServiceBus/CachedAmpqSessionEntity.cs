/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
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

namespace Helsenorge.Messaging.ServiceBus
{
    internal abstract class CachedAmpqSessionEntity<TLink> : ICachedMessagingEntity
        where TLink : Link
    {
        protected readonly ServiceBusConnection Connection;
        protected ISession _session;
        protected TLink _link;
        
        private readonly SemaphoreSlim _mySemaphoreSlim = new SemaphoreSlim(1);

        protected CachedAmpqSessionEntity(ServiceBusConnection connection)
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

        protected async Task OnSessionClosing()
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

        protected async Task EnsureOpen()
        {
            await _mySemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                CheckNotClosed();
                if (await Connection.EnsureConnection().ConfigureAwait(false) || _session == null || _session.IsClosed || _link == null || _link.IsClosed)
                {
                    if (_link != null && !_link.IsClosed)
                    {
                        await _link.CloseAsync().ConfigureAwait(false);
                    }

                    if (_session != null && !_session.IsClosed)
                    {
                        await _session.CloseAsync().ConfigureAwait(false);
                    }

                    _session = CreateSession(await Connection.GetConnection().ConfigureAwait(false));
                    _link = CreateLink(_session);
                }
            }
            finally
            {
                _mySemaphoreSlim.Release();
            }
        }

        public bool IsClosed { get; private set; }

        public async Task Close()
        {
            if (IsClosed)
            {
                return;
            }
            IsClosed = true;
            if (_session != null && !_session.IsClosed)
            {
                await OnSessionClosing().ConfigureAwait(false);
                await _session.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}

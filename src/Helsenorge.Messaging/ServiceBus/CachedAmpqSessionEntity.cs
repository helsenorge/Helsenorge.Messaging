/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

﻿using System;
using System.Threading.Tasks;
using Amqp;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.ServiceBus
{
    internal abstract class CachedAmpqSessionEntity<TLink> : ICachedMessagingEntity
        where TLink : Link
    {
        protected readonly ServiceBusConnection Connection;
        protected Session _session;
        protected TLink _link;

        protected CachedAmpqSessionEntity(ServiceBusConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Method is called every time it's needed to renew the link,
        /// e.g. when connection is closed or session is expired etc.
        /// </summary>
        protected abstract TLink CreateLink(ISession session);

        protected Session CreateSession(Connection connection)
        {
            return ((IConnection)connection).CreateSession() as Session;
        }

        protected async Task OnSessionClosing()
        {
            if (_link == null || _link.IsClosed)
            {
                return;
            }
            await _link.CloseAsync();
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
            CheckNotClosed();
            if (Connection.EnsureConnection() || _session == null || _session.IsClosed || _link == null || _link.IsClosed)
            {
                if(_link != null && !_link.IsClosed)
                {
                    await _link.CloseAsync();
                }
                if (_session != null && !_session.IsClosed)
                {
                    await _session.CloseAsync();
                }
                _session = CreateSession(Connection.Connection);
                _link = CreateLink(_session);
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
                await OnSessionClosing();
                await _session.CloseAsync();
            }
        }
    }
}

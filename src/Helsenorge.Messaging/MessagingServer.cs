/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Receivers;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging
{
    /// <summary>
    /// Default implementation for <see cref="IMessagingServer"/>
    /// This must be hosted as a singleton in order to leverage connection pooling against the message bus
    /// </summary>
    public sealed class MessagingServer : MessagingCore, IMessagingServer, IMessagingNotification
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentBag<MessageListener> _listeners = new ConcurrentBag<MessageListener>();
        private readonly ConcurrentBag<Task> _tasks = new ConcurrentBag<Task>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private Func<IncomingMessage, Task> _onAsynchronousMessageReceived;
        private Func<IncomingMessage, Task> _onAsynchronousMessageReceivedStarting;
        private Func<IncomingMessage, Task> _onAsynchronousMessageReceivedCompleted;

        private Func<IncomingMessage, Task<XDocument>> _onSynchronousMessageReceived;
        private Func<IncomingMessage, Task> _onSynchronousMessageReceivedCompleted;
        private Func<IncomingMessage, Task> _onSynchronousMessageReceivedStarting;

        private Func<IMessagingMessage, Task> _onErrorMessageReceived;
        private Func<IncomingMessage, Task> _onErrorMessageReceivedStarting;

        private Func<IMessagingMessage, Exception, Task> _onUnhandledException;
        private Func<IMessagingMessage, Exception, Task> _onHandledException;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="logger">Logger used for generic messages</param>
        /// <param name="loggerFactory">Logger Factory</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        [Obsolete("This constructor is replaced by ctor(MessagingSettings, ILoggerFactory, ICollaborationProtocolRegistry, IAddressRegistry) and will be removed in a future version")]
        public MessagingServer(
            MessagingSettings settings,
            ILogger logger,
            ILoggerFactory loggerFactory,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry) : base(settings, collaborationProtocolRegistry, addressRegistry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="logger">Logger used for generic messages</param>
        /// <param name="loggerFactory">Logger Factory</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        /// <param name="certificateStore">Reference to an implementation of <see cref="ICertificateStore"/>.</param>
        [Obsolete("This constructor is replaced by ctor(MessagingSettings, ILoggerFactory, ICollaborationProtocolRegistry, IAddressRegistry, ICertificateStore) and will be removed in a future version")]
        public MessagingServer(
            MessagingSettings settings,
            ILogger logger,
            ILoggerFactory loggerFactory,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry,
            ICertificateStore certificateStore) : base(settings, collaborationProtocolRegistry, addressRegistry, certificateStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="logger">Logger used for generic messages</param>
        /// <param name="loggerFactory">Logger Factory</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        /// <param name="certificateStore">Reference to an implementation of <see cref="ICertificateStore"/>.</param>
        /// <param name="certificateValidator">Reference to an implementation of <see cref="ICertificateValidator"/>.</param>
        /// <param name="messageProtection">Reference to an implementation of <see cref="IMessageProtection"/>.</param>
        [Obsolete("This constructor is replaced by ctor(MessagingSettings, ILoggerFactory, ICollaborationProtocolRegistry, IAddressRegistry, ICertificateStore, ICertificateValidator, IMessageProtection) and will be removed in a future version")]
        public MessagingServer(
            MessagingSettings settings,
            ILogger logger,
            ILoggerFactory loggerFactory,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry,
            ICertificateStore certificateStore,
            ICertificateValidator certificateValidator,
            IMessageProtection messageProtection) : base(settings, collaborationProtocolRegistry, addressRegistry, certificateStore, certificateValidator, messageProtection)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="loggerFactory">Logger Factory</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        public MessagingServer(
            MessagingSettings settings,
            ILoggerFactory loggerFactory,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry) : base(settings, collaborationProtocolRegistry, addressRegistry)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger(nameof(MessagingServer));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="loggerFactory">Logger Factory</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        /// <param name="certificateStore">Reference to an implementation of <see cref="ICertificateStore"/>.</param>
        public MessagingServer(
            MessagingSettings settings,
            ILoggerFactory loggerFactory,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry,
            ICertificateStore certificateStore) : base(settings, collaborationProtocolRegistry, addressRegistry, certificateStore)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger(nameof(MessagingServer));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="loggerFactory">Logger Factory</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        /// <param name="certificateStore">Reference to an implementation of <see cref="ICertificateStore"/>.</param>
        /// <param name="certificateValidator">Reference to an implementation of <see cref="ICertificateValidator"/>.</param>
        /// <param name="messageProtection">Reference to an implementation of <see cref="IMessageProtection"/>.</param>
        public MessagingServer(
            MessagingSettings settings,
            ILoggerFactory loggerFactory,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry,
            ICertificateStore certificateStore,
            ICertificateValidator certificateValidator,
            IMessageProtection messageProtection) : base(settings, collaborationProtocolRegistry, addressRegistry, certificateStore, certificateValidator, messageProtection)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger(nameof(MessagingServer));
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public Task Start()
        {
            _logger.LogInformation("Messaging Server starting up");

            for (var i = 0; i < Settings.ServiceBus.Asynchronous.ProcessingTasks; i++)
            {
                _listeners.Add(new AsynchronousMessageListener(ServiceBus, _loggerFactory.CreateLogger($"AsyncListener_{i}"), this));
            }
            for (var i = 0; i < Settings.ServiceBus.Synchronous.ProcessingTasks; i++)
            {
                _listeners.Add(new SynchronousMessageListener(ServiceBus, _loggerFactory.CreateLogger($"SyncListener_{i}"), this));
            }
            for (var i = 0; i < Settings.ServiceBus.Error.ProcessingTasks; i++)
            {
                _listeners.Add(new ErrorMessageListener(ServiceBus, _loggerFactory.CreateLogger($"ErrorListener_{i}"), this));
            }
            foreach (var listener in _listeners)
            {
                _tasks.Add(Task.Run(async () => await listener.Start(_cancellationTokenSource.Token).ConfigureAwait(false)));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the server, waiting for all tasks to complete their current work
        /// </summary>
        public Task Stop()
        {
            return Stop(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Stops the server, waiting for all tasks to complete their current work
        /// </summary>
        /// <param name="timeout">The amount of time we wait for things to shut down</param>
        public async Task Stop(TimeSpan timeout)
        {
            _logger.LogInformation("Messaging Server shutting down");

            _cancellationTokenSource.Cancel();
            await Task.WhenAll(_tasks.ToArray()).ConfigureAwait(false);
            
            // when all the listeners have shut down, close down the messaging infrastructure
            await ServiceBus.SenderPool.Shutdown(_logger).ConfigureAwait(false);
            await ServiceBus.ReceiverPool.Shutdown(_logger).ConfigureAwait(false);
            await ServiceBus.FactoryPool.Shutdown(_logger).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterAsynchronousMessageReceivedCallback(Func<IncomingMessage, Task> action) => _onAsynchronousMessageReceived = action;

        async Task IMessagingNotification.NotifyAsynchronousMessageReceived(IncomingMessage message)
        {
            _logger.LogDebug("NotifyAsynchronousMessageReceived");
            if (_onAsynchronousMessageReceived != null)
            {
                await _onAsynchronousMessageReceived.Invoke(message).ConfigureAwait(false);
            }
        } 
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterAsynchronousMessageReceivedStartingCallback(Func<IncomingMessage, Task> action) => _onAsynchronousMessageReceivedStarting = action;

        async Task IMessagingNotification.NotifyAsynchronousMessageReceivedStarting(IncomingMessage message)
        {
            _logger.LogDebug("NotifyAsynchronousMessageReceivedStarting");
            if (_onAsynchronousMessageReceivedStarting != null)
            {
                await _onAsynchronousMessageReceivedStarting.Invoke(message).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterAsynchronousMessageReceivedCompletedCallback(Func<IncomingMessage, Task> action) => _onAsynchronousMessageReceivedCompleted = action;

        async Task IMessagingNotification.NotifyAsynchronousMessageReceivedCompleted(IncomingMessage message)
        {
            _logger.LogDebug("NotifyAsynchronousMessageReceivedCompleted");
            if (_onAsynchronousMessageReceivedCompleted != null)
            {
                await _onAsynchronousMessageReceivedCompleted.Invoke(message).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Registers a delegate that should be called when we receive an error message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterErrorMessageReceivedCallback(Func<IMessagingMessage, Task> action) => _onErrorMessageReceived = action;

        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterErrorMessageReceivedStartingCallback(Func<IncomingMessage, Task> action) => _onErrorMessageReceivedStarting = action;

        async Task IMessagingNotification.NotifyErrorMessageReceivedStarting(IncomingMessage message)
        {
            _logger.LogDebug("NotifyErrorMessageReceivedStarting");
            if (_onErrorMessageReceivedStarting != null)
            {
                await _onErrorMessageReceivedStarting.Invoke(message).ConfigureAwait(false);
            }
        }

        async Task IMessagingNotification.NotifyErrorMessageReceived(IMessagingMessage message)
        {
            _logger.LogDebug("NotifyErrorMessageReceived");
            if (_onErrorMessageReceived != null)
            {
                await _onErrorMessageReceived.Invoke(message).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterSynchronousMessageReceivedCallback(Func<IncomingMessage, Task<XDocument>> action) => _onSynchronousMessageReceived = action;

        async Task<XDocument> IMessagingNotification.NotifySynchronousMessageReceived(IncomingMessage message)
        {
            _logger.LogDebug("NotifySynchronousMessageReceived");
            if (_onSynchronousMessageReceived != null)
            {
                return await _onSynchronousMessageReceived.Invoke(message).ConfigureAwait(false);
            }
            return default;
        }
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterSynchronousMessageReceivedCompletedCallback(Func<IncomingMessage, Task> action) => _onSynchronousMessageReceivedCompleted = action;

        async Task IMessagingNotification.NotifySynchronousMessageReceivedCompleted(IncomingMessage message)
        {
            _logger.LogDebug("NotifySynchronousMessageReceivedCompleted");
            if (_onSynchronousMessageReceivedCompleted != null)
            {
                await _onSynchronousMessageReceivedCompleted.Invoke(message).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterSynchronousMessageReceivedStartingCallback(Func<IncomingMessage, Task> action) => _onSynchronousMessageReceivedStarting = action;

        async Task IMessagingNotification.NotifySynchronousMessageReceivedStarting(IncomingMessage message)
        {
            _logger.LogDebug("NotifySynchronousMessageReceivedStarting");
            if (_onSynchronousMessageReceivedStarting != null)
            {
                await _onSynchronousMessageReceivedStarting.Invoke(message).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Registers a delegate that should be called when we have an handled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterHandledExceptionCallback(Func<IMessagingMessage, Exception, Task> action) => _onHandledException = action;

        async Task IMessagingNotification.NotifyHandledException(IMessagingMessage message, Exception ex)
        {
            _logger.LogDebug("NotifyHandledException");
            if (_onHandledException != null)
            {
                await _onHandledException.Invoke(message, ex).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Registers a delegate that should be called when we have an unhandled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterUnhandledExceptionCallback(Func<IMessagingMessage, Exception, Task> action) => _onUnhandledException = action;

        async Task IMessagingNotification.NotifyUnhandledException(IMessagingMessage message, Exception ex)
        {
            _logger.LogDebug("NotifyUnhandledException");
            if (_onUnhandledException != null)
            {
                await _onUnhandledException.Invoke(message, ex).ConfigureAwait(false);
            }
        }
    }
}

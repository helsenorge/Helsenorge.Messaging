/* 
 * Copyright (c) 2022-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class BusManager : IBusManager
    {
        private readonly SoapServiceInvoker _invoker;
        private readonly ILogger<BusManager> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public BusManager(BusManagerSettings settings, ILogger<BusManager> logger)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _invoker = new SoapServiceInvoker(settings.WcfConfiguration);
        }

        /// <inheritdoc cref="IBusManager.GetSubscriptionsAsync"/>
        public async Task<IEnumerable<EventSubscription>> GetSubscriptionsAsync()
        {
            var subscriptions = await GetSubscriptionsAsyncInternal();
            return subscriptions.AsEnumerable();
        }

        /// <inheritdoc cref="IBusManager.SubscribeAsync"/>
        public Task<EventSubscription> SubscribeAsync(SubscriptionEventSource eventSource, string systemIdentificator, string eventName = null)
            => SubscribeAsyncInternal(eventSource, systemIdentificator, eventName ?? string.Empty);

        /// <inheritdoc cref="IBusManager.UnsubscribeAsync"/>
        public Task UnsubscribeAsync(string queueName)
            => UnsubscribeAsyncInternal(queueName);

        /// <inheritdoc cref="IBusManager.GetSubscriptionsAsync"/>
        [ExcludeFromCodeCoverage]
        protected async virtual Task<EventSubscription[]> GetSubscriptionsAsyncInternal()
            => await Invoke(sbm => sbm.GetSubscriptionsAsync(), "GetSubscriptionsAsync").ConfigureAwait(false);
        
        /// <inheritdoc cref="IBusManager.SubscribeAsync"/>
        [ExcludeFromCodeCoverage]
        protected async virtual Task<EventSubscription> SubscribeAsyncInternal(SubscriptionEventSource eventSource, string systemIdentificator, string eventName)
            => await Invoke(sbm => sbm.SubscribeAsync(eventSource, eventName, systemIdentificator), "SubscribeAsync").ConfigureAwait(false);

        /// <inheritdoc cref="IBusManager.UnsubscribeAsync"/>
        [ExcludeFromCodeCoverage]
        protected async virtual Task UnsubscribeAsyncInternal(string queueName)
            => await Invoke(sbm => sbm.UnsubscribeAsync(queueName), "UnsubscribeAsync").ConfigureAwait(false);

        [ExcludeFromCodeCoverage] // Requires wire communication.
        private Task<T> Invoke<T>(Func<IServiceBusManagerV2, Task<T>> action, string methodName)
            => _invoker.ExecuteAsync(_logger, action, methodName);
        
        [ExcludeFromCodeCoverage] // Requires wire communication.
        private Task Invoke(Func<IServiceBusManagerV2, Task> action, string methodName)
            => _invoker.ExecuteAsync(_logger, action, methodName);
    }
}

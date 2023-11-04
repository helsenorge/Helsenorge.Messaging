/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Registries.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Utilities
{
    internal class SoapServiceInvoker
    {
        private readonly WcfConfiguration _wcfConfiguration;
        private readonly ConcurrentDictionary<Type, object> FactoryCache = new ConcurrentDictionary<Type, object>();
        private readonly object _lockerObject = new object();

        public SoapServiceInvoker(WcfConfiguration wcfConfiguration)
        {
            _wcfConfiguration = wcfConfiguration ?? throw new ArgumentNullException(nameof(wcfConfiguration));
        }

        [ExcludeFromCodeCoverage] // requires wire communication
        public async Task<TResponse> ExecuteAsync<TContract, TResponse>(ILogger logger, Func<TContract, Task<TResponse>> action, string operationName)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

            ChannelFactory<TContract> factory = null;
            var channel = default(TContract);
            var response = default(TResponse);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                factory = GetChannelFactory<TContract>();
                channel = factory.CreateChannel();

                logger.LogInformation("Start-ServiceCall: {OperationName} {Address}",
                    operationName, factory.Endpoint.Address.Uri.AbsoluteUri);
                stopwatch.Start();

                response = await action(channel).ConfigureAwait(false);

                stopwatch.Stop();
                logger.LogInformation("End-ServiceCall: {OperationName} {Address} ExecutionTime: {ElapsedMilliseconds} ms",
                    operationName, factory.Endpoint.Address.Uri.AbsoluteUri, stopwatch.ElapsedMilliseconds);

                var communicatonObject = (channel as ICommunicationObject);
                communicatonObject?.Close();

                channel = default(TContract);
            }
            catch (FaultException ex)
            {
                if (factory == null) throw;

                ex.Data.Add("Endpoint-Name", factory.Endpoint.Name);
                var address = factory.Endpoint.Address.Uri.AbsoluteUri;
                ex.Data.Add("Endpoint-Address", address);
                ex.Data.Add("Endpoint-Operation", operationName);
                throw;
            }
            finally
            {
                if (!Equals(channel, default(TContract)))
                {
                    var communicatonObject = (channel as ICommunicationObject);
                    communicatonObject?.Abort();
                }
            }
            return response;
        }

        [ExcludeFromCodeCoverage] // requires wire communication
        public async Task ExecuteAsync<TContract>(ILogger logger, Func<TContract, Task> action, string operationName)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

            ChannelFactory<TContract> factory = null;
            var channel = default(TContract);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                factory = GetChannelFactory<TContract>();
                channel = factory.CreateChannel();

                logger.LogInformation("Start-ServiceCall: {OperationName} {Address}",
                    operationName, factory.Endpoint.Address.Uri.AbsoluteUri);
                stopwatch.Start();

                await action(channel).ConfigureAwait(false);

                stopwatch.Stop();
                logger.LogInformation("End-ServiceCall: {OperationName} {Address} ExecutionTime: {ElapsedMilliseconds} ms",
                    operationName, factory.Endpoint.Address.Uri.AbsoluteUri, stopwatch.ElapsedMilliseconds);

                var communicatonObject = (channel as ICommunicationObject);
                communicatonObject?.Close();

                channel = default(TContract);
            }
            catch (FaultException ex)
            {
                if (factory == null) throw;

                ex.Data.Add("Endpoint-Name", factory.Endpoint.Name);
                var address = factory.Endpoint.Address.Uri.AbsoluteUri;
                ex.Data.Add("Endpoint-Address", address);
                ex.Data.Add("Endpoint-Operation", operationName);
                throw;
            }
            finally
            {
                if (!Equals(channel, default(TContract)))
                {
                    var communicatonObject = (channel as ICommunicationObject);
                    communicatonObject?.Abort();
                }
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [ExcludeFromCodeCoverage] // requires wire communication
        internal ChannelFactory<TContract> GetChannelFactory<TContract>()
        {
            // First, try to retrieve ChannelFactory from cache without locking.
            if (FactoryCache.TryGetValue(typeof(TContract), out var factoryObject))
                return (ChannelFactory<TContract>)factoryObject;

            // If a ChannelFactory was not found, then lock and try again.
            lock (_lockerObject)
            {
                // Try to retrieve ChannelFactory from cache again, in case another thread already added it.
                if (FactoryCache.TryGetValue(typeof(TContract), out factoryObject))
                    return (ChannelFactory<TContract>)factoryObject;

                var factory = new ConfigurationChannelFactory<TContract>(_wcfConfiguration);
                FactoryCache.TryAdd(typeof(TContract), factory);
                factoryObject = factory;
            }
            return (ChannelFactory<TContract>)factoryObject;
        }
    }
}

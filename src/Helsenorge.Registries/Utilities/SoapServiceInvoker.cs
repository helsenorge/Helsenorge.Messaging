using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Utilities
{
    internal class SoapServiceInvoker
    {
        private readonly Configuration _wcfConfiguration;
        private string _userName;
        private string _password;
        private readonly ConcurrentDictionary<Type, object> FactoryCache = new ConcurrentDictionary<Type, object>();
        private readonly object _lockerObject = new object();
    
        public SoapServiceInvoker(Configuration wcfConfiguration)
        {
            if (wcfConfiguration == null) throw new ArgumentNullException(nameof(wcfConfiguration));
            _wcfConfiguration = wcfConfiguration;
        }
        public void SetClientCredentials(string userName, string password)
        {
            _userName = userName;
            _password = password;
        }
        [ExcludeFromCodeCoverage] // requires wire communication
        public async Task<TResponse> Execute<TContract, TResponse>(ILogger logger, Func<TContract, Task<TResponse>> action, string operationName,
            string endpointConfigurationName)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));
            if (string.IsNullOrEmpty(endpointConfigurationName)) throw new ArgumentNullException(nameof(endpointConfigurationName));

            ChannelFactory<TContract> factory = null;
            var channel = default(TContract);
            var response = default(TResponse);

            try
            {
                factory = GetChannelFactory<TContract>(endpointConfigurationName);
                channel = factory.CreateChannel();

                logger.LogInformation("Start-ServiceCall: {OperationName} {Address}", 
                    operationName, factory.Endpoint.Address.Uri.AbsoluteUri);

                response = await action(channel).ConfigureAwait(false);

                logger.LogInformation("End-ServiceCall: {OperationName} {Address}",
                    operationName, factory.Endpoint.Address.Uri.AbsoluteUri);
        
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
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [ExcludeFromCodeCoverage] // requires wire communication
        private ChannelFactory<TContract> GetChannelFactory<TContract>(string endpointConfigurationName)
        {
            object factoryObject;

            if (FactoryCache.TryGetValue(typeof(TContract), out factoryObject) == false)
            {
                lock (_lockerObject)
                {
                    if (FactoryCache.TryGetValue(typeof(TContract), out factoryObject) == false)
                    {
                        var factory = new ConfigurationChannelFactory<TContract>(endpointConfigurationName, _wcfConfiguration, null);

                        factory.Credentials.UserName.UserName = _userName;
                        factory.Credentials.UserName.Password = _password;

                        FactoryCache.TryAdd(typeof(TContract), factory);
                        factoryObject = factory;
                    }
                }
            }
            return (ChannelFactory<TContract>)factoryObject;
        }
    }
}

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Utilities
{
    internal class SoapServiceInvoker
    {
        private readonly SoapConfiguration _configuration;
        private readonly ConcurrentDictionary<Type, object> FactoryCache = new ConcurrentDictionary<Type, object>();
        private readonly object _lockerObject = new object();

        public SoapServiceInvoker(SoapConfiguration configuration)
        {
            _configuration = configuration;
        }

        [ExcludeFromCodeCoverage] // requires wire communication
        public async Task<TResponse> Execute<TContract, TResponse>(ILogger logger, Func<TContract, Task<TResponse>> action, string operationName,
            string endpoint)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));
            if (string.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));

            ChannelFactory<TContract> factory = null;
            var channel = default(TContract);
            var response = default(TResponse);

            try
            {
                factory = GetChannelFactory<TContract>(endpoint);
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
        private ChannelFactory<TContract> GetChannelFactory<TContract>(string endpoint)
        {
            object factoryObject;

            if (FactoryCache.TryGetValue(typeof(TContract), out factoryObject) == false)
            {
                lock (_lockerObject)
                {
                    if (FactoryCache.TryGetValue(typeof(TContract), out factoryObject) == false)
                    {
                        Uri endpointUri = new Uri(endpoint);
                        var binding = GetBinding(endpointUri);
                        var factory = new ChannelFactory<TContract>(binding, new EndpointAddress(endpointUri));
                        factory.Credentials.UserName.UserName = _configuration.UserName;
                        factory.Credentials.UserName.Password = _configuration.Password;

                        FactoryCache.TryAdd(typeof(TContract), factory);
                        factoryObject = factory;
                    }
                }
            }
            return (ChannelFactory<TContract>)factoryObject;
        }

        private Binding GetBinding(Uri endpoint)
        {
            switch (endpoint.Scheme)
            {
                case "https":
                    BasicHttpsBinding httpsBinding = new BasicHttpsBinding();
                    httpsBinding.Security.Mode = BasicHttpsSecurityMode.Transport;
                    httpsBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
                    httpsBinding.MaxBufferSize = _configuration.MaxBufferSize;
                    httpsBinding.MaxBufferPoolSize = _configuration.MaxBufferPoolSize;
                    httpsBinding.MaxReceivedMessageSize = _configuration.MaxRecievedMessageSize;
                    return httpsBinding;
                case "net.tcp":
                    NetTcpBinding netTcpBinding = new NetTcpBinding();
                    netTcpBinding.Security.Mode = SecurityMode.TransportWithMessageCredential;
                    netTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                    netTcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
                    netTcpBinding.MaxBufferSize = _configuration.MaxBufferSize;
                    netTcpBinding.MaxBufferPoolSize = _configuration.MaxBufferPoolSize;
                    netTcpBinding.MaxReceivedMessageSize = _configuration.MaxRecievedMessageSize;
                    return netTcpBinding;
                default:
                    throw new InvalidChannelBindingException($"The URI scheme '{endpoint.Scheme}' is not supported. Accepted schemes are 'https' and 'net.tcp'.");
            }
        }
    }
}

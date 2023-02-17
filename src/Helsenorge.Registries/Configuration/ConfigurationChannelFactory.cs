/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Helsenorge.Registries.Configuration
{
    internal class ConfigurationChannelFactory<TChannel> : ChannelFactory<TChannel>
    {
        public ConfigurationChannelFactory(WcfConfiguration configuration) : base(typeof(TChannel))
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrEmpty(configuration.Address))
            {
                throw new ArgumentException("Endpoint address must be specified");
            }

            var binding = CreateBinding(configuration);
            var address = CreateEndpointAddress(configuration);
            InitializeEndpoint(binding, address);

            if (!string.IsNullOrEmpty(configuration.UserName) &&
                !string.IsNullOrEmpty(configuration.Password))
            {
                Credentials.UserName.UserName = configuration.UserName;
                Credentials.UserName.Password = configuration.Password;
            }
        }

        private static Binding CreateBinding(WcfConfiguration configuration)
        {
            if (configuration.Address.StartsWith("net.tcp"))
            {
                var netTcpBinding = new NetTcpBinding(SecurityMode.TransportWithMessageCredential);
                netTcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
                if (configuration.MaxBufferSize != default)
                {
                    netTcpBinding.MaxBufferSize = configuration.MaxBufferSize.GetValueOrDefault();
                }
                if (configuration.MaxBufferPoolSize != default)
                {
                    netTcpBinding.MaxBufferPoolSize = configuration.MaxBufferPoolSize.GetValueOrDefault();
                }
                if (configuration.MaxReceivedMessageSize != default)
                {
                    netTcpBinding.MaxReceivedMessageSize = configuration.MaxReceivedMessageSize.GetValueOrDefault();
                }
                return netTcpBinding;
            }

            if (configuration.Address.StartsWith("http"))
            {
                switch (configuration.HttpBinding)
                {
                    case WcfHttpBinding.Basic:
                        var basicHttpBinding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
                        basicHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
                        if (configuration.MaxBufferSize != default)
                        {
                            basicHttpBinding.MaxBufferSize = configuration.MaxBufferSize.GetValueOrDefault();
                        }
                        if (configuration.MaxBufferPoolSize != default)
                        {
                            basicHttpBinding.MaxBufferPoolSize = configuration.MaxBufferPoolSize.GetValueOrDefault();
                        }
                        if (configuration.MaxReceivedMessageSize != default)
                        {
                            basicHttpBinding.MaxReceivedMessageSize = configuration.MaxReceivedMessageSize.GetValueOrDefault();
                        }
                        if (configuration.UseDefaultWebProxy != default) 
                        {
                            basicHttpBinding.UseDefaultWebProxy = configuration.UseDefaultWebProxy.GetValueOrDefault();
                        }
                        if (configuration.BypassProxyOnLocal != default)
                        {
                            basicHttpBinding.BypassProxyOnLocal = configuration.BypassProxyOnLocal.GetValueOrDefault();
                        }
                        if (configuration.ProxyAddress != default)
                        {
                            basicHttpBinding.ProxyAddress = configuration.ProxyAddress;
                        }
                        return basicHttpBinding;

                    case WcfHttpBinding.WsHttp:
                        var wsHttpBinding = new WSHttpBinding(SecurityMode.Transport);
                        wsHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
                        if (configuration.MaxBufferPoolSize != default)
                        {
                            wsHttpBinding.MaxBufferPoolSize = configuration.MaxBufferPoolSize.GetValueOrDefault();
                        }
                        if (configuration.MaxReceivedMessageSize != default)
                        {
                            wsHttpBinding.MaxReceivedMessageSize = configuration.MaxReceivedMessageSize.GetValueOrDefault();
                        }
                        if (configuration.UseDefaultWebProxy != default)
                        {
                            wsHttpBinding.UseDefaultWebProxy = configuration.UseDefaultWebProxy.GetValueOrDefault();
                        }
                        if (configuration.BypassProxyOnLocal != default)
                        {
                            wsHttpBinding.BypassProxyOnLocal = configuration.BypassProxyOnLocal.GetValueOrDefault();
                        }
                        if (configuration.ProxyAddress != default)
                        {
                            wsHttpBinding.ProxyAddress = configuration.ProxyAddress;
                        }
                        return wsHttpBinding;
                }
            }

            throw new NotSupportedException($"Endpoint address {configuration.Address} not supported");
        }

        private static EndpointAddress CreateEndpointAddress(WcfConfiguration configuration)
        {
            if (configuration.Address.StartsWith("net.tcp"))
            {
                var uri = new Uri(configuration.Address);
                var endpointIdentity = new DnsEndpointIdentity(uri.Host);
                return new EndpointAddress(uri, endpointIdentity);
            }

            if (configuration.Address.StartsWith("http"))
            {
                return new EndpointAddress(configuration.Address);
            }

            throw new NotSupportedException($"Endpoint address {configuration.Address} not supported");
        }
    }
}

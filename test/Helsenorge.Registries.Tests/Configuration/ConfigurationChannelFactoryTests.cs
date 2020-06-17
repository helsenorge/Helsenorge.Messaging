using System;
using System.ServiceModel;
using Helsenorge.Registries.AddressService;
using Helsenorge.Registries.Configuration;
using Xunit;

namespace Helsenorge.Registries.Tests.Configuration
{
    public class ConfigurationChannelFactoryTests
    {
        [Fact]
        public void Should_Throw_Exception_If_Configuration_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigurationChannelFactory<ICommunicationPartyService>(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Throw_Exception_If_Endpoint_Address_Is_Not_Specified(string address)
        {
            Assert.Throws<ArgumentException>(() => new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = address
            }));
        }

        [Fact]
        public void Should_Throw_Exception_If_Address_Scheme_Is_Not_Supported()
        {
            Assert.Throws<NotSupportedException>(() => new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "some://address"
            }));
        }

        [Fact]
        public void Should_Create_Channel_Factory_With_Basic_Http_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic"
            });
            var binding = factory.Endpoint.Binding as BasicHttpBinding;
            Assert.NotNull(binding);
            Assert.NotNull(binding.Security.Transport);
            Assert.Equal(BasicHttpSecurityMode.Transport, binding.Security.Mode);
            Assert.Equal(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.Null(factory.Credentials?.UserName.UserName);
            Assert.Null(factory.Credentials?.UserName.Password);
        }

        [Fact]
        public void Should_Support_Additional_Parameters_For_Basic_Http_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic",
                UserName = "user",
                Password = "pass",
                MaxBufferSize = 1001,
                MaxBufferPoolSize = 2001,
                MaxReceivedMessageSize = 30001
            });
            var binding = factory.Endpoint.Binding as BasicHttpBinding;
            Assert.NotNull(binding);
            Assert.NotNull(binding.Security.Transport);
            Assert.Equal(BasicHttpSecurityMode.Transport, binding.Security.Mode);
            Assert.Equal(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.Equal(1001, binding.MaxBufferSize);
            Assert.Equal(2001, binding.MaxBufferPoolSize);
            Assert.Equal(30001, binding.MaxReceivedMessageSize);
            Assert.NotNull(factory.Credentials?.UserName.UserName);
            Assert.NotNull(factory.Credentials?.UserName.Password);
            Assert.Equal("user", factory.Credentials.UserName.UserName);
            Assert.Equal("pass", factory.Credentials.UserName.Password);
        }

        [Fact]
        public void Should_Create_Channel_Factory_With_Ws_Http_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR",
                HttpBinding = WcfHttpBinding.WsHttp
            });
            var binding = factory.Endpoint.Binding as WSHttpBinding;
            Assert.NotNull(binding);
            Assert.NotNull(binding.Security.Transport);
            Assert.Equal(SecurityMode.Transport, binding.Security.Mode);
            Assert.Equal(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.Null(factory.Credentials?.UserName.UserName);
            Assert.Null(factory.Credentials?.UserName.Password);
        }

        [Fact]
        public void Should_Support_Additional_Parameters_For_Ws_Http_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR",
                HttpBinding = WcfHttpBinding.WsHttp,
                UserName = "user",
                Password = "pass",
                MaxBufferSize = 1001,
                MaxBufferPoolSize = 2001,
                MaxReceivedMessageSize = 30001
            });
            var binding = factory.Endpoint.Binding as WSHttpBinding;
            Assert.NotNull(binding);
            Assert.NotNull(binding.Security.Transport);
            Assert.Equal(SecurityMode.Transport, binding.Security.Mode);
            Assert.Equal(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.Equal(2001, binding.MaxBufferPoolSize);
            Assert.Equal(30001, binding.MaxReceivedMessageSize);
            Assert.NotNull(factory.Credentials?.UserName.UserName);
            Assert.NotNull(factory.Credentials?.UserName.Password);
            Assert.Equal("user", factory.Credentials.UserName.UserName);
            Assert.Equal("pass", factory.Credentials.UserName.Password);
        }

        [Fact]
        public void Should_Create_Channel_Factory_With_Net_Tcp_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "net.tcp://ws-web.test.nhn.no:9876/v1/AR"
            });
            var binding = factory.Endpoint.Binding as NetTcpBinding;
            Assert.NotNull(binding);
            Assert.NotNull(binding.Security.Transport);
            Assert.Equal(SecurityMode.TransportWithMessageCredential, binding.Security.Mode);
            Assert.Equal(MessageCredentialType.UserName, binding.Security.Message.ClientCredentialType);
            Assert.Null(factory.Credentials?.UserName.UserName);
            Assert.Null(factory.Credentials?.UserName.Password);
        }

        [Fact]
        public void Should_Support_Additional_Parameters_For_Net_Tcp_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "net.tcp://ws-web.test.nhn.no:9876/v1/AR",
                UserName = "user",
                Password = "pass",
                MaxBufferSize = 1001,
                MaxBufferPoolSize = 2001,
                MaxReceivedMessageSize = 30001
            });
            var binding = factory.Endpoint.Binding as NetTcpBinding;
            Assert.NotNull(binding);
            Assert.NotNull(binding.Security.Transport);
            Assert.Equal(SecurityMode.TransportWithMessageCredential, binding.Security.Mode);
            Assert.Equal(MessageCredentialType.UserName, binding.Security.Message.ClientCredentialType);
            Assert.Equal(2001, binding.MaxBufferPoolSize);
            Assert.Equal(30001, binding.MaxReceivedMessageSize);
            Assert.NotNull(factory.Credentials?.UserName.UserName);
            Assert.NotNull(factory.Credentials?.UserName.Password);
            Assert.Equal("user", factory.Credentials.UserName.UserName);
            Assert.Equal("pass", factory.Credentials.UserName.Password);
        }
    }
}

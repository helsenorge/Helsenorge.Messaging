/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Registries.AddressService;
using Helsenorge.Registries.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ServiceModel;

namespace Helsenorge.Registries.Tests.Configuration
{
    [TestClass]
    public class ConfigurationChannelFactoryTests
    {
        [TestMethod]
        public void Should_Throw_Exception_If_Configuration_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigurationChannelFactory<ICommunicationPartyService>(null));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void Should_Throw_Exception_If_Endpoint_Address_Is_Not_Specified(string address)
        {
            Assert.Throws<ArgumentException>(() => new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = address
            }));
        }

        [TestMethod]
        public void Should_Throw_Exception_If_Address_Scheme_Is_Not_Supported()
        {
            Assert.Throws<NotSupportedException>(() => new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "some://address"
            }));
        }

        [TestMethod]
        public void Should_Create_Channel_Factory_With_Basic_Http_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic"
            });
            var binding = factory.Endpoint.Binding as BasicHttpBinding;
            Assert.IsNotNull(binding);
            Assert.IsNotNull(binding.Security.Transport);
            Assert.AreEqual(BasicHttpSecurityMode.Transport, binding.Security.Mode);
            Assert.AreEqual(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.IsNull(factory.Credentials?.UserName.UserName);
            Assert.IsNull(factory.Credentials?.UserName.Password);
            Assert.AreEqual(default, binding.ProxyAddress);
            Assert.IsFalse(binding.BypassProxyOnLocal);
            Assert.IsTrue(binding.UseDefaultWebProxy);
            Assert.AreEqual(65536, binding.MaxBufferSize);
            Assert.AreEqual(524288, binding.MaxBufferPoolSize);
            Assert.AreEqual(65536, binding.MaxReceivedMessageSize);
        }

        [TestMethod]
        public void Should_Create_Channel_Factory_With_Basic_Http_Binding_Configured()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                UserName = "John",
                Password = "Doe",
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic",
                UseDefaultWebProxy = false,
                BypassProxyOnLocal = true,
                ProxyAddress = new Uri("http://proxy.helsenorge.utvikling:8080"),
                MaxBufferSize = 1,
                MaxBufferPoolSize = 2,
                MaxReceivedMessageSize = 3
            });
            var binding = factory.Endpoint.Binding as BasicHttpBinding;
            Assert.IsNotNull(binding);
            Assert.AreEqual("John", factory.Credentials?.UserName.UserName);
            Assert.AreEqual("Doe", factory.Credentials?.UserName.Password);
            Assert.AreEqual("https://ws-web.test.nhn.no/v1/AR/Basic", factory.Endpoint.Address.Uri.OriginalString);
            Assert.IsTrue(binding.BypassProxyOnLocal);
            Assert.AreEqual("http://proxy.helsenorge.utvikling:8080", binding.ProxyAddress.OriginalString);
            Assert.AreEqual(1, binding.MaxBufferSize);
            Assert.AreEqual(2, binding.MaxBufferPoolSize);
            Assert.AreEqual(3, binding.MaxReceivedMessageSize);
        }

        [TestMethod]
        public void Should_Support_Additional_Parameters_For_Basic_Http_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic",
                UserName = "user",
                Password = "pass",
                MaxBufferSize = 1001,
                MaxBufferPoolSize = 2001,
                MaxReceivedMessageSize = 30001,
                ProxyAddress = new Uri("http://test.test"),
                BypassProxyOnLocal = true,
                UseDefaultWebProxy = true
            });
            var binding = factory.Endpoint.Binding as BasicHttpBinding;
            Assert.IsNotNull(binding);
            Assert.IsNotNull(binding.Security.Transport);
            Assert.AreEqual(BasicHttpSecurityMode.Transport, binding.Security.Mode);
            Assert.AreEqual(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.AreEqual(1001, binding.MaxBufferSize);
            Assert.AreEqual(2001, binding.MaxBufferPoolSize);
            Assert.AreEqual(30001, binding.MaxReceivedMessageSize);
            Assert.IsNotNull(factory.Credentials?.UserName.UserName);
            Assert.IsNotNull(factory.Credentials?.UserName.Password);
            Assert.AreEqual("user", factory.Credentials.UserName.UserName);
            Assert.AreEqual("pass", factory.Credentials.UserName.Password);
            Assert.AreEqual(new Uri("http://test.test").AbsoluteUri, binding.ProxyAddress.AbsoluteUri);
            Assert.IsTrue(binding.BypassProxyOnLocal);
            Assert.IsTrue(binding.UseDefaultWebProxy);
        }

        [TestMethod]
        public void Should_Create_Channel_Factory_With_Ws_Http_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR",
                HttpBinding = WcfHttpBinding.WsHttp
            });
            var binding = factory.Endpoint.Binding as WSHttpBinding;
            Assert.IsNotNull(binding);
            Assert.IsNotNull(binding.Security.Transport);
            Assert.AreEqual(SecurityMode.Transport, binding.Security.Mode);
            Assert.AreEqual(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.IsNull(factory.Credentials?.UserName.UserName);
            Assert.IsNull(factory.Credentials?.UserName.Password);
        }

        [TestMethod]
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
            Assert.IsNotNull(binding);
            Assert.IsNotNull(binding.Security.Transport);
            Assert.AreEqual(SecurityMode.Transport, binding.Security.Mode);
            Assert.AreEqual(HttpClientCredentialType.Basic, binding.Security.Transport.ClientCredentialType);
            Assert.AreEqual(2001, binding.MaxBufferPoolSize);
            Assert.AreEqual(30001, binding.MaxReceivedMessageSize);
            Assert.IsNotNull(factory.Credentials?.UserName.UserName);
            Assert.IsNotNull(factory.Credentials?.UserName.Password);
            Assert.AreEqual("user", factory.Credentials.UserName.UserName);
            Assert.AreEqual("pass", factory.Credentials.UserName.Password);
        }

        [TestMethod]
        public void Should_Create_Channel_Factory_With_Net_Tcp_Binding()
        {
            var factory = new ConfigurationChannelFactory<ICommunicationPartyService>(new WcfConfiguration
            {
                Address = "net.tcp://ws-web.test.nhn.no:9876/v1/AR"
            });
            var binding = factory.Endpoint.Binding as NetTcpBinding;
            Assert.IsNotNull(binding);
            Assert.IsNotNull(binding.Security.Transport);
            Assert.AreEqual(SecurityMode.TransportWithMessageCredential, binding.Security.Mode);
            Assert.AreEqual(MessageCredentialType.UserName, binding.Security.Message.ClientCredentialType);
            Assert.IsNull(factory.Credentials?.UserName.UserName);
            Assert.IsNull(factory.Credentials?.UserName.Password);
        }

        [TestMethod]
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
            Assert.IsNotNull(binding);
            Assert.IsNotNull(binding.Security.Transport);
            Assert.AreEqual(SecurityMode.TransportWithMessageCredential, binding.Security.Mode);
            Assert.AreEqual(MessageCredentialType.UserName, binding.Security.Message.ClientCredentialType);
            Assert.AreEqual(2001, binding.MaxBufferPoolSize);
            Assert.AreEqual(30001, binding.MaxReceivedMessageSize);
            Assert.IsNotNull(factory.Credentials?.UserName.UserName);
            Assert.IsNotNull(factory.Credentials?.UserName.Password);
            Assert.AreEqual("user", factory.Credentials.UserName.UserName);
            Assert.AreEqual("pass", factory.Credentials.UserName.Password);
        }
    }
}

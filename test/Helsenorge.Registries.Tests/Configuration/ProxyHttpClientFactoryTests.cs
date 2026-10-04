/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Registries.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;

namespace Helsenorge.Registries.Tests.Configuration
{
    [TestClass]
    public class ProxyHttpClientFactoryTests
    {
        [TestMethod]
        public void Should_Throw_Exception_If_Configuration_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new ProxyHttpClientFactory(null));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void Should_Throw_Exception_If_Address_Is_Not_Specified(string address)
        {
            var factory = new ProxyHttpClientFactory(new RestConfiguration
            {
                Address = address
            });
            Assert.Throws<ArgumentException>(() => factory.CreateHttpClient());
        }

        [TestMethod]
        public void Should_Use_Default_HttpClient_Timeout_When_Not_Configured()
        {
            var factory = new ProxyHttpClientFactory(new RestConfiguration
            {
                Address = "https://cppa.test.grunndata.nhn.no/"
            });
            using var httpClient = factory.CreateHttpClient();
            Assert.AreEqual(new HttpClient().Timeout, httpClient.Timeout);
        }

        [TestMethod]
        public void Should_Apply_Configured_Timeout()
        {
            var timeout = TimeSpan.FromSeconds(15);
            var factory = new ProxyHttpClientFactory(new RestConfiguration
            {
                Address = "https://cppa.test.grunndata.nhn.no/",
                Timeout = timeout
            });
            using var httpClient = factory.CreateHttpClient();
            Assert.AreEqual(timeout, httpClient.Timeout);
        }
    }
}

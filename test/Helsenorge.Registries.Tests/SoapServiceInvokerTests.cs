/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Registries.AddressService;
using Helsenorge.Registries.Configuration;
using Helsenorge.Registries.CPAService;
using Helsenorge.Registries.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;

namespace Helsenorge.Registries.Tests
{
    [TestClass]
    public class SoapServiceInvokerTests
    {
        [TestMethod]
        public void Should_Return_ChannelFactory_ICommunicationPartyService_If_TContract_Is_ICommunicationPartyService()
        {
            SoapServiceInvoker invoker = new SoapServiceInvoker(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic"
            });
            
            var factory = invoker.GetChannelFactory<ICommunicationPartyService>();

            Assert.IsNotNull(factory);
            Assert.IsInstanceOfType<ChannelFactory<ICommunicationPartyService>>(factory);
        }

        [TestMethod]
        public void Should_Return_ChannelFactory_ICPPAService_If_TContract_Is_ICPPAService()
        {
            SoapServiceInvoker invoker = new SoapServiceInvoker(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic"
            });

            var factory = invoker.GetChannelFactory<ICPPAService>();

            Assert.IsNotNull(factory);
            Assert.IsInstanceOfType<ChannelFactory<ICPPAService>>(factory);
        }
    }
}

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
using System.ServiceModel;
using Xunit;

namespace Helsenorge.Registries.Tests
{
    public class SoapServiceInvokerTests
    {
        [Fact]
        public void Should_Return_ChannelFactory_ICommunicationPartyService_If_TContract_Is_ICommunicationPartyService()
        {
            SoapServiceInvoker invoker = new SoapServiceInvoker(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic"
            });
            
            var factory = invoker.GetChannelFactory<ICommunicationPartyService>();

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<ChannelFactory<ICommunicationPartyService>>(factory);
        }

        [Fact]
        public void Should_Return_ChannelFactory_ICPPAService_If_TContract_Is_ICPPAService()
        {
            SoapServiceInvoker invoker = new SoapServiceInvoker(new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic"
            });

            var factory = invoker.GetChannelFactory<ICPPAService>();

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<ChannelFactory<ICPPAService>>(factory);
        }
    }
}

/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Tests
{
    [TestClass]
    public class WindowsCertificateStoreTests
    {
        [TestMethod]
        public void WindowsCertificateStore_ctor_MissingStoreName_ExpectedArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WindowsCertificateStore(null, StoreLocation.LocalMachine));
        }

        [TestMethod]
        public void WindowsCertificateStore_ctor_MissingStoreLocation_ExpectedArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new WindowsCertificateStore(StoreName.My, null));
        }

        [TestMethod]
        public void WindowsCertificateStore_GetCertificate_ArgumentThumbprintIsStringEmpty_ExpectedArgumentException()
        {
            var store = new WindowsCertificateStore(StoreName.My, StoreLocation.LocalMachine);
            Assert.Throws<ArgumentException>(() => store.GetCertificate(string.Empty));
        }
    }
}

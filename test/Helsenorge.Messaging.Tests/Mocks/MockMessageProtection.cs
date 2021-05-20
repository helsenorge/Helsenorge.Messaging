/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Tests.Mocks
{
    public class MockMessageProtection : MessageProtection
    {
        public MockMessageProtection(X509Certificate2 signingCertificate, X509Certificate2 encryptionCertificate, X509Certificate2 legacyEncryptionCertificate = null) 
            : base(signingCertificate, encryptionCertificate, legacyEncryptionCertificate)
        {
        }

        public override Stream Protect(Stream data, X509Certificate2 encryptionCertificate)
        {
            return data;
        }

        public override Stream Unprotect(Stream data, X509Certificate2 signingCertificate)
        {
            return data;
        }
    }
}

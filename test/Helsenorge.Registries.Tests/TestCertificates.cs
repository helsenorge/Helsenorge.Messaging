/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Registries.Tests
{
    /// <summary>
    /// THese certificates are not use, they have only been generated for test purposes
    /// </summary>
    internal static class TestCertificates
    {
        public static X509Certificate2 CounterpartyPublicSignature
        {
            get
            {
                var notBefore = DateTime.Now.AddDays(-1);
                var notAfter = DateTime.Now.AddDays(1);
                var keyUsage = X509KeyUsageFlags.NonRepudiation;
                var testCertificate =
                    CertificateGenerator.GenerateSelfSignedCertificate("Test Certificate", notBefore, notAfter,
                        keyUsage);
                return testCertificate;
            }
        }

        public static X509Certificate2 CounterpartyPublicSignatureInvalidStart
        {
            get
            {
                var notBefore = DateTime.Now.AddDays(1);
                var notAfter = DateTime.Now.AddDays(2);
                var keyUsage = X509KeyUsageFlags.NonRepudiation;
                var testCertificate =
                    CertificateGenerator.GenerateSelfSignedCertificate("Test Certificate", notBefore, notAfter,
                        keyUsage);
                return testCertificate;
            }
        }

        public static X509Certificate2 CounterpartyPublicSignatureInvalidEnd
        {
            get
            {
                var notBefore = DateTime.Now.AddDays(-2);
                var notAfter = DateTime.Now.AddDays(-1);
                var keyUsage = X509KeyUsageFlags.NonRepudiation;
                var testCertificate =
                    CertificateGenerator.GenerateSelfSignedCertificate("Test Certificate", notBefore, notAfter,
                        keyUsage);
                return testCertificate;
            }
        }
    }
}

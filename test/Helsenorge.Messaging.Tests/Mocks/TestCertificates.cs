/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Messaging.Abstractions;

namespace Helsenorge.Messaging.Tests.Mocks
{
    /// <summary>
    /// THese certificates are not use, they have only been generated for test purposes
    /// </summary>
    public static class TestCertificates
    {

        public static X509Certificate2 GenerateX509Certificate2 (X509KeyUsageFlags x509KeyUsage,DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using RSA rsa = RSA.Create(2048);
            var req = new CertificateRequest("cn=HelseNorge", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509KeyUsageExtension(x509KeyUsage, true));
            var cert = req.CreateSelfSigned(notBefore, notAfter);
            return cert;
        }

    }
}

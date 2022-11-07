/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
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

namespace Helsenorge.Messaging.Tests.Mocks
{
    /// <summary>
    /// These certificates are not in test or production use, they have only been generated for test purposes.
    /// </summary>
    public static class TestCertificates
    {
        private const string HelsenorgeCertificatePassword = "drQhrTUSKyQCHYoY";
        private const string CounterpartyCertificatePassword = "XVNQ2QEL1PrREwVD";

        public static X509Certificate2 CounterpartyPublicEncryption => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Counterparty_Encryption.cer"));

        public static X509Certificate2 CounterpartyPublicSignature => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Counterparty_Signature.cer"));

        //makecert -r -pe -b 01/01/2020 -e 01/01/2030 -n "CN=XYZ Signature start" -ss my -sky 2 Counterparty_SignatureInvalidStart.cer
        public static X509Certificate2 CounterpartyPublicSignatureInvalidStart => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Counterparty_SignatureInvalidStart.cer"));

        //makecert -r -pe -b 01/01/2005 -e 01/01/2010 -n "CN=XYZ Signature end" -ss my -sky 2 Counterparty_SignatureInvalidEnd.cer
        public static X509Certificate2 CounterpartyPublicSignatureInvalidEnd => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Counterparty_SignatureInvalidEnd.cer"));

        public const string CounterpartyEncryptionThumbprint = "b1fae38326a6cefa72708f7633541262e8633b2c";
        public static X509Certificate2 CounterpartyPrivateEncryption => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Counterparty_Encryption.p12"), CounterpartyCertificatePassword.ToSecureString());

        public const string CounterpartySigntatureThumbprint = "76b0195ba41374d5f372f4b70f907e7f9725fc02";
        public static X509Certificate2 CounterpartyPrivateSigntature => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Counterparty_Signature.p12"), CounterpartyCertificatePassword.ToSecureString());

        public static X509Certificate2 HelsenorgePublicEncryption => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Helsenorge_Encryption.cer"));

        public static X509Certificate2 HelsenorgePublicEncryptionInvalid => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Helsenorge_InvalidEncryption.cer"));

        public static X509Certificate2 HelsenorgePublicSignature => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Helsenorge_Signature.cer"));

        public const string HelsenorgeEncryptionThumbprint = "fddbcfbb3231f0c66ee2168358229d3cac95e88a";
        public static X509Certificate2 HelsenorgePrivateEncryption => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Helsenorge_Encryption.p12"), HelsenorgeCertificatePassword.ToSecureString());
        
        public const string HelsenorgeEncryptionInvalidStartThumbPrint = "be26ea7a3eb54e7b00c55782304765777f854192";
        //makecert -r -pe -b 01/01/2020 -e 01/01/2030 -n "CN=XYZ Encryption start" -ss my -sky 1 Helsenorge_EncryptionInvalidStart.cer
        public static X509Certificate2 HelsenorgePrivateEncryptionInvalidStart => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Helsenorge_EncryptionInvalidStart.pfx"), HelsenorgeCertificatePassword.ToSecureString());

        public const string HelsenorgeEncryptionInvalidEndThumbprint = "be26ea7a3eb54e7b00c55782304765777f854191";
        // makecert -r -pe -b 01/01/2005 -e 01/01/2010 -n "CN=XYZ Encryption end" -ss my -sky 1 Helsenorge_EncryptionInvalidEnd.cer
        public static X509Certificate2 HelsenorgePrivateEncryptionInvalidEnd => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Helsenorge_EncryptionInvalidEnd.pfx"), HelsenorgeCertificatePassword.ToSecureString());

        public const string HelsenorgeSigntatureThumbprint = "bd302b20fcdcf3766bf0bcba485dfb4b2bfe1379";
        public static X509Certificate2 HelsenorgePrivateSigntature => new X509Certificate2(TestFileUtility.GetFullPathToFile($"Files{Path.DirectorySeparatorChar}Helsenorge_Signature.p12"), HelsenorgeCertificatePassword.ToSecureString());

        private static SecureString ToSecureString(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;
            else
            {
                var result = new SecureString();
                foreach (var c in source.ToCharArray())
                    result.AppendChar(c);
                return result;
            }
        }

        public static X509Certificate2 GenerateSelfSignedCertificate(string commonName, X509KeyUsageFlags keyUsage, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using var rsa = RSA.Create(2048);
            var certificateRequest = new CertificateRequest($"CN={commonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            certificateRequest.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));
            var selfSignedCertificate = certificateRequest.CreateSelfSigned(notBefore, notAfter);
            return selfSignedCertificate;
        }
    }
}

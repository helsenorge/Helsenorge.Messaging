using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Tests.Mocks
{
    /// <summary>
    /// THese certificates are not use, they have only been generated for test purposes
    /// </summary>
    internal static class TestCertificates
    {
        private const string HelsenorgeCertificatePassword = "drQhrTUSKyQCHYoY";
        private const string CounterpartyCertificatePassword = "XVNQ2QEL1PrREwVD";

        public static X509Certificate2 CounterpartyPublicEncryption => new X509Certificate2(@"Files\Counterparty_Encryption.cer");

        public static X509Certificate2 CounterpartyPublicSignature => new X509Certificate2(@"Files\Counterparty_Signature.cer");

        //makecert -r -pe -b 01/01/2020 -e 01/01/2030 -n "CN=XYZ Signature start" -ss my -sky 2 Counterparty_SignatureInvalidStart.cer
        public static X509Certificate2 CounterpartyPublicSignatureInvalidStart => new X509Certificate2(@"Files\Counterparty_SignatureInvalidStart.cer");

        //makecert -r -pe -b 01/01/2005 -e 01/01/2010 -n "CN=XYZ Signature end" -ss my -sky 2 Counterparty_SignatureInvalidEnd.cer
        public static X509Certificate2 CounterpartyPublicSignatureInvalidEnd => new X509Certificate2(@"Files\Counterparty_SignatureInvalidEnd.cer");

        public static X509Certificate2 CounterpartyPrivateEncryption => new X509Certificate2(@"Files\Counterparty_Encryption.p12", CounterpartyCertificatePassword.ToSecureString());

        public static X509Certificate2 CounterpartyPrivateSigntature => new X509Certificate2(@"Files\Counterparty_Signature.p12", CounterpartyCertificatePassword.ToSecureString());


        public static X509Certificate2 HelsenorgePublicEncryption => new X509Certificate2(@"Files\Helsenorge_Encryption.cer");

        public static X509Certificate2 HelsenorgePublicEncryptionInvalid => new X509Certificate2(@"Files\Helsenorge_InvalidEncryption.cer");

        public static X509Certificate2 HelsenorgePublicSignature => new X509Certificate2(@"Files\Helsenorge_Signature.cer");

        public static X509Certificate2 HelsenorgePrivateEncryption => new X509Certificate2(@"Files\Helsenorge_Encryption.p12", HelsenorgeCertificatePassword.ToSecureString());
        //makecert -r -pe -b 01/01/2020 -e 01/01/2030 -n "CN=XYZ Encryption start" -ss my -sky 1 Helsenorge_EncryptionInvalidStart.cer
        public static X509Certificate2 HelsenorgePrivateEncryptionInvalidStart => new X509Certificate2(@"Files\Helsenorge_EncryptionInvalidStart.pfx", HelsenorgeCertificatePassword.ToSecureString());
        // makecert -r -pe -b 01/01/2005 -e 01/01/2010 -n "CN=XYZ Encryption end" -ss my -sky 1 Helsenorge_EncryptionInvalidEnd.cer
        public static X509Certificate2 HelsenorgePrivateEncryptionInvalidEnd => new X509Certificate2(@"Files\Helsenorge_EncryptionInvalidEnd.pfx", HelsenorgeCertificatePassword.ToSecureString());

        public static X509Certificate2 HelsenorgePrivateSigntature => new X509Certificate2(@"Files\Helsenorge_Signature.p12", HelsenorgeCertificatePassword.ToSecureString());

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
    }
}

using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Registries.Tests
{
    /// <summary>
    /// THese certificates are not use, they have only been generated for test purposes
    /// </summary>
    internal static class TestCertificates
    {
        public static X509Certificate2 CounterpartyPublicSignature => new X509Certificate2(@"Files\Counterparty_Signature.cer");

        //makecert -r -pe -b 01/01/2020 -e 01/01/2030 -n "CN=XYZ Signature start" -ss my -sky 2 Counterparty_SignatureInvalidStart.cer
        public static X509Certificate2 CounterpartyPublicSignatureInvalidStart => new X509Certificate2(@"Files\Counterparty_SignatureInvalidStart.cer");

        //makecert -r -pe -b 01/01/2005 -e 01/01/2010 -n "CN=XYZ Signature end" -ss my -sky 2 Counterparty_SignatureInvalidEnd.cer
        public static X509Certificate2 CounterpartyPublicSignatureInvalidEnd => new X509Certificate2(@"Files\Counterparty_SignatureInvalidEnd.cer");
    }
}

using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace Helsenorge.Registries.Tests
{
    public class CertificateGenerator
    {
        public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, DateTime notBefore, DateTime notAfter, X509KeyUsageFlags keyUsage)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Set key usage
                request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, true));

                // Basic constraints
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));

                // Subject key identifier
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                // Create the self-signed certificate
                var certificate = request.CreateSelfSigned(notBefore, notAfter);

                // Export and re-import to get an X509Certificate2 with private key
                return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
            }
        }
    }
}

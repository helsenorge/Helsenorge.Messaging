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
                return request.CreateSelfSigned(notBefore, notAfter);
            }
        }

        public static X509Certificate2 GenerateCertificateAuthority(string subjectName, DateTime notBefore, DateTime notAfter)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest($"CN={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Set key usage
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

                // Basic constraints, marked as certificate authority
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

                // Subject key identifier
                request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

                // Create the self-signed certificate authority
                return request.CreateSelfSigned(notBefore, notAfter);
            }
        }

        public static X509Certificate2 GenerateCertificateIssuedBy(string subjectName, X509Certificate2 issuer, DateTime notBefore, DateTime notAfter, X509KeyUsageFlags keyUsage)
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

                // Authority key identifier
                request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(issuer, true, false));

                // Create the certificate signed by the issuer
                return request.Create(issuer, notBefore, notAfter, RandomNumberGenerator.GetBytes(16));
            }
        }
    }
}

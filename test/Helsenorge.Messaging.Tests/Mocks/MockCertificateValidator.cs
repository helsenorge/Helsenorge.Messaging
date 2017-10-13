using System;
using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.Abstractions;

namespace Helsenorge.Messaging.Tests.Mocks
{
    internal class MockCertificateValidator : ICertificateValidator
    {
        private Func<X509Certificate2, X509KeyUsageFlags, CertificateErrors> _error;

        public void SetError(Func<X509Certificate2, X509KeyUsageFlags, CertificateErrors> func) => _error = func;

        public CertificateErrors Validate(X509Certificate2 certificate, X509KeyUsageFlags usage)
        {
            if(certificate == null) return CertificateErrors.Missing;

            return _error?.Invoke(certificate, usage) ?? CertificateErrors.None;
        }
    }
}

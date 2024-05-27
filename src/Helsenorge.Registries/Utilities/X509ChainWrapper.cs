using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Registries.Utilities
{
    internal class X509ChainWrapper : IX509Chain
    {
        private readonly X509Chain _chain;

        internal X509ChainWrapper()
        {
            _chain = new X509Chain();
        }

        public X509ChainPolicy ChainPolicy => _chain.ChainPolicy;

        public X509ChainStatus[] ChainStatus => _chain.ChainStatus;

        public bool Build(X509Certificate2 certificate)
        {
            return _chain.Build(certificate);
        }

        public void Dispose()
        {
            _chain.Dispose();
        }
    }
}

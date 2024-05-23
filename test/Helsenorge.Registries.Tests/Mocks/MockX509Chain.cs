using System.Security.Cryptography.X509Certificates;
using Helsenorge.Registries.Utilities;

namespace Helsenorge.Registries.Tests.Mocks
{
    public class MockX509Chain : IX509Chain
    {
        public X509ChainPolicy ChainPolicy { get; private set; }

        public X509ChainStatus[] ChainStatus { get; private set; }

        public MockX509Chain()
        {
            ChainPolicy = new X509ChainPolicy();
        }

        public void SetChainStatus(X509ChainStatus[] statuses)
        {
            ChainStatus = statuses;
        }

        public bool Build(X509Certificate2 certificate)
        {
            return false;
        }

        public void Dispose()
        {
            // No resources to dispose
        }
    }
}

using System;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Registries.Utilities
{
    internal interface IX509Chain : IDisposable
    {
        internal X509ChainPolicy ChainPolicy { get; }
        internal X509ChainStatus[] ChainStatus { get; }
        internal bool Build(X509Certificate2 certificate);
    }
}

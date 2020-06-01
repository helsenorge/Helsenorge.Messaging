using Microsoft.Extensions.Logging;
using System.IO;

namespace Helsenorge.Messaging.Abstractions
{
    internal interface IServiceBusFactoryPool
    {
        IMessagingFactory FindNextFactory(ILogger logger);
        void RegisterAlternateMessagingFactory(IMessagingFactory factory);
        void Shutdown(ILogger logger);
        IMessagingMessage CreateMessage(ILogger logger, Stream stream);
    }
}

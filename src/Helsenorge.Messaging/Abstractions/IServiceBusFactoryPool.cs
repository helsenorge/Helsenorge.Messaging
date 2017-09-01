using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    internal interface IServiceBusFactoryPool
    {
        IMessagingFactory FindNextFactory(ILogger logger);
        void RegisterAlternateMessagingFactory(IMessagingFactory factory);
        void Shutdown(ILogger logger);
        IMessagingMessage CreateMessage(ILogger logger, Stream stream, OutgoingMessage outgoingMessage);
    }
}

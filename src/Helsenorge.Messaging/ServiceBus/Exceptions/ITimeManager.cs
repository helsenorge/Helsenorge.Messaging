using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal interface ITimeManager
    {
        Task DelayAsync(TimeSpan timeSpan);

        DateTime CurrentTimeUtc { get; }
    }
}

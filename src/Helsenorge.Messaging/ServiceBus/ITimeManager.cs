using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus
{
    internal interface ITimeManager
    {
        Task DelayAsync(TimeSpan timeSpan);

        DateTime CurrentTimeUtc { get; }
    }
}

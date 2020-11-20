using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus
{
    public class DefaultTimeManager : ITimeManager
    {
        public async Task DelayAsync(TimeSpan timeSpan)
        {
            await Task.Delay(timeSpan).ConfigureAwait(false);
        }

        public DateTime CurrentTimeUtc => DateTime.UtcNow;
    }
}

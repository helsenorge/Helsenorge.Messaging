using System;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    public static class DateTimeExtensions
    {
        public static DateTime TrimMillis(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
        }
    }
}

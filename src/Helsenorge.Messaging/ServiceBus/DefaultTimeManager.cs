/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class DefaultTimeManager : ITimeManager
    {
        public async Task DelayAsync(TimeSpan timeSpan)
        {
            await Task.Delay(timeSpan).ConfigureAwait(false);
        }

        public DateTime CurrentTimeUtc => DateTime.UtcNow;
    }
}

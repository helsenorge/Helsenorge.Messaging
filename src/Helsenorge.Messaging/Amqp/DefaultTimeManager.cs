/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Amqp
{
    internal class DefaultTimeManager : ITimeManager
    {
        public void Delay(TimeSpan timeSpan)
        {
            Thread.Sleep(timeSpan);
        }

        public async Task DelayAsync(TimeSpan timeSpan)
        {
            await Task.Delay(timeSpan).ConfigureAwait(false);
        }

        public DateTime CurrentTimeUtc => DateTime.UtcNow;
    }
}

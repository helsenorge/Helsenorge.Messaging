/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.ServiceBus.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// <code>Microsoft.Azure.ServiceBus</code>-like Retry Policy (exponential).
    /// </summary>
    internal abstract class ServiceBusOperationBase
    {
        private readonly ILogger _logger;
        private readonly string _operationName;

        private readonly DateTime _deadline;
        private int _currentRetryCount;

        private readonly TimeSpan _minimalBackoff;
        private readonly TimeSpan _maximumBackoff;
        private readonly TimeSpan _deltaBackoff;
        private readonly int _maxRetryCount;
        private readonly ITimeManager _timeManager;

        protected ServiceBusOperationBase(ServiceBusOperationBuilder builder)
        {
            _logger = builder.Logger;
            _operationName = builder.OperationName;
            _minimalBackoff = builder.MinimalBackoff;
            _maximumBackoff = builder.MaximumBackoff;
            _deltaBackoff = builder.DeltaBackoff;
            _maxRetryCount = builder.MaxRetryCount;
            _timeManager = builder.TimeManager;
            _deadline = _timeManager.CurrentTimeUtc + builder.Timeout;
        }

        protected async Task RetryAsync(Exception e)
        {
            e = e.ToServiceBusException();

            if (!CanRetry(e))
            {
                throw e;
            }

            if (++_currentRetryCount > _maxRetryCount)
            {
                throw e;
            }

            // Logic: - first use currentRetryCount to calculate the size of the interval.
            //        - then get the interval in terms of sleep time (between min and max sleep time)
            //        - if interval to large to fit inside remainingTime, we quit.
            var randomizedInterval = new Random().Next((int)(_deltaBackoff.TotalMilliseconds * 0.8), (int)(_deltaBackoff.TotalMilliseconds * 1.2));
            var increment = (Math.Pow(2, _currentRetryCount) - 1) * randomizedInterval;
            var timeToSleepMsec = Math.Min(_minimalBackoff.TotalMilliseconds + increment, _maximumBackoff.TotalMilliseconds);
            var retryAfter = TimeSpan.FromMilliseconds(timeToSleepMsec);

            var remainingTime = _deadline - _timeManager.CurrentTimeUtc;
            if (retryAfter >= remainingTime)
            {
                throw e;
            }

            _logger.LogInformation(e,
                "{0} operation encountered an exception and will retry after {1}ms",
                _operationName, retryAfter.TotalMilliseconds);

            await _timeManager.DelayAsync(retryAfter).ConfigureAwait(false);
        }

        private static bool CanRetry(Exception e)
        {
            return e is ServiceBusException serviceBusException &&
                   serviceBusException.CanRetry;
        }
    }


    internal class ServiceBusOperation<T> : ServiceBusOperationBase
    {
        private readonly Func<Task<T>> _func;

        internal ServiceBusOperation(ServiceBusOperationBuilder builder, Func<Task<T>> func) : base(builder)
        {
            _func = func;
        }

        public async Task<T> PerformAsync()
        {
            while (true)
            {
                try
                {
                    return await _func.Invoke().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await RetryAsync(e).ConfigureAwait(false);
                }
            }
        }
    }

    internal class ServiceBusOperation : ServiceBusOperationBase
    {
        private readonly Func<Task> _func;

        internal ServiceBusOperation(ServiceBusOperationBuilder builder, Func<Task> func) : base(builder)
        {
            _func = func;
        }

        public async Task PerformAsync()
        {
            while (true)
            {
                try
                {
                    await _func.Invoke().ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    await RetryAsync(e).ConfigureAwait(false);
                }
            }
        }
    }
}

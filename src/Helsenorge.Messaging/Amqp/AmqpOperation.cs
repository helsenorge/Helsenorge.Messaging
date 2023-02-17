/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Amqp.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Amqp
{
    /// <summary>
    /// The base class for Bus Operations.
    /// </summary>
    internal abstract class AmqpOperationBase
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

        protected AmqpOperationBase(AmqpOperationBuilder builder)
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

        protected void Retry(Exception e)
        {
            e = e.ToBusException();

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

            _logger.LogRetryOperationInProgress($"{_operationName} operation encountered an exception and will retry after {retryAfter.TotalMilliseconds}ms");

            _timeManager.Delay(retryAfter);
        }

        protected async Task RetryAsync(Exception e)
        {
            e = e.ToBusException();

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
            return e is BusException serviceBusException &&
                   serviceBusException.CanRetry;
        }
    }

    internal class AmqpOperation<T> : AmqpOperationBase
    {
        private readonly Func<T> _func;

        internal AmqpOperation(AmqpOperationBuilder builder, Func<T> func) : base(builder)
        {
            _func = func;
        }

        public T Perform()
        {
            while (true)
            {
                try
                {
                    return _func.Invoke();
                }
                catch (Exception e)
                {
                    Retry(e);
                }
            }
        }
    }

    internal class AmqpAsyncOperation<T> : AmqpOperationBase
    {
        private readonly Func<Task<T>> _func;

        internal AmqpAsyncOperation(AmqpOperationBuilder builder, Func<Task<T>> func) : base(builder)
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

    internal class AmqpOperation : AmqpOperationBase
    {
        private readonly Action _func;

        internal AmqpOperation(AmqpOperationBuilder builder, Action func) : base(builder)
        {
            _func = func;
        }

        public void Perform()
        {
            while (true)
            {
                try
                {
                    _func.Invoke();
                    return;
                }
                catch (Exception e)
                {
                    Retry(e);
                }
            }
        }
    }

    internal class AmqpAsyncOperation : AmqpOperationBase
    {
        private readonly Func<Task> _func;

        internal AmqpAsyncOperation(AmqpOperationBuilder builder, Func<Task> func) : base(builder)
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

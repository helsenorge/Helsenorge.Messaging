/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Bus
{
    internal class ServiceBusOperationBuilder
    {
        public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan DefaultRetryMinBackoff = TimeSpan.Zero;
        public static readonly TimeSpan DefaultRetryMaxBackoff = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan DefaultRetryDeltaBackoff = TimeSpan.FromSeconds(3);
        public const int DefaultRetryMaxCount = 5;

        public TimeSpan MinimalBackoff = DefaultRetryMinBackoff;
        public TimeSpan MaximumBackoff = DefaultRetryMaxBackoff;
        public TimeSpan DeltaBackoff = DefaultRetryDeltaBackoff;
        public int MaxRetryCount = DefaultRetryMaxCount;
        public ITimeManager TimeManager = new DefaultTimeManager();
        public TimeSpan Timeout = DefaultOperationTimeout;
        public ILogger Logger;
        public string OperationName;

        public ServiceBusOperationBuilder(ILogger logger, string operationName)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException(nameof(operationName));
            }
        }

        private void Validate()
        {
            if (Timeout < TimeSpan.Zero)
            {
                throw new ArgumentException(nameof(Timeout));
            }
        }

        public ServiceBusAsyncOperation<T> Build<T>(Func<Task<T>> func)
        {
            Validate();
            return new ServiceBusAsyncOperation<T>(this, func);
        }

        public ServiceBusAsyncOperation Build(Func<Task> func)
        {
            Validate();
            return new ServiceBusAsyncOperation(this, func);
        }

        public ServiceBusOperation<T> Build<T>(Func<T> func)
        {
            Validate();
            return new ServiceBusOperation<T>(this, func);
        }

        public ServiceBusOperation Build(Action func)
        {
            Validate();
            return new ServiceBusOperation(this, func);
        }
    }
}

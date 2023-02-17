﻿/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Helsenorge.Messaging.Tests.Mocks
{
    class MockLogger : ILogger
    {
        private readonly Func<string, LogLevel, bool> _filter; 
        private readonly string _name;
        private MockLoggerProvider _provider;

        public MockLogger(string name, MockLoggerProvider provider)
            : this(name, filter: null)
        {
            _provider = provider;
        } 
        public MockLogger(string name, Func<string, LogLevel, bool> filter)
        { 
            _name = string.IsNullOrEmpty(name) ? nameof(MockLogger) : name; 
            _filter = filter; 
        } 
        public IDisposable BeginScope<TState>(TState state)
        { 
            return NoopDisposable.Instance; 
        } 
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        } 
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        { 
            if (!IsEnabled(logLevel)) 
            { 
                return; 
            } 
            if(formatter == null) 
            { 
                throw new ArgumentNullException(nameof(formatter)); 
            } 
            var message = formatter(state, exception); 
            if (string.IsNullOrEmpty(message)) 
            { 
                return; 
            } 
            message = $"{ logLevel }: {message}"; 

            if (exception != null) 
            { 
                message += Environment.NewLine + Environment.NewLine + exception.ToString(); 
            }
            Debug.WriteLine(message, _name);

            _provider.Entries.Add(new MockLoggerProvider.Entry()
            {
                LogLevel = logLevel,
                EventId = eventId,
                Exception = exception,
                Message = formatter(state, exception)
            });
        } 
        private class NoopDisposable : IDisposable 
        { 
            public static NoopDisposable Instance = new NoopDisposable(); 
 
            public void Dispose()
            { 
            } 
        } 
    }
}

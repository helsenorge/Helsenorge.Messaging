/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace Helsenorge.Messaging.IntegrationTests
{
    public interface IWriter
    {
        void WriteLine(string str);
    }

    public class BaseTest : IWriter
    {
        public ITestOutputHelper Output { get; }

        public BaseTest(ITestOutputHelper output)
        {
            Output = output;
        }

        public void WriteLine(string str)
        {
            try
            {
                Output.WriteLine(str ?? Environment.NewLine);
            }
            catch (InvalidOperationException)
            {
                // There is no currently active test. Skipping.
            }
        }
    }

    public class XUnitLoggerProvider : ILoggerProvider
    {
        public IWriter Writer { get; }
        public LogLevel MinLevel { get; set; } = LogLevel.Information;

        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            Writer = new BaseTest(output);
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(Writer, MinLevel);
        }

        public class XUnitLogger : ILogger
        {
            public IWriter Writer { get; }
            private LogLevel _minLevel;

            public XUnitLogger(IWriter writer, LogLevel minLevel)
            {
                Writer = writer;
                _minLevel = minLevel;
                Name = nameof(XUnitLogger);
            }

            public string Name { get; set; }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (!this.IsEnabled(logLevel))
                    return;

                if (formatter == null)
                    throw new ArgumentNullException(nameof(formatter));

                string message = formatter(state, exception);
                if (string.IsNullOrEmpty(message) && exception == null)
                    return;

                string line = $"{logLevel}: {this.Name}: {message}";

                Writer.WriteLine(line);

                if (exception != null)
                    Writer.WriteLine(exception.ToString());
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= _minLevel;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return new XUnitScope();
            }
        }

        public class XUnitScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}

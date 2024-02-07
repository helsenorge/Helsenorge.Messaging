/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Tests.Mocks
{
    public class MockLoggerProvider : ILoggerProvider
    {
        // teh server creates multiple loggers, this acts as the hub for all logger output
        public List<Entry> Entries { get; set; } = new List<Entry>();
        private readonly Func<string, LogLevel, bool> _filter; 

         public MockLoggerProvider(Func<string, LogLevel, bool> filter)
         { 
             _filter = filter; 
         } 

         public ILogger CreateLogger(string name)
         { 
             return new MockLogger(name, this); 
         } 
 
 
         public void Dispose()
         {             
         }

        public Entry FindEntry(EventId id)
        {
            var r = (from e in Entries where e.EventId.Id == id.Id && e.EventId.Name == id.Name select e).FirstOrDefault();
            return r;
        }

        public class Entry
        {
            public LogLevel LogLevel { get; set; }
            public EventId EventId { get; set; }
            public Exception Exception { get; set; }
            public string Message { get; set; }
        }
    } 
}

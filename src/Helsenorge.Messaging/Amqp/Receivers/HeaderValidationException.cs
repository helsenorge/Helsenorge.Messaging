/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Helsenorge.Messaging.Amqp.Receivers
{
    [Serializable]
    internal class HeaderValidationException : Exception
    {
        public IEnumerable<string> Fields { get; set; }

        public HeaderValidationException()
        {
        }

        public HeaderValidationException(string message) : base(message)
        {
        }

        public HeaderValidationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

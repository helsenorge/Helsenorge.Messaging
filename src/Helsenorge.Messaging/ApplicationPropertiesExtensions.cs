/*
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;
using Amqp.Framing;

namespace Helsenorge.Messaging
{
    internal static class ApplicationPropertiesExtensions
    {
        public static void AddApplicationProperties(this ApplicationProperties applicationProperties, IDictionary<string, object> properties)
        {
            foreach (var property in properties)
                applicationProperties[property.Key] = property.Value;
        }
    }
}

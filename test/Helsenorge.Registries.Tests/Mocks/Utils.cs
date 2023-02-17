/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Helsenorge.Registries.Tests.Mocks
{
    internal static class Utils
    {
        public static T Deserialize<T>(XNode node)
        {
            // the type is flagged with a DataContract attribute
            if (typeof(T).IsDefined(typeof(DataContractAttribute), true) == true)
            {
                var serializer = new DataContractSerializer(typeof(T));
                var reader = node.CreateReader();
                return (T)serializer.ReadObject(reader);
            }
            throw new InvalidOperationException();
        }
    }
}

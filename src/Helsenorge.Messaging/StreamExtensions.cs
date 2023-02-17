/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Amqp.Receivers;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace Helsenorge.Messaging
{
    internal static class StreamExtensions
    {
        internal static XDocument ToXDocument(this Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            try
            {
                return XDocument.Load(stream);
            }
            catch (XmlException)
            {
                // some parties have chose to use string instead of stream when sending unecrypted XML (soap faults)
                // since the GetBody<Stream>() always returns a valid stream, it causes a problem if the original data was string

                // the general XDocument.Load() fails, then we try a fallback to a manually deserialize the content
                try
                {
                    stream.Position = 0;
                    var serializer = new DataContractSerializer(typeof(string));
                    var dictionary = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max);
                    var xmlContent = serializer.ReadObject(dictionary);

                    return XDocument.Parse(xmlContent as string);
                }
                catch (Exception ex)
                {
                    throw new PayloadDeserializationException("Could not deserialize payload", ex);
                }
            }
        }
    }
}

/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Helsenorge.Registries.Utilities
{
    internal static class XmlCacheFormatter
    {
        public static byte[] Serialize(object value)
        {
            var serializer = new DataContractSerializer(value.GetType());
            using var stream = new MemoryStream();
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
            };

            using var writer = XmlWriter.Create(stream, settings);
            serializer.WriteObject(writer, value);
            writer.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        public static async Task<T> DeserializeAsync<T>(byte[] value)
            where T : class
        {
            using var stream = new MemoryStream(value);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var serializer = new DataContractSerializer(typeof(T));
            return await Task.FromResult(serializer.ReadObject(stream) as T);
        }
    }
}

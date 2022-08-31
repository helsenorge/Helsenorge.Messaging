/* 
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Registries.Utilities
{
    /// <summary>
    /// Enumerates how to serialize/deserialize items when caching.
    /// </summary>
    public enum CacheFormatterType
    {
        /// <summary>
        /// Use the <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"/> formatter.
        /// </summary>
        BinaryFormatter,

        /// <summary>
        /// Use <see cref="System.Xml.Serialization.XmlSerializer"/>.
        /// </summary>
        XmlFormatter,
    }
}
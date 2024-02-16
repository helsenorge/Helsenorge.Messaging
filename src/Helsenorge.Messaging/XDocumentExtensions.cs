/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Helsenorge.Messaging
{
    internal static class XDocumentExtensions
    {
        /// <summary>
        /// Converts the contents of a <see cref="XDocument"/> to a <see cref="Stream"/> in UTF-8 format.
        /// </summary>
        /// <param name="document">The <see cref="XDocument"/> instance which will be converted to a <see cref="Stream"/> in UTF-8 format.</param>
        /// <returns>The contents of <see cref="XDocument"/> as an UTF-8 <see cref="Stream"/>.</returns>
        internal static Stream ToStream(this XDocument document)
        {
            return ToStream(document, Encoding.UTF8);
        }

        /// <summary>
        /// Converts the contents of a <see cref="XDocument"/> to a <see cref="Stream"/> in UTF-8 format.
        /// </summary>
        /// <param name="document">The <see cref="XDocument"/> instance which will be converted to a <see cref="Stream"/>.</param>
        /// <param name="encoding">The <see cref="Encoding"/> format to be used in the conversion</param>
        /// <returns>The contents of <see cref="XDocument"/> as an <see cref="Stream"/>.</returns>
        internal static Stream ToStream(this XDocument document, Encoding encoding)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));

            return new MemoryStream(encoding.GetBytes(document.ToString()));
        }
    }
}

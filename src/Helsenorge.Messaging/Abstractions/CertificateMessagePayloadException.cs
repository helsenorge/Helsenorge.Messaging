/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Security;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    ///     Exception related to signing certificate with inheritance to SecurityException
    /// </summary>
    public class CertificateMessagePayloadException : SecurityException
    {
        /// <summary>
        ///     Initiate a new instance
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="payload">Payload of message</param>
        public CertificateMessagePayloadException(string message, byte[] payload) : base(message)
        {
            Payload = payload;
        }

        /// <summary>
        ///     Payload of message with error signing certificate
        /// </summary>
        public byte[] Payload { get; }
    }
}

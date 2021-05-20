/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Defines the different content types available in messaging
    /// </summary>
    public static class ContentType
    {
        /// <summary>
        /// The content has been signed and encrypted
        /// </summary>
        public const string SignedAndEnveloped = "application/pkcs7-mime; smime-type=signed-and-enveloped-data";
        //public const string Signed = "application/pkcs7-mime; smime-type=signed-data";
        //public const string Enveloped = "application/pkcs7-mime; smime-type=enveloped-data";
        /// <summary>
        /// The content is plain text
        /// </summary>
        public const string Text = "text/plain";
        /// <summary>
        /// THe content is a soap fault
        /// </summary>
        public const string Soap = "application/soap+xml";
    }
}

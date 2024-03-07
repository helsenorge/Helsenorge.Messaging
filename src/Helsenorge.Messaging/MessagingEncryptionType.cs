/* 
 * Copyright (c) 2022-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Messaging
{
    /// <summary>
    /// Enum used for setting messaging encryption type 
    /// </summary>
    [Flags]
    public enum MessagingEncryptionType
    {
        /// <summary>
        /// Specifies no encryption used in messaging.
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies the AES256 encryption used in messaging.
        /// </summary>
        AES256 = 1,

        /// <summary>
        /// Specifies the TripleDES encryption used in messaging.
        /// </summary>
        TripleDES = 2,

        /// <summary>
        /// Specifies the DES encryption used in messaging.
        /// </summary>
        DES = 4,
    }
}

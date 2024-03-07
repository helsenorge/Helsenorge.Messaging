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
    public enum MessagingEncryptionType
    {
        /// <summary>
        /// Specifies the AES256 encryption used in messaging.
        /// </summary>
        AES256 = 1,

        /// <summary>
        /// Specifies the TripleDES encryption used in messaging.
        /// </summary>
        TripleDES = 2,
    }

    /// <summary>
    /// Enum used for setting messaging rejection encryption types
    /// </summary>
    [Flags]
    public enum RejectionMessagingEncryptionType
    {
        /// <summary>
        /// Specifies no encryption rejection used in messaging.
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies rejection messages encrypted using DES encryption.
        /// </summary>
        DES = 1,

        /// <summary>
        /// Specifies rejection messages encrypted using TripleDES encryption.
        /// </summary>
        TripleDES = 2,
    }
}

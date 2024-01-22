/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    ///     CouldNotVerifyCertificateException used for issues with verifying the certificate
    /// </summary>
    public class CouldNotVerifyCertificateException : Exception
    {
        /// <summary>
        ///     Initiates a new instance of CouldNotVerifyCertificateException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="herId"></param>
        public CouldNotVerifyCertificateException(string message, int herId) : base(message)
        {
            HerId = herId;
        }

        /// <summary>
        /// HerId of counterparty whose certificate could not be verified
        /// </summary>
        public int HerId { get; }
    }
}

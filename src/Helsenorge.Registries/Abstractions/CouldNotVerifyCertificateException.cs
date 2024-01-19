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
        /// <param name="innerException"></param>
        public CouldNotVerifyCertificateException(string message, Exception innerException, int herId) : base(message, innerException)
        {
            HerId = herId;
        }

        public int HerId { get; set; }
    }
}

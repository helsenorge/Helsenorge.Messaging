/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Specifies what type of error the certificate validation encountered.
    /// May be multiple errors
    /// </summary>
    [Flags]
    public enum CertificateErrors
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0,
        /// <summary>
        /// Start date is invalid
        /// </summary>
        StartDate = 1,
        /// <summary>
        /// End date is invalid
        /// </summary>
        EndDate = 2,
        /// <summary>
        /// Certificate has incorrect usage
        /// </summary>
        Usage = 4,
        /// <summary>
        /// Certificate was revoked
        /// </summary>
        Revoked = 8,
        /// <summary>
        /// Unable to determine revocation status. Service may be unavailable
        /// </summary>
        RevokedUnknown = 16,
        /// <summary>
        /// The certificate is missing
        /// </summary>
        Missing = 32
    }
}

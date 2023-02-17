/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Abstraction for the certificate store
    /// </summary>
    public interface ICertificateStore
    {
        /// <summary>
        /// Returns a certificate from a certificate store
        /// </summary>
        /// <returns></returns>
        X509Certificate2 GetCertificate(object identifier);
    }
}

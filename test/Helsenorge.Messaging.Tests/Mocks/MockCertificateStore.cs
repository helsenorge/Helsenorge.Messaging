﻿/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Messaging.Tests.Mocks
{
    public class MockCertificateStore : ICertificateStore
    {
        public X509Certificate2 GetCertificate(object thumbprint)
        {
            if (thumbprint == null) throw new ArgumentNullException(nameof(thumbprint));
            if (!(thumbprint is string)) throw new ArgumentException("Argument is expected to be of type string.", nameof(thumbprint));

            string tp = thumbprint.ToString();
            if (string.IsNullOrWhiteSpace(tp)) throw new ArgumentException($"Argument '{nameof(thumbprint)}' must contain a value.", nameof(thumbprint));

            return TestCertificates.GetCertificate(thumbprint.ToString());
        }
    }
}

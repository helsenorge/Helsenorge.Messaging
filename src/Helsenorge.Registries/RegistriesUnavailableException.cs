/*
 * Copyright (c) 2020-2026, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Exception which indicates that a registry (e.g. the CPP/CPA registry) is temporarily unavailable,
    /// i.e. the service could not be reached, timed out or returned a server error.
    /// This is a transient condition and callers should retry the operation later instead of
    /// treating the result as authoritative (e.g. do not fall back to a dummy profile or report
    /// a permanent error to the message sender).
    /// </summary>
    [Serializable]
    public class RegistriesUnavailableException : RegistriesException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public RegistriesUnavailableException()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public RegistriesUnavailableException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public RegistriesUnavailableException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}


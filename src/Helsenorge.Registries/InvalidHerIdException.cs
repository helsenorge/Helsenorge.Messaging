/*
 * Copyright (c) 2022-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Used when an invalid HerID is used against AR.
    /// </summary>
    public class InvalidHerIdException : Exception
    {
        private readonly int _herId;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="herId">The invalid HER-id.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public InvalidHerIdException(int herId, Exception innerException): base($"The HER-id {herId} is invalid", innerException)
        {
            _herId = herId;
        }

        /// <summary>
        /// Returns the invalid HER-id.
        /// </summary>
        public int HerId => _herId;
    }
}

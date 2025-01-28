/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries
{
    internal static class EventIds
    {
        private const string Name = "REG";

        public static EventId CommunicationPartyDetails = new EventId(1, Name);
        public static EventId CollaborationProfile = new EventId(2, Name);
        public static EventId CollaborationAgreement = new EventId(3, Name);
        public static EventId CerificateDetails = new EventId(4, Name);
    }

    /// <summary>
    /// Exception for registries based operations
    /// </summary>
    [Serializable]
    public class RegistriesException : Exception
    {
        /// <summary>
        /// Gets the event id to use when logging this exception
        /// </summary>
        public EventId EventId { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>
        public RegistriesException()
        {
            
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public RegistriesException(string message) : base(message)
        {
        }
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public RegistriesException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

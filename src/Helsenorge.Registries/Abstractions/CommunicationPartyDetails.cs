/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Represents information about a communication party
    /// </summary>
    public class CommunicationPartyDetails
    {
        /// <summary>
        /// The HER-ID of the communication party. This is identifies this party in the Address Registry
        /// </summary>
        public int HerId { get; set; }
        /// <summary>
        /// Name of the communication party.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Name of the queue where the party expects synchronous messages to be received
        /// </summary>
        public string SynchronousQueueName { get; set; }
        /// <summary>
        /// Name of the queue where the party expectes asynchronous message to be received
        /// </summary>
        public string AsynchronousQueueName { get; set; }
        /// <summary>
        /// Name of the queue where the party expects error messages to be received
        /// </summary>
        public string ErrorQueueName { get; set; }
        /// <summary>
        /// The HER-ID of the communication party's parent
        /// </summary>
        public int ParentHerId { get; set; }
        /// <summary>
        /// The ENH-ID of the parent organization  (ENH-ID = organisasjonsnummer)
        /// </summary>
        public int ParentOrganizationNumber  { get; set; }
        /// <summary>
        /// Name of the communication party's parent
        /// </summary>
        public string ParentName { get; set; }
        /// <summary>
        /// Set to true if communication party is active, otherwise false.
        /// </summary>
        public bool Active { get; set; }
        /// <summary>
        /// Returns true if the communication party can receive EDI message, otherwise false.
        /// </summary>
        /// <remarks>This will return true if this expression is true: Type == Service || Type == Person</remarks>
        public bool IsValidCommunicationParty { get;  set; }
        /// <summary>
        /// Type of Communication Party: Service, Person, Organization, Department, or all of the above
        /// </summary>
        public CommunicationPartyTypeEnum Type { get; set; }
    }

}

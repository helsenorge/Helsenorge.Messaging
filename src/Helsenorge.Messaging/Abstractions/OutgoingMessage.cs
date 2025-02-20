/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Xml.Linq;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Information about an outgoing message
    /// </summary>
    public class OutgoingMessage
    {
        /// <summary>
        /// The XML payload we are sending
        /// </summary>
        public XDocument Payload { get; set; }
        /// <summary>
        /// The type of message we are sending
        /// </summary>
        public string MessageFunction { get; set; }
        /// <summary>
        /// If the outgoing message is a receipt message, this is the value of the message function we are sending the receipt for.
        /// It will be used to determine the correct protocol for sending the receipt
        /// </summary>
        public string ReceiptForMessageFunction { get; set; }
        /// <summary>
        /// The HER-id of the sender
        /// </summary>
        public int FromHerId { get; set; }
        /// <summary>
        /// The HER-id of the recipient
        /// </summary>
        public int ToHerId { get; set; }
        /// <summary>
        /// The logical id of the message we are sending
        /// </summary>
        public string MessageId { get; set; }
        /// <summary>
        /// The id of the person the message belongs to. Can be empty for messages not directly tied to a person
        /// </summary>
        public string PersonalId { get; set; }
        /// <summary>
        /// Default constructor
        /// </summary>
        public OutgoingMessage()
        {
        }
    }
}

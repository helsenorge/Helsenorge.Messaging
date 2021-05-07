/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// A collaboration protocol profile provides information required to communicate with it
    /// </summary>
    [Serializable]
    public class CollaborationProtocolProfile
    {
        // These are used during serialization and deserialization since X509Certificate2 and X509Certificate are no longer serializable in .net core.
        private string _signatureCertificateBase64String;
        private string _encryptionCertificateBase64String;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CollaborationProtocolProfile()
        {
            CpaId = Guid.Empty;
        }



        /// <summary>
        /// Called on serializing the object, exports the certificate to a base64-encoded string.
        /// </summary>
        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            _encryptionCertificateBase64String = EncryptionCertificate == null 
                ? null 
                : Convert.ToBase64String(EncryptionCertificate.Export(X509ContentType.SerializedCert));
            _signatureCertificateBase64String = SignatureCertificate == null 
                ? null 
                : Convert.ToBase64String(SignatureCertificate.Export(X509ContentType.SerializedCert));
        }

        /// <summary>
        /// Called when object is deserialized, imports the certificates from the previously serialized base64-encoded string.
        /// </summary>
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            EncryptionCertificate = string.IsNullOrWhiteSpace(_encryptionCertificateBase64String) 
                ? null 
                : new X509Certificate2(Convert.FromBase64String(_encryptionCertificateBase64String));
            SignatureCertificate = string.IsNullOrWhiteSpace(_signatureCertificateBase64String) 
                ? null 
                : new X509Certificate2(Convert.FromBase64String(_signatureCertificateBase64String));
        }

        /// <summary>
        /// Name of other party
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Her Id of other communication party
        /// </summary>
        public int HerId { get; set; }
        /// <summary>
        /// Gets the CPA id this profile belongs to
        /// </summary>
        public Guid CpaId { get; set; }
        /// <summary>
        /// A list of roles that the other party provides. This can also be thougth of as services. 
        /// In practice it means what message types they support. The messaging system can support a vast array of different message types, but only a fraction may be 
        /// supported by any given counterparty
        /// </summary>
        public IList<CollaborationProtocolRole> Roles { get; set; }
        /// <summary>
        /// The public certificate used for signature validations
        /// </summary>
        [field: NonSerialized]
        public X509Certificate2 SignatureCertificate { get; set; }
        /// <summary>
        /// The public certificate used for encryption
        /// </summary>
        [field: NonSerialized]
        public X509Certificate2 EncryptionCertificate { get; set; }

        /// <summary>
        /// Finds message deatils for a specific message. 
        /// This information is required to build the correct XML document the party can consume
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForReceiveMessage(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            var message = FindMessageForReceiver(messageName);
            return message?.Parts;
        }
        /// <summary>
        /// Finds message deatils for a specific message. 
        /// This information is required to build the correct XML document the party can consume
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForSendMessage(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            var message = FindMessageForSender(messageName);
            return message?.Parts;
        }
        /// <summary>
        /// This information is required to build the correct XML document the party can consume
        /// Finds message deatils for a receipt message for a specific message. 
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForReceiveAppRec(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            return FindMessagePartsForSenderOrReceiverAppRec(messageName, (r) => r.ReceiveMessages);
        }
        /// <summary>
        /// Finds message deatils for a receipt message for a specific message. 
        /// This information is required to build the correct XML document the party can consume
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForSendAppRec(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            return FindMessagePartsForSenderOrReceiverAppRec(messageName, (r) => r.SendMessages);
        }
        /// <summary>
        /// Finds the collaboration information for a specific message
        /// Find using the ProcessSpecification name as this matches the message name in both new and old Cpp formats
        /// For response messages such as AppRec the ProcessSpecification name is not setup and the receivemessages need to be checked
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public CollaborationProtocolMessage FindMessageForSender(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            var messages = Roles.FirstOrDefault(role => role.ProcessSpecification.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));

            if (messages == null)
            {
                return Roles.SelectMany(role => role.SendMessages).FirstOrDefault((m) => m.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return messages.SendMessages.FirstOrDefault();
            }
        }
        /// <summary>
        /// Finds the collaboration information for a specific message
        /// Find using the ProcessSpecification name as this matches the message name in both new and old Cpp formats
        /// For response messages such as AppRec the ProcessSpecification name is not setup and the receivemessages need to be checked
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public CollaborationProtocolMessage FindMessageForReceiver(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            var messages = Roles.FirstOrDefault(role => role.ProcessSpecification.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));

            if (messages == null)
            {
                return Roles.SelectMany(role => role.ReceiveMessages).FirstOrDefault((m) => m.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return messages.ReceiveMessages.FirstOrDefault();
            }
        }

        private IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForSenderOrReceiverAppRec(string messageName, Func<CollaborationProtocolRole, IList<CollaborationProtocolMessage>> sendOrReceive)
        {
            foreach (var role in Roles)
            {
                var messages = sendOrReceive(role);

                var message = messages.FirstOrDefault((m) => m.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));
                // first find the role with the correct message
                if (message == null) continue;
                
                // then we find the Apprec message in the same role
                message = messages.FirstOrDefault((m) => m.Name.Equals("APPREC", StringComparison.OrdinalIgnoreCase));
                if (message != null)
                {
                    return message.Parts;
                }
            }
            return null;
        }
    }
}

﻿/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
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
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Helsenorge.Registries.Utilities;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// A collaboration protocol profile provides information required to communicate with it
    /// </summary>
    [Serializable]
    public class CollaborationProtocolProfile
    {
        private static XNamespace NameSpace = "http://www.oasis-open.org/committees/ebxml-cppa/schema/cpp-cpa-2_0.xsd";

        /// <summary>
        /// Returns a <see cref="CollaborationProtocolProfile"/> parsed from the PartyInfo element.
        /// </summary>
        /// <param name="partyInfo">An <see cref="XElement"/> containing the PartyInfo element.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Returns a <see cref="CollaborationProtocolProfile"/>.</exception>
        public static CollaborationProtocolProfile CreateFromPartyInfoElement(XElement partyInfo)
        {
            if (partyInfo == null) throw new ArgumentNullException(nameof(partyInfo));

            var collaborationProtocolProfile = new CollaborationProtocolProfile
            {
                Roles = new List<CollaborationProtocolRole>(),
                Name = partyInfo.Attribute(NameSpace + "partyName").Value,
                HerId = partyInfo.Element(NameSpace + "PartyId").Value.ToInt()
            };

            foreach (var role in partyInfo.Elements(NameSpace + "CollaborationRole"))
            {
                collaborationProtocolProfile.Roles.Add(CollaborationProtocolRole.CreateFromCollaborationRole(role, partyInfo));
            }

            XNamespace xmlSig = "http://www.w3.org/2000/09/xmldsig#";
            foreach (var certificateElement in partyInfo.Elements(NameSpace + "Certificate"))
            {
                var base64 = certificateElement.Descendants(xmlSig + "X509Certificate").First().Value;
#if NET9_0_OR_GREATER
                var certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64));
#else
                var certificate = new X509Certificate2(Convert.FromBase64String(base64));
#endif

                if (certificate.HasKeyUsage(X509KeyUsageFlags.KeyEncipherment))
                {
                    collaborationProtocolProfile.EncryptionCertificate = certificate;
                }
                else if (certificate.HasKeyUsage(X509KeyUsageFlags.NonRepudiation))
                {
                    collaborationProtocolProfile.SignatureCertificate = certificate;
                }
            }
            return collaborationProtocolProfile;
        }

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
                : Convert.ToBase64String(EncryptionCertificate.Export(X509ContentType.Cert));
            _signatureCertificateBase64String = SignatureCertificate == null
                ? null
                : Convert.ToBase64String(SignatureCertificate.Export(X509ContentType.Cert));
        }

        /// <summary>
        /// Called when object is deserialized, imports the certificates from the previously serialized base64-encoded string.
        /// </summary>
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            EncryptionCertificate = string.IsNullOrWhiteSpace(_encryptionCertificateBase64String)
                ? null
#if NET9_0_OR_GREATER
                : X509CertificateLoader.LoadCertificate(Convert.FromBase64String(_encryptionCertificateBase64String));
#else
                : new X509Certificate2(Convert.FromBase64String(_encryptionCertificateBase64String));
#endif
            SignatureCertificate = string.IsNullOrWhiteSpace(_signatureCertificateBase64String)
                ? null
#if NET9_0_OR_GREATER
                : X509CertificateLoader.LoadCertificate(Convert.FromBase64String(_signatureCertificateBase64String));
#else
                : new X509Certificate2(Convert.FromBase64String(_signatureCertificateBase64String));
#endif
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
        /// Get or set the Id for the Collaboration Protocol Profile (CPP).
        /// </summary>
        public Guid CppId { get; set; }
        /// <summary>
        /// A list of roles that the other party provides. This can also be thought of as services.
        /// In practice it means what message types they support. The messaging system can support a vast array of different message types, but only a fraction may be
        /// supported by any given counterparty.
        /// </summary>
        public IList<CollaborationProtocolRole> Roles { get; set; }
        /// <summary>
        /// The public certificate used for signature validations
        /// </summary>
        [field: NonSerialized]
        [JsonIgnore]
        public X509Certificate2 SignatureCertificate { get; set; }
        /// <summary>
        /// The public certificate used for encryption
        /// </summary>
        [field: NonSerialized]
        [JsonIgnore]
        public X509Certificate2 EncryptionCertificate { get; set; }

        /// <summary>
        /// Finds message details for a specific message.
        /// This information is required to build the correct XML document the party can consume
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForReceiveMessage(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            var message = FindMessageForReceiver(messageName);
            return message?.Parts;
        }
        /// <summary>
        /// Finds message details for a specific message.
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
        /// Finds message details for a receipt message for a specific message.
        /// </summary>
        /// <param name="messageName">i.e. DIALOG_INNBYGER_EKONTAKT, DIALOG_INNBYGGER_KOORDINATOR, etc.</param>
        /// <returns></returns>
        public IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForReceiveAppRec(string messageName)
        {
            if (string.IsNullOrEmpty(messageName)) throw new ArgumentNullException(nameof(messageName));

            return FindMessagePartsForSenderOrReceiverAppRec(messageName, (r) => r.ReceiveMessages);
        }
        /// <summary>
        /// Finds message details for a receipt message for a specific message.
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

            var messages = Roles.FirstOrDefault(role =>
                role.ProcessSpecification != null && role.ProcessSpecification.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));

            if (messages == null)
            {
                return Roles.SelectMany(role => role.SendMessages).FirstOrDefault((m) =>
                    (m.Action != null && m.Action.Equals(messageName, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Name != null && m.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase)));
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

            var messages = Roles.FirstOrDefault(role =>
                role.ProcessSpecification != null && role.ProcessSpecification.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));

            if (messages == null)
            {
                return Roles.SelectMany(role => role.ReceiveMessages).FirstOrDefault((m) =>
                    (m.Action != null && m.Action.Equals(messageName, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Name != null && m.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                return messages.ReceiveMessages.FirstOrDefault();
            }
        }

        private IEnumerable<CollaborationProtocolMessagePart> FindMessagePartsForSenderOrReceiverAppRec(string messageName, Func<CollaborationProtocolRole, IList<CollaborationProtocolMessage>> sendOrReceive)
        {
            // first find the role with the correct message
            var role = Roles.FirstOrDefault(r => r.ProcessSpecification.Name.Equals(messageName, StringComparison.OrdinalIgnoreCase));
            if (role == null)
                return null;

            var messages = sendOrReceive(role);

            // then we find the Apprec message in the same role
            var message = messages.FirstOrDefault((m) =>
                (m.Action != null && m.Action.Equals("APPREC", StringComparison.OrdinalIgnoreCase)) ||
                (m.Name != null && m.Name.Equals("APPREC", StringComparison.OrdinalIgnoreCase)));

            if (message == null)
                return null;

            return message.Parts;
        }
    }
}

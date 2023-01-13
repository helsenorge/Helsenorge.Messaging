/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// A role can be thought of as the party providing a specific service. The CollaborationRole links the role to the ProcessSpecification and messages allowed 
    /// These are defined in 2 formats; the first example is an older format and the second a newer format that better defines the messages and roles
    /// </summary>
    /// <example>
    /// <![CDATA[
    ///  <tns:CollaborationRole>
    ///  <tns:ProcessSpecification tns:name="Dialog_Innbygger_Timereservasjon" tns:version="1.1" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes/Dialog_Innbygger_Timereservasjon.xml" tns:uuid="4ab55eaa-a095-4a4f-96e4-48fbf577fe48" />
    ///   <tns:Role tns:name="DIALOG_INNBYGGER_TIMERESERVASJONsender" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes#DIALOG_INNBYGGER_TIMERESERVASJONsender" />
    ///  <tns:ApplicationCertificateRef tns:certId="enc" />
    ///  <tns:ServiceBinding>
    ///		<tns:Service tns:type="string">S-DIALOG_INNBYGGER_TIMERESERVASJON</tns:Service>
    ///		<!-- CanSend and CanReceive content omitted -->
    ///		<tns:CanSend />
    ///		<tns:CanSend />
    ///		<tns:CanReceive />
    ///		<tns:CanReceive />
    ///  </tns:ServiceBinding>
    ///	</tns:CollaborationRole>
    ///	
    /// <tns:CollaborationRole>
    ///  <tns:ProcessSpecification tns:name="Dialog_Innbygger_BehandlerOversikt" tns:version="1.0" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes/Dialog_Innbygger_behandlerOversikt.xml" tns:uuid="b86b0d41-21fd-4ab7-86f3-af633ea7b27a" />
    ///  <tns:Role tns:name="Innbygger" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes#Innbygger" />
    ///  <tns:ApplicationCertificateRef tns:certId="enc" />
    ///  <tns:ServiceBinding>
    ///    <tns:Service tns:type="string">BehandlerOversikt</tns:Service>
    ///    <tns:CanSend>
    ///      <tns:ThisPartyActionBinding tns:id="Dialog_Innbygger_Behandler_Hent" tns:action="Hent" tns:packageId="package_eKontakt_v1p2" xlink:type="simple">
    ///        <tns:BusinessTransactionCharacteristics tns:isNonRepudiationRequired="true" tns:isNonRepudiationReceiptRequired="true" tns:isConfidential="none" tns:isAuthenticated="none" tns:isTamperProof="none" tns:isAuthorizationRequired="false" tns:isIntelligibleCheckRequired="false" tns:timeToPerform="P180M" />
    ///        <tns:ChannelId>AMQPSync_30e2b6bb-277d-4507-abb5-e6685f6179bc</tns:ChannelId>
    ///      </tns:ThisPartyActionBinding>
    ///    </tns:CanSend>
    ///    <tns:CanReceive>
    ///      <tns:ThisPartyActionBinding tns:id="Dialog_Innbygger_Svar" tns:action="Svar" tns:packageId="package_dia_v1.1_hp_v1p0" xlink:type="simple">
    ///        <tns:BusinessTransactionCharacteristics tns:isNonRepudiationRequired="true" tns:isNonRepudiationReceiptRequired="true" tns:isConfidential="none" tns:isAuthenticated="none" tns:isTamperProof="none" tns:isAuthorizationRequired="false" tns:isIntelligibleCheckRequired="false" tns:timeToPerform="P180M" />
    ///        <tns:ChannelId>AMQPSync_30e2b6bb-277d-4507-abb5-e6685f6179bc</tns:ChannelId>
    ///      </tns:ThisPartyActionBinding>
    ///    </tns:CanReceive>
    ///  </tns:ServiceBinding>
    ///</tns:CollaborationRole>
    /// ]]>
    /// </example>
    [Serializable]
    public class CollaborationProtocolRole
    {
        private static XNamespace NameSpace = "http://www.oasis-open.org/committees/ebxml-cppa/schema/cpp-cpa-2_0.xsd";

        public static CollaborationProtocolRole CreateFromCollaborationRole(XContainer element, XElement partyInfo)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (partyInfo == null) throw new ArgumentNullException(nameof(partyInfo));

            var role = new CollaborationProtocolRole
            {
                ReceiveMessages = new List<CollaborationProtocolMessage>(),
                SendMessages = new List<CollaborationProtocolMessage>(),
                RoleName = element.Element(NameSpace + "Role")?.Attribute(NameSpace + "name").Value
             };

            var processSpecification = new ProcessSpecification
            {
                Name = element.Element(NameSpace + "ProcessSpecification")?.Attribute(NameSpace + "name").Value,
                VersionString = element.Element(NameSpace + "ProcessSpecification")?.Attribute(NameSpace + "version").Value
            };
            role.ProcessSpecification = processSpecification;

            var serviceBinding = element.Element(NameSpace + "ServiceBinding");
            if (serviceBinding == null) return role;

            foreach (var item in serviceBinding.Elements(NameSpace + "CanSend"))
            {
                role.SendMessages.Add(CollaborationProtocolMessage.CreateFromThisPartyActionBinding(item.Element(NameSpace + "ThisPartyActionBinding"), partyInfo, processSpecification.Name));
            }
            foreach (var item in serviceBinding.Elements(NameSpace + "CanReceive"))
            {
                role.ReceiveMessages.Add(CollaborationProtocolMessage.CreateFromThisPartyActionBinding(item.Element(NameSpace + "ThisPartyActionBinding"), partyInfo, processSpecification.Name));
            }
            return role;
        }

        /// <summary>
        /// Name of role
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// List of messages this role can send. If messages are bi-directional, the same information will be present in both the SendMessages and ReceiveMessages
        /// </summary>
        public IList<CollaborationProtocolMessage> SendMessages { get; set; }
        /// <summary>
        /// List of messages this role can receive. If messages are bi-directional, the same information will be present in both the SendMessages and ReceiveMessages
        /// </summary>
        public IList<CollaborationProtocolMessage> ReceiveMessages { get; set; }

        /// <summary>
        /// Contains information on the name and version of message
        /// </summary>
        public ProcessSpecification ProcessSpecification { get; set; }
    }
}

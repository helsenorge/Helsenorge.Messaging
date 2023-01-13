/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Helsenorge.Registries.Utilities;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// Defines the different types of delivery protocols we support
    /// </summary>
    public enum DeliveryProtocol
    {
        /// <summary>
        /// We don't have enough information to determine the protocol
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// We are using AMQP (service bus)
        /// </summary>
        Amqp = 1
    }

    /// <summary>
    /// Represents information about a message
    /// </summary>
    [Serializable]
    public class CollaborationProtocolMessage
    {
        private static XNamespace NameSpace = "http://www.oasis-open.org/committees/ebxml-cppa/schema/cpp-cpa-2_0.xsd";

        /// <summary>
        /// Returns a <see cref="CollaborationProtocolMessage"/> from a ThisPartyActionBinding XML element.
        /// </summary>
        /// <param name="thisPartyActionBinding">The ThisPartyActionBinding XML element.</param>
        /// <param name="partyInfo">The PartyInfo XML node.</param>
        /// <param name="messageFunction">The message function this <see cref="CollaborationProtocolMessage"/> represents.</param>
        /// <returns></returns>
        /// <example>
        /// <![CDATA[
        ///		<tns:ThisPartyActionBinding tns:id="Dialog_Innbygger_Ekonsultasjon-v1p1-DIALOG_INNBYGGER_EKONSULTASJONreceiver-Receive-APPREC-v1p1" tns:action="APPREC" tns:packageId="package_apprec_v1p1" xlink:type="simple">
        ///			<tns:BusinessTransactionCharacteristics tns:isNonRepudiationRequired="true" tns:isNonRepudiationReceiptRequired="true" tns:isConfidential="none" tns:isAuthenticated="none" tns:isTamperProof="none" tns:isAuthorizationRequired="false" tns:isIntelligibleCheckRequired="false" tns:timeToPerform="P180M" />
        ///			<tns:ChannelId>AMQPAsync_81b6cff2-7f96-4bae-b314-d70f7b0e1d62</tns:ChannelId>
        ///		</tns:ThisPartyActionBinding>
        /// ]]>
        /// </example>
        public static CollaborationProtocolMessage CreateFromThisPartyActionBinding(XElement thisPartyActionBinding, XContainer partyInfo, string messageFunction)
        {
            if (thisPartyActionBinding == null) throw new ArgumentNullException(nameof(thisPartyActionBinding));
            if (partyInfo == null) throw new ArgumentNullException(nameof(partyInfo));

            //	<tns:ThisPartyActionBinding tns:id="Dialog_Innbygger_Ekonsultasjon-v1p1-DIALOG_INNBYGGER_EKONSULTASJONreceiver-Receive-APPREC-v1p1" tns:action="APPREC" tns:packageId="package_apprec_v1p1" xlink:type="simple">
            //		<tns:BusinessTransactionCharacteristics tns:isNonRepudiationRequired="true" tns:isNonRepudiationReceiptRequired="true" tns:isConfidential="none" tns:isAuthenticated="none" tns:isTamperProof="none" tns:isAuthorizationRequired="false" tns:isIntelligibleCheckRequired="false" tns:timeToPerform="P180M" />
            //		<tns:ChannelId>AMQPAsync_81b6cff2-7f96-4bae-b314-d70f7b0e1d62</tns:ChannelId>
            //	</tns:ThisPartyActionBinding>
            var channelIdNode = thisPartyActionBinding.Element(NameSpace + "ChannelId");
            if (channelIdNode == null) throw new InvalidOperationException("ChannelId node is empty");

            //<tns:DeliveryChannel tns:channelId="AMQPAsync_81b6cff2-7f96-4bae-b314-d70f7b0e1d62" tns:transportId="transport_0_1" tns:docExchangeId="docexchange_async_amqp">
            //		<tns:MessagingCharacteristics />
            // </tns:DeliveryChannel>

            var transportId = (from c in partyInfo.Elements(NameSpace + "DeliveryChannel")
                    where c.Attribute(NameSpace + "channelId").Value.Equals(channelIdNode.Value)
                    select c.Attribute(NameSpace + "transportId").Value).FirstOrDefault();
            if (transportId == null) throw new InvalidOperationException("TransportId is empty");

            // <tns:Transport tns:transportId="transport_0_1">
            //	<tns:TransportSender>
            //		<tns:TransportProtocol tns:version="1.0">AMQP</tns:TransportProtocol>
            //	</tns:TransportSender>
            //	<tns:TransportReceiver>
            //		<tns:TransportProtocol tns:version="1.0">AMQP</tns:TransportProtocol>
            //		<tns:Endpoint tns:uri="sb.test.nhn.no/DigitalDialog/93238_async" />
            //	</tns:TransportReceiver>
            //</tns:Transport>

            var transportReceiverNode = (from t in partyInfo.Elements(NameSpace + "Transport")
                     where t.Attribute(NameSpace + "transportId").Value.Equals(transportId)
                     select t.Element(NameSpace + "TransportReceiver")).FirstOrDefault();
            if (transportReceiverNode == null) throw new InvalidOperationException("TransportReceiver is null");

            var packageId = thisPartyActionBinding.Attribute(NameSpace + "packageId")?.Value;

            var message = new CollaborationProtocolMessage
            {
                Name = messageFunction.ToUpper(),
                Action = thisPartyActionBinding.Attribute(NameSpace + "action").Value,
                DeliveryChannel = transportReceiverNode.Element(NameSpace + "Endpoint")?.Attribute(NameSpace + "uri")?.Value,
                DeliveryProtocol = ParseDeliveryProtocol(transportReceiverNode.Element(NameSpace + "TransportProtocol")?.Value),
                Parts = FindMessageParts(packageId, partyInfo)
            };
            return message;
        }

        private static DeliveryProtocol ParseDeliveryProtocol(string value)
        {
            switch (value)
            {
                case "AMQP":
                    return DeliveryProtocol.Amqp;
                default:
                    return DeliveryProtocol.Unknown;
            }
        }

        private static IEnumerable<CollaborationProtocolMessagePart> FindMessageParts(string packageId, XObject partyInfo)
        {
            if (partyInfo == null) throw new ArgumentNullException(nameof(partyInfo));

            //<tns:Packaging tns:id="package_dialogmld_v1p1">
            //	<tns:ProcessingCapabilities tns:parse="true" tns:generate="true" />
            //	<tns:CompositeList>
            //		<tns:Encapsulation tns:id="enc_dialogmld_v1p1" tns:mimetype="application/pkcs7-mime" tns:mimeparameters="smime-type=&quot;enveloped-data&quot;">
            //			<tns:Constituent tns:idref="message_dialogmld_v1p1" />
            //		</tns:Encapsulation>
            //		<tns:Composite tns:id="request_msg_dialogmld_v1p1" tns:mimetype="multipart/related" tns:mimeparameters="type=text/xml">
            //			<tns:Constituent tns:idref="enc_dialogmld_v1p1" />
            //		</tns:Composite>
            //	</tns:CompositeList>
            //</tns:Packaging>

            if (partyInfo.Parent == null) throw new InvalidOperationException("Cannot determine parent for partyInfo");

            var packagingNode = (from p in partyInfo.Parent.Elements(NameSpace + "Packaging")
                             where p.Attribute(NameSpace + "id").Value.Equals(packageId)
                             select p).FirstOrDefault();

            var compositeListNode = packagingNode?.Element(NameSpace + "CompositeList");
            if (compositeListNode == null) return null;

            var constituents = compositeListNode.Elements(NameSpace + "Encapsulation").Elements(NameSpace + "Constituent").ToList();
            constituents.AddRange(compositeListNode.Elements(NameSpace + "Composite").Elements(NameSpace + "Constituent"));
            if (!constituents.Any()) return null;

            var parts = new List<CollaborationProtocolMessagePart>();

            foreach (var constituent in constituents)
            {
                var simplePartId = constituent.Attribute(NameSpace + "idref").Value;
                var min = (constituent.Attribute(NameSpace + "minOccurs") == null) ? 0 : constituent.Attribute(NameSpace + "minOccurs").Value.ToInt(0);
                var max = (constituent.Attribute(NameSpace + "maxOccurs") == null) ? 1 : constituent.Attribute(NameSpace + "maxOccurs").Value.ToInt(1);

                var simpleParts = from sp in partyInfo.Parent.Elements(NameSpace + "SimplePart")
                        where sp.Attribute(NameSpace + "id").Value.Equals(simplePartId)
                        select sp;

                foreach (var part in simpleParts)
                {
                    parts.AddRange(part.Elements(NameSpace + "NamespaceSupported").Select(namespaceSupported => new CollaborationProtocolMessagePart()
                    {
                        XmlNamespace = namespaceSupported.Value,
                        XmlSchema = namespaceSupported.Attribute(NameSpace + "location").Value,
                        MinOccurrence = min,
                        MaxOccurrence = max
                    }));
                }
            }
            return parts;
        }

        /// <summary>
        /// Name of message function. i.e. DIALOG_INNBYGGER_KOORDINATOR
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Name of action. i.e. SvarLedigeTimer or APPREC
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// The delivery channel that should be used. This will be the full queue name
        /// </summary>
        public string DeliveryChannel { get; set; }
        /// <summary>
        /// The type of protocol to be used
        /// </summary>
        public DeliveryProtocol DeliveryProtocol { get; set; }
        /// <summary>
        /// A list over parts the make up the message. If a message can contain information from many different xsd's, there should be one entry per xsd.
        /// Earlier versions of some roles may not contain all xsd information, so one has to assume a default version if nothing is provided.
        /// </summary>
        public IEnumerable<CollaborationProtocolMessagePart> Parts { get; set; }
    }
}

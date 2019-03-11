using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Helsenorge.Registries.AddressService;

namespace Helsenorge.Registries
{
    /// <summary>
    /// Default implementation for interacting with Collaboration Protocol registry.
    /// This is designed so it can be used as a singleton
    /// </summary>
    public class CollaborationProtocolRegistry : ICollaborationProtocolRegistry
    {
        XNamespace _ns = "http://www.oasis-open.org/committees/ebxml-cppa/schema/cpp-cpa-2_0.xsd";

        /// <summary>
        /// A constant that is used to indicate a Dummy Collaboration Protocol Profile (CPP) in case the Communication Party completely lacks a CPP.
        /// </summary>
        /// <remarks>
        /// This will be removed in a future version without warning, since lacking a CPP is not considered good or acceptable practice
        /// </remarks>
        public const string DummyPartyName = "DummyCollaborationProtocolProfile";

        private readonly SoapServiceInvoker _invoker;
        private readonly CollaborationProtocolRegistrySettings _settings;
        private readonly IDistributedCache _cache;
        private readonly IAddressRegistry _adressRegistry;
        /// <summary>
        /// The certificate validator to use
        /// </summary>
        public ICertificateValidator CertificateValidator { get; set; }

        /// <summary>
        /// Contstructor
        /// </summary>
        /// <param name="settings">Options for this instance</param>
        /// <param name="cache">Cache implementation to use</param>
        /// <param name="adressRegistry">AdressRegistry implementation to use</param>
        public CollaborationProtocolRegistry(
            CollaborationProtocolRegistrySettings settings,
            IDistributedCache cache,
            IAddressRegistry adressRegistry)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (adressRegistry == null) throw new ArgumentNullException(nameof(adressRegistry));

            _settings = settings;
            _cache = cache;
            _adressRegistry = adressRegistry;
            _invoker = new SoapServiceInvoker(settings.WcfConfiguration);
            _invoker.SetClientCredentials(_settings.UserName, _settings.Password);
            CertificateValidator = new CertificateValidator(_settings.UseOnlineRevocationCheck);
        }

        /// <summary>
        /// Gets the CPP profile for a specific communication party
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId">Her Id of communication party</param>
        /// <returns></returns>
        public async Task<CollaborationProtocolProfile> FindProtocolForCounterpartyAsync(ILogger logger, int counterpartyHerId)
        {
            logger.LogDebug($"FindProtocolForCounterpartyAsync {counterpartyHerId}");

            var key = $"CPA_FindProtocolForCounterpartyAsync_{counterpartyHerId}";
            var result = await CacheExtensions.ReadValueFromCache<CollaborationProtocolProfile>(logger, _cache, key).ConfigureAwait(false);
            var xmlString = string.Empty;

            if (result != null)
            {
                var errors = CertificateErrors.None; 
                errors |= CertificateValidator.Validate(result.EncryptionCertificate, X509KeyUsageFlags.DataEncipherment);
                errors |= CertificateValidator.Validate(result.SignatureCertificate, X509KeyUsageFlags.NonRepudiation);
                // if the certificates are valid, only then do we return a value from the cache
                if (errors == CertificateErrors.None)
                {
                    return result;
                }
            }
            try
            {
                xmlString = await FindProtocolForCounterparty(logger, counterpartyHerId).ConfigureAwait(false);
            }
            catch (FaultException<CPAService.GenericFault> ex)
            {
                // if this happens, we fall back to the dummy profile further down
                logger.LogWarning($"Error resolving protocol for counterparty. ErrorCode: {ex.Detail.ErrorCode} Message: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                throw new RegistriesException(ex.Message, ex)
                {
                    EventId = EventIds.CollaborationProfile,
                    Data = { { "HerId", counterpartyHerId } }
                };
            }
            if (string.IsNullOrEmpty(xmlString))
            {
                result = CreateDummyCollaborationProtocolProfile(counterpartyHerId,
                    await _adressRegistry.GetCertificateDetailsForEncryptionAsync(logger, counterpartyHerId).ConfigureAwait(false),
                    await _adressRegistry.GetCertificateDetailsForValidatingSignatureAsync(logger, counterpartyHerId).ConfigureAwait(false));
            }
            else
            {
                var doc = XDocument.Parse(xmlString);
                result = doc.Root == null ? null : MapFrompartyInfo(doc.Root.Element(_ns + "PartyInfo"));
            }

            await CacheExtensions.WriteValueToCache(logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        protected virtual Task<string> FindProtocolForCounterparty(ILogger logger, int counterpartyHerId)
            => Invoke(logger, x => x.GetCppXmlForCommunicationPartyAsync(counterpartyHerId),"GetCppXmlForCommunicationPartyAsync");

        /// <summary>
        /// Finds a CPA based on an id, and returns the CPP profile for the other communication party
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id">CPA id</param>
        /// <returns></returns>
        public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(ILogger logger, Guid id)
        {
            return await FindAgreementByIdAsync(logger, id, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds a CPA based on an id, and returns the CPP profile for the other communication party
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id">CPA id</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        public async Task<CollaborationProtocolProfile> FindAgreementByIdAsync(ILogger logger, Guid id, bool forceUpdate)
        {
            logger.LogDebug($"FindAgreementByIdAsync {id}");

            var key = $"CPA_FindAgreementByIdAsync_{id}";
            var result = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<CollaborationProtocolProfile>(logger, _cache, key).ConfigureAwait(false);

            if (result != null)
            {
                var errors = CertificateErrors.None;
                errors |= CertificateValidator.Validate(result.EncryptionCertificate, X509KeyUsageFlags.DataEncipherment);
                errors |= CertificateValidator.Validate(result.SignatureCertificate, X509KeyUsageFlags.NonRepudiation);
                // if the certificates are valid, only then do we return a value from the cache
                if (errors == CertificateErrors.None)
                {
                    return result;
                }
            }

            CPAService.CpaXmlDetails details;

            try
            {
                details = await FindAgreementById(logger, id).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                throw new RegistriesException(ex.Message, ex)
                {
                    EventId = EventIds.CollaborationAgreement,
                    Data = { { "CpaId", id } }
                };
            }
        
            if (string.IsNullOrEmpty(details?.CollaborationProtocolAgreementXml)) return null;
            var doc = XDocument.Parse(details.CollaborationProtocolAgreementXml);
            if (doc.Root == null) return null;

            var node = (from x in doc.Root.Elements(_ns + "PartyInfo").Elements(_ns + "PartyId")
                where x.Value != _settings.MyHerId.ToString()
                select x.Parent).First();

            result = MapFrompartyInfo(node);
            result.CpaId = id;

            await CacheExtensions.WriteValueToCache(logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual Task<CPAService.CpaXmlDetails> FindAgreementById(ILogger logger, Guid id)
            => Invoke(logger, x => x.GetCpaXmlAsync(id), "GetCpaXmlAsync");

        /// <summary>
        /// Finds the counterparty between us and some other communication party
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId">Her id of counterparty</param>
        /// <returns></returns>
        public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(ILogger logger, int counterpartyHerId)
        {
            return await FindAgreementForCounterpartyAsync(logger, counterpartyHerId, false).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds the counterparty between us and some other communication party
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId">Her id of counterparty</param>
        /// <param name="forceUpdate">Set to true to force cache update.</param>
        /// <returns></returns>
        public async Task<CollaborationProtocolProfile> FindAgreementForCounterpartyAsync(ILogger logger, int counterpartyHerId, bool forceUpdate)
        {

            var key = $"CPA_FindAgreementForCounterpartyAsync_{_settings.MyHerId}_{counterpartyHerId}";
            var result = forceUpdate ? null : await CacheExtensions.ReadValueFromCache<CollaborationProtocolProfile>(logger, _cache, key).ConfigureAwait(false);

            if (result != null)
            {
                var errors = CertificateErrors.None;
                errors |= CertificateValidator.Validate(result.EncryptionCertificate, X509KeyUsageFlags.DataEncipherment);
                errors |= CertificateValidator.Validate(result.SignatureCertificate, X509KeyUsageFlags.NonRepudiation);
                // if the certificates are valid, only then do we return a value from the cache
                if (errors == CertificateErrors.None)
                {
                    return result;
                }
            }

            CPAService.CpaXmlDetails details;

            try
            {
                details = await FindAgreementForCounterparty(logger, counterpartyHerId).ConfigureAwait(false);
            }
            catch (FaultException ex)
            {
                // if there are error getting a proper CPA, we fallback to getting CPP.
                logger.LogWarning($"Failed to resolve CPA between {_settings.MyHerId} and {counterpartyHerId}. {ex.Message}");
                return await FindProtocolForCounterpartyAsync(logger, counterpartyHerId).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(details?.CollaborationProtocolAgreementXml)) return null;
            var doc = XDocument.Parse(details.CollaborationProtocolAgreementXml);
            if (doc.Root == null) return null;

            var node = (from x in doc.Root.Elements(_ns + "PartyInfo").Elements(_ns + "PartyId")
                        where x.Value != _settings.MyHerId.ToString()
                        select x.Parent).First();

            result = MapFrompartyInfo(node);
            result.CpaId = Guid.Parse(doc.Root.Attribute(_ns + "cpaid").Value);
            
            await CacheExtensions.WriteValueToCache(logger, _cache, key, result, _settings.CachingInterval).ConfigureAwait(false);
            return result;
        }
        
        private CollaborationProtocolProfile CreateDummyCollaborationProtocolProfile(int herId, Abstractions.CertificateDetails encryptionCertificate, Abstractions.CertificateDetails signatureCertificate)
        {
            return new CollaborationProtocolProfile
            {
                Roles = new List<CollaborationProtocolRole>(),
                HerId = herId,
                Name = DummyPartyName,
                EncryptionCertificate = encryptionCertificate?.Certificate,
                SignatureCertificate = signatureCertificate?.Certificate
            };
        }

        /// <summary>
        /// Makes the actual call to the registry. Virtual so that it can overriden by mocks.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="counterpartyHerId"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage] // requires wire communication
        internal virtual Task<CPAService.CpaXmlDetails> FindAgreementForCounterparty(ILogger logger, int counterpartyHerId)
            => Invoke(logger, x => x.GetCpaForCommunicationPartiesXmlAsync(_settings.MyHerId, counterpartyHerId), "GetCpaForCommunicationPartiesXmlAsync");

        [ExcludeFromCodeCoverage] // requires wire communication
        private Task<T> Invoke<T>(ILogger logger, Func<CPAService.ICPPAService, Task<T>> action, string methodName)
            => _invoker.Execute(logger, action, methodName, _settings.EndpointName);

        [ExcludeFromCodeCoverage] // requires wire communication
        private Task<T> Invoke<T>(ILogger logger, Func<ICommunicationPartyService, Task<T>> action, string methodName)
            => _invoker.Execute(logger, action, methodName, _settings.EndpointName);

        private CollaborationProtocolProfile MapFrompartyInfo(XElement partyInfo)
        {
            if (partyInfo == null) throw new ArgumentNullException(nameof(partyInfo));

            var cpa = new CollaborationProtocolProfile
            {
                Roles = new List<CollaborationProtocolRole>(),
                Name = partyInfo.Attribute(_ns + "partyName").Value,
                HerId = ParseInt(partyInfo.Element(_ns + "PartyId").Value, 0)
            };

            foreach (var role in partyInfo.Elements(_ns + "CollaborationRole"))
            {
                cpa.Roles.Add(CreateFromCollaborationRole(role, partyInfo));
            }

            XNamespace xmlSig = "http://www.w3.org/2000/09/xmldsig#";
            foreach (var certificate in partyInfo.Elements(_ns + "Certificate"))
            {
                var id = certificate.Attribute(_ns + "certId").Value;
                var base64 = certificate.Descendants(xmlSig + "X509Certificate").First().Value;

                if (id.Equals("enc", StringComparison.Ordinal))
                {
                    cpa.EncryptionCertificate = new X509Certificate2(Convert.FromBase64String(base64));
                }
                else
                {
                    cpa.SignatureCertificate = new X509Certificate2(Convert.FromBase64String(base64));
                }
            }
            return cpa;
        }
        private CollaborationProtocolRole CreateFromCollaborationRole(XContainer element, XElement partyInfo)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (partyInfo == null) throw new ArgumentNullException(nameof(partyInfo));

             //<tns:CollaborationRole >
             //	<tns:ProcessSpecification tns:name="Dialog_Innbygger_Ekonsultasjon" tns:version="1.1" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes/Dialog_Innbygger_Ekonsultasjon.xml" tns:uuid="FB6A0156-7EEA-4AE3-AED3-C3A15D916A1C" />
             //	<tns:Role tns:name="DIALOG_INNBYGGER_EKONSULTASJONreceiver" xlink:type="simple" xlink:href="http://www.helsedirektoratet.no/processes#DIALOG_INNBYGGER_EKONSULTASJONreceiver" />
             //	<tns:ApplicationCertificateRef tns:certId="enc" />
             //	<tns:ServiceBinding>
             //		<tns:Service tns:type="string">S-DIALOG_INNBYGGER_EKONSULTASJON</tns:Service>
             //		<tns:CanSend />
             //		<tns:CanSend />
             //		<tns:CanReceive />
             //		<tns:CanReceive />
             //	</tns:ServiceBinding>
             //</tns:CollaborationRole>

            var role = new CollaborationProtocolRole
            {
                ReceiveMessages = new List<CollaborationProtocolMessage>(),
                SendMessages = new List<CollaborationProtocolMessage>(),
                Name = element.Element(_ns + "Role")?.Attribute(_ns + "name").Value,
                VersionString = element.Element(_ns + "ProcessSpecification")?.Attribute(_ns + "version").Value,
                RoleName = element.Element(_ns + "Role")?.Attribute(_ns + "name").Value
             };

            var processSpecification = new ProcessSpecification
            {
                Name = element.Element(_ns + "ProcessSpecification")?.Attribute(_ns + "name").Value,
                VersionString = element.Element(_ns + "ProcessSpecification")?.Attribute(_ns + "version").Value
            };
            role.ProcessSpecification = processSpecification;

            var serviceBinding = element.Element(_ns + "ServiceBinding");
            if (serviceBinding == null) return role;

            foreach (var item in serviceBinding.Elements(_ns + "CanSend"))
            {
                role.SendMessages.Add(CreateFromThisPartyActionBinding(item.Element(_ns + "ThisPartyActionBinding"), partyInfo));
            }
            foreach (var item in serviceBinding.Elements(_ns + "CanReceive"))
            {
                role.ReceiveMessages.Add(CreateFromThisPartyActionBinding(item.Element(_ns + "ThisPartyActionBinding"), partyInfo));
            }
            return role;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="thisPartyActionBinding"></param>
        /// <param name="partyInfo"></param>
        /// <returns></returns>
        /// <example>
        /// <![CDATA[
        ///		<tns:ThisPartyActionBinding tns:id="Dialog_Innbygger_Ekonsultasjon-v1p1-DIALOG_INNBYGGER_EKONSULTASJONreceiver-Receive-APPREC-v1p1" tns:action="APPREC" tns:packageId="package_apprec_v1p1" xlink:type="simple">
        ///			<tns:BusinessTransactionCharacteristics tns:isNonRepudiationRequired="true" tns:isNonRepudiationReceiptRequired="true" tns:isConfidential="none" tns:isAuthenticated="none" tns:isTamperProof="none" tns:isAuthorizationRequired="false" tns:isIntelligibleCheckRequired="false" tns:timeToPerform="P180M" />
        ///			<tns:ChannelId>AMQPAsync_81b6cff2-7f96-4bae-b314-d70f7b0e1d62</tns:ChannelId>
        ///		</tns:ThisPartyActionBinding>
        /// ]]>
        /// </example>
        private CollaborationProtocolMessage CreateFromThisPartyActionBinding(XElement thisPartyActionBinding, XContainer partyInfo)
        {
            if (thisPartyActionBinding == null) throw new ArgumentNullException(nameof(thisPartyActionBinding));
            if (partyInfo == null) throw new ArgumentNullException(nameof(partyInfo));

            //	<tns:ThisPartyActionBinding tns:id="Dialog_Innbygger_Ekonsultasjon-v1p1-DIALOG_INNBYGGER_EKONSULTASJONreceiver-Receive-APPREC-v1p1" tns:action="APPREC" tns:packageId="package_apprec_v1p1" xlink:type="simple">
            //		<tns:BusinessTransactionCharacteristics tns:isNonRepudiationRequired="true" tns:isNonRepudiationReceiptRequired="true" tns:isConfidential="none" tns:isAuthenticated="none" tns:isTamperProof="none" tns:isAuthorizationRequired="false" tns:isIntelligibleCheckRequired="false" tns:timeToPerform="P180M" />
            //		<tns:ChannelId>AMQPAsync_81b6cff2-7f96-4bae-b314-d70f7b0e1d62</tns:ChannelId>
            //	</tns:ThisPartyActionBinding>
            var channelIdNode = thisPartyActionBinding.Element(_ns + "ChannelId");
            if (channelIdNode == null) throw new InvalidOperationException("ChannelId node is empty");

            //<tns:DeliveryChannel tns:channelId="AMQPAsync_81b6cff2-7f96-4bae-b314-d70f7b0e1d62" tns:transportId="transport_0_1" tns:docExchangeId="docexchange_async_amqp">
            //		<tns:MessagingCharacteristics />
            // </tns:DeliveryChannel>

            var transportId = (from c in partyInfo.Elements(_ns + "DeliveryChannel")
                    where c.Attribute(_ns + "channelId").Value.Equals(channelIdNode.Value)
                    select c.Attribute(_ns + "transportId").Value).FirstOrDefault();
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

            var transportReceiverNode = (from t in partyInfo.Elements(_ns + "Transport")
                     where t.Attribute(_ns + "transportId").Value.Equals(transportId)
                     select t.Element(_ns + "TransportReceiver")).FirstOrDefault();//.Element(_ns + "Endpoint").Attribute(_ns + "uri").Value;
            if (transportReceiverNode == null) throw new InvalidOperationException("TransportReceiver is null");

            var packageId = thisPartyActionBinding.Attribute(_ns + "packageId")?.Value;

            var message = new CollaborationProtocolMessage
            {
                Name = thisPartyActionBinding.Attribute(_ns + "action").Value,
                DeliveryChannel = transportReceiverNode.Element(_ns + "Endpoint")?.Attribute(_ns + "uri")?.Value,
                DeliveryProtocol = ParseDeliveryProtocol(transportReceiverNode.Element(_ns + "TransportProtocol")?.Value),
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
        private IEnumerable<CollaborationProtocolMessagePart> FindMessageParts(string packageId, XObject partyInfo)
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
        
            var packagingNode = (from p in partyInfo.Parent.Elements(_ns + "Packaging")
                             where p.Attribute(_ns + "id").Value.Equals(packageId)
                             select p).FirstOrDefault();

            var compositeListNode = packagingNode?.Element(_ns + "CompositeList");
            if (compositeListNode == null) return null;

            var constituents = compositeListNode.Elements(_ns + "Encapsulation").Elements(_ns + "Constituent").ToList();
            if (!constituents.Any()) return null;

            var parts = new List<CollaborationProtocolMessagePart>();

            foreach (var constituent in constituents)
            {
                var simplePartId = constituent.Attribute(_ns + "idref").Value;
                var min = (constituent.Attribute(_ns + "minOccurs") == null) ? 0 : ParseInt(constituent.Attribute(_ns + "minOccurs").Value, 0);
                var max = (constituent.Attribute(_ns + "maxOccurs") == null) ? 1 : ParseInt(constituent.Attribute(_ns + "maxOccurs").Value, 1);

                var simpleParts = from sp in partyInfo.Parent.Elements(_ns + "SimplePart")
                        where sp.Attribute(_ns + "id").Value.Equals(simplePartId)
                        select sp;

                foreach (var part in simpleParts)
                {
                    parts.AddRange(part.Elements(_ns + "NamespaceSupported").Select(namespaceSupported => new CollaborationProtocolMessagePart()
                    {
                        XmlNamespace = namespaceSupported.Value,
                        XmlSchema = namespaceSupported.Attribute(_ns + "location").Value,
                        MinOccurrence = min,
                        MaxOccurrence = max
                    }));
                }
            }
            return parts;
        }

        private static int ParseInt(string value, int defaultValue)
        {
            int i;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out i) == false ? defaultValue : i;
        }
    }
}

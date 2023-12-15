using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Helsenorge.Messaging.Amqp;

/// <summary>
/// Helper class to extract insensitive data from xml business messages
/// </summary>
public static class MetadataHelper
{
    private static XmlNamespaceManager _manager;
    private const string MsgHead = "http://www.kith.no/xmlstds/msghead/2006-05-24";
    private const string Base64Container = "http://www.kith.no/xmlstds/base64container";
    private const string Apprec10 = "http://www.kith.no/xmlstds/apprec/2004-11-21";
    private const string Apprec11 = "http://www.kith.no/xmlstds/apprec/2012-02-15";

    //prefixes
    private const string MH = "mh";
    private const string A10 = "a10";
    private const string A11 = "a11";
    private const string B = "b";

    static MetadataHelper()
    {
        _manager = new XmlNamespaceManager(new NameTable());
        _manager.AddNamespace(MH, MsgHead);
        _manager.AddNamespace(A10, Apprec10);
        _manager.AddNamespace(A11, Apprec11);
        _manager.AddNamespace(B, Base64Container);
    }

    /// <summary>
    /// Extracting properties from XML
    /// Implemented support for:
    ///  - MsgHead 1.2 - http://www.kith.no/xmlstds/msghead/2006-05-24
    ///  - Apprec 1.0 - http://www.kith.no/xmlstds/apprec/2004-11-21
    ///  - Apprec 1.1 - http://www.kith.no/xmlstds/apprec/2012-02-15
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    public static IDictionary<string, string> ExtractMessageProperties(XDocument payload)
    {
        var el = payload?.Root;
        if (el == null) return new Dictionary<string, string>();

        var ns = GetNamespace(el);

        var properties = ns switch
        {
            MsgHead => ExtractMessagePropertiesFromMsgHead(el),
            Apprec10 => ExtractMessagePropertiesFromApprec(el, A10),
            Apprec11 => ExtractMessagePropertiesFromApprec(el, A11),
            _ => ExtractMessagePropertiesFromUnspecified(ns)
        };

        return properties.ToDictionary(k=>k.Key, v=>v.Value);
    }

    private static IEnumerable<KeyValuePair<string, string>> ExtractMessagePropertiesFromUnspecified(string ns)
    {
        yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoSchemaNamespace, ns);
    }

    private static IEnumerable<KeyValuePair<string, string>> ExtractMessagePropertiesFromApprec(XElement el, string prefix)
    {
        var apprecEl = GetElement($"/{prefix}:AppRec", el);
        var mainNs = GetNamespace(apprecEl);
        if (!string.IsNullOrWhiteSpace(mainNs))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoSchemaNamespace, mainNs);
        var msgType = GetElementAttribute($"{prefix}:MsgType", "V", apprecEl)?.Value;
        if (!string.IsNullOrWhiteSpace(msgType))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoMsgType, msgType);
        var msgId = GetElement($"{prefix}:Id", apprecEl)?.Value;
        if (!string.IsNullOrWhiteSpace(msgId))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoMsgId, msgId);

        var originalEl = GetElement($"{prefix}:OriginalMsgId", apprecEl);
        var originalMsgId = GetElement($"{prefix}:Id", originalEl)?.Value;
        if (!string.IsNullOrWhiteSpace(originalMsgId))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoOriginalMsgId, originalMsgId);
        var originalMsgType = GetElementAttribute($"{prefix}:MsgType", "V", originalEl)?.Value;
        if (!string.IsNullOrWhiteSpace(originalMsgType))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoOriginalMsgType, originalMsgType);

        var senderEl = GetElement($"{prefix}:Sender", apprecEl);
        var senderLvl1Id = GetApprecLvl1Id(senderEl, prefix);
        if (!string.IsNullOrWhiteSpace(senderLvl1Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.SenderLvl1Id, senderLvl1Id);
        var senderLvl2Id = GetApprecLvl2Id(senderEl, prefix);
        if (!string.IsNullOrWhiteSpace(senderLvl2Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.SenderLvl2Id, senderLvl2Id);

        var receiverEl = GetElement($"{prefix}:Receiver", apprecEl);
        var receiverLvl1Id = GetApprecLvl1Id(receiverEl, prefix);
        if (!string.IsNullOrWhiteSpace(receiverLvl1Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.ReceiverLvl1Id, receiverLvl1Id);
        var receiverLvl2Id = GetApprecLvl2Id(receiverEl, prefix);
        if (!string.IsNullOrWhiteSpace(receiverLvl2Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.ReceiverLvl2Id, receiverLvl2Id);
    }

    private static string GetNamespace(XElement element)
    {
        var ns =  element.GetDefaultNamespace()?.NamespaceName;
        if (string.IsNullOrWhiteSpace(ns)) ns = element?.Name.NamespaceName;
        return ns;
    }

    private static IEnumerable<KeyValuePair<string, string>> ExtractMessagePropertiesFromMsgHead(XElement el)
    {
        var msgInfoEl = GetElement($"//{MH}:MsgInfo", el);
        var msgInfoType = GetElementAttribute($"{MH}:Type", "V", msgInfoEl)?.Value;
        if (!string.IsNullOrWhiteSpace(msgInfoType))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoMsgType, msgInfoType);

        var msgInfoMsgId = GetElement($"{MH}:MsgId", msgInfoEl)?.Value;
        if (!string.IsNullOrWhiteSpace(msgInfoMsgId))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoMsgId, msgInfoMsgId);

        var conversationRefEl = GetElement($"{MH}:ConversationRef", msgInfoEl);
        var msgInfoRefToParent = GetElement($"{MH}:RefToParent", conversationRefEl)?.Value;
        if (!string.IsNullOrWhiteSpace(msgInfoRefToParent))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoRefToParent, msgInfoRefToParent);

        var msgInfoRefToConversation = GetElement($"{MH}:RefToConversation", conversationRefEl)?.Value;
        if (!string.IsNullOrWhiteSpace(msgInfoRefToConversation))
            yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoRefConversation,
                msgInfoRefToConversation);

        var senderEl = GetElement($"{MH}:Sender", msgInfoEl);
        var senderLvl1Id = GetMsgHeadLvl1Id(senderEl);
        if (!string.IsNullOrWhiteSpace(senderLvl1Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.SenderLvl1Id, senderLvl1Id);
        var senderLvl2Id = GetMsgHeadLvl2Id(senderEl);
        if (!string.IsNullOrWhiteSpace(senderLvl2Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.SenderLvl2Id, senderLvl2Id);

        var receiverEl = GetElement($"{MH}:Receiver", msgInfoEl);
        var receiverLvl1Id = GetMsgHeadLvl1Id(receiverEl);
        if (!string.IsNullOrWhiteSpace(receiverLvl1Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.ReceiverLvl1Id, receiverLvl1Id);
        var receiverLvl2Id = GetMsgHeadLvl2Id(receiverEl);
        if (!string.IsNullOrWhiteSpace(receiverLvl2Id))
            yield return new KeyValuePair<string, string>(MetadataKeys.ReceiverLvl2Id, receiverLvl2Id);

        var documents = GetElements($"//{MH}:Document", el);
        var attachmentCounter = 0;
        int attachmentsTotalCount = 0;
        var hasExternalReference = false;
        for (int i = 0; i < documents.Count(); i++)
        {
            var currentDocument = documents.ElementAt(i);
            if (i == 0)
            {
                var innerDocument = GetElement($"{MH}:RefDoc/{MH}:Content", currentDocument)?.FirstNode as XElement;
                var innerNamespace = GetNamespace(innerDocument);
                if (!string.IsNullOrWhiteSpace(innerNamespace))
                    yield return new KeyValuePair<string, string>(MetadataKeys.MessageInfoSchemaNamespace,
                        innerNamespace);
            }
            else
            {
                attachmentCounter++;
                var basee64ContentEl = GetElement($"{MH}:RefDoc/{MH}:Content/{B}:Base64Container", currentDocument);
                if (basee64ContentEl != null)
                {
                    var contentString = basee64ContentEl.Value;
                    var bytes = Encoding.UTF8.GetBytes(contentString);
                    if (bytes.Length > 0) attachmentsTotalCount += bytes.Length;
                }

                var fileReferenceContent = GetElement($"{MH}:RefDoc/{MH}:FileReference", currentDocument);
                if (fileReferenceContent != null) hasExternalReference = true;
            }
        }

        yield return new KeyValuePair<string, string>(MetadataKeys.AttachmentInfoCount, attachmentCounter.ToString());
        yield return new KeyValuePair<string, string>(MetadataKeys.AttachmentInfoTotalSizeInBytes,
            attachmentsTotalCount.ToString());
        yield return new KeyValuePair<string, string>(MetadataKeys.AttachmentInfoHasExternalReference,
            hasExternalReference.ToString());
    }

    private static string GetApprecLvl1Id(XElement senderReceiverElement, string prefix)
    {
        var idents = GetElements($"{prefix}:HCP/{prefix}:Inst/{prefix}:TypeId", senderReceiverElement);
        var identOfInterest = idents.FirstOrDefault(se => se.Attribute("V")?.Value == "HER");
        return GetElement($"{prefix}:Id", identOfInterest?.Parent)?.Value;
    }

    private static string GetApprecLvl2Id(XElement senderReceiverElement, string prefix)
    {
        var identsOrg = GetElements($"{prefix}:HCP/{prefix}:Inst/{prefix}:Dept/{prefix}:TypeId", senderReceiverElement);
        var identsHcp = GetElements($"{prefix}:HCP/{prefix}:Inst/{prefix}:HCPerson/{prefix}:TypeId",
            senderReceiverElement);
        var allIdents = identsOrg.Union(identsHcp);
        var identOfInterest = allIdents.FirstOrDefault(se => se.Attribute("V")?.Value == "HER");
        return GetElement($"{prefix}:Id", identOfInterest?.Parent)?.Value;
    }

    private static string GetMsgHeadLvl1Id(XElement senderReceiverElement)
    {
        var idents = GetElements($"{MH}:Organisation/{MH}:Ident/{MH}:TypeId", senderReceiverElement);
        var identOfInterest = idents.FirstOrDefault(se => se.Attribute("V")?.Value == "HER");
        return GetElement($"{MH}:Id", identOfInterest?.Parent)?.Value;
    }

    private static string GetMsgHeadLvl2Id(XElement senderReceiverElement)
    {
        var identsOrg = GetElements($"{MH}:Organisation/{MH}:Organisation/{MH}:Ident/{MH}:TypeId",
            senderReceiverElement);
        var identsHcp = GetElements($"{MH}:Organisation/{MH}:HealthcareProfessional/{MH}:Ident/{MH}:TypeId",
            senderReceiverElement);
        var allIdents = identsOrg.Union(identsHcp);
        var identOfInterest = allIdents.FirstOrDefault(se => se.Attribute("V")?.Value == "HER");
        return GetElement($"{MH}:Id", identOfInterest?.Parent)?.Value;
    }

    private static XElement GetElement(string expression, XElement el)
    {
        return el?.XPathSelectElement(expression, _manager);
    }

    private static IEnumerable<XElement> GetElements(string expression, XElement el)
    {
        return el?.XPathSelectElements(expression, _manager);
    }

    private static XAttribute GetElementAttribute(string expression, string attribute, XElement el)
    {
        return GetElement(expression, el)?.Attribute(attribute);
    }
}
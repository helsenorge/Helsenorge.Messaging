/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Xml.Linq;
using Helsenorge.Messaging.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Amqp;

[TestClass]
public class MetadataHelperTests
{
    [TestMethod]
    public void WhenPayloadIsMissing_ThenResponseShouldBeEmpty()
    {
        XDocument payload = null;
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.IsEmpty(properties);
    }

    [TestMethod]
    public void WhenDialogmeldingIsValid_ThenAppPropsShouldBeAdded()
    {
        var payload = XDocument.Load($"Files/MetadataTestsDialogmelding11.xml");
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.IsNotNull(properties);
        Assert.IsNotEmpty(properties);

        Assert.AreEqual("DIALOG_HELSEFAGLIG", properties[MetadataKeys.MessageInfoMsgType]);
        Assert.AreEqual("de1a95c0-4d0c-11e7-9598-0800200c9a66", properties[MetadataKeys.MessageInfoMsgId]);
        Assert.AreEqual("d14ea2f3-d796-4d7b-8271-a4480d2b4035", properties[MetadataKeys.MessageInfoRefToParent]);
        Assert.AreEqual("a14ea2f3-d796-4d7b-8271-a4480d2b4037", properties[MetadataKeys.MessageInfoRefConversation]);

        Assert.AreEqual("http://www.kith.no/xmlstds/dialog/2013-01-23", properties[MetadataKeys.MessageInfoSchemaNamespace]);

        Assert.AreEqual("3", properties[MetadataKeys.AttachmentInfoCount]);
        Assert.AreEqual("218", properties[MetadataKeys.AttachmentInfoTotalSizeInBytes]);
        Assert.AreEqual("True", properties[MetadataKeys.AttachmentInfoHasExternalReference]);

        Assert.AreEqual("56704", properties[MetadataKeys.SenderLvl1Id]);
        Assert.AreEqual("258521", properties[MetadataKeys.SenderLvl2Id]);
        Assert.AreEqual("59", properties[MetadataKeys.ReceiverLvl1Id]);
        Assert.AreEqual("90998", properties[MetadataKeys.ReceiverLvl2Id]);
    }

    [TestMethod]
    public void WhenApprec10gIsValid_ThenAllPropsShouldBeAdded()
    {
        var payload = XDocument.Load("Files/MetadataTests_Apprec10.xml");
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.IsNotNull(properties);
        Assert.IsNotEmpty(properties);

        Assert.AreEqual("APPREC", properties[MetadataKeys.MessageInfoMsgType]);
        Assert.AreEqual("A76D8934-D4BA-4A22-8F5D-5DF5E3FC5756", properties[MetadataKeys.MessageInfoMsgId]);
        Assert.AreEqual("de1a95c0-4d0c-11e7-9598-0800200c9a66", properties[MetadataKeys.MessageInfoOriginalMsgId]);
        Assert.AreEqual("DIALOG_HELSEFAGLIG", properties[MetadataKeys.MessageInfoOriginalMsgType]);
        Assert.AreEqual("http://www.kith.no/xmlstds/apprec/2004-11-21", properties[MetadataKeys.MessageInfoSchemaNamespace]);

        Assert.AreEqual("59", properties[MetadataKeys.SenderLvl1Id]);
        Assert.AreEqual("90998", properties[MetadataKeys.SenderLvl2Id]);
        Assert.AreEqual("56704", properties[MetadataKeys.ReceiverLvl1Id]);
        Assert.AreEqual("258521", properties[MetadataKeys.ReceiverLvl2Id]);
    }

    [TestMethod]
    public void WhenApprec11gIsValid_ThenAllPropsShouldBeAdded()
    {
        var payload = XDocument.Load("Files/MetadataTests_Apprec11.xml");
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.IsNotNull(properties);
        Assert.IsNotEmpty(properties);

        Assert.AreEqual("APPREC", properties[MetadataKeys.MessageInfoMsgType]);
        Assert.AreEqual("C829A5F9-5BBF-4376-A37F-ADE4B399916C", properties[MetadataKeys.MessageInfoMsgId]);
        Assert.AreEqual("de1a95c0-4d0c-11e7-9598-0800200c9a66", properties[MetadataKeys.MessageInfoOriginalMsgId]);
        Assert.AreEqual("DIALOG_HELSEFAGLIG", properties[MetadataKeys.MessageInfoOriginalMsgType]);
        Assert.AreEqual("http://www.kith.no/xmlstds/apprec/2012-02-15", properties[MetadataKeys.MessageInfoSchemaNamespace]);

        Assert.AreEqual("56704", properties[MetadataKeys.SenderLvl1Id]);
        Assert.AreEqual("258521", properties[MetadataKeys.SenderLvl2Id]);
        Assert.AreEqual("59", properties[MetadataKeys.ReceiverLvl1Id]);
        Assert.AreEqual("90998", properties[MetadataKeys.ReceiverLvl2Id]);
    }
}



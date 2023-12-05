/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Xml.Linq;
using Helsenorge.Messaging.Amqp;
using Xunit;

namespace Helsenorge.Messaging.Tests.Amqp;

public class MetadataHelperTests
{
    [Fact]
    public void WhenPayloadIsMissing_ThenResponseShouldBeEmpty()
    {
        XDocument payload = null;
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.Empty(properties);
    }

    [Fact]
    public void WhenDialogmeldingIsValid_ThenAppPropsShouldBeAdded()
    {
        var payload = XDocument.Load($"Files/MetadataTestsDialogmelding11.xml");
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.NotNull(properties);
        Assert.NotEmpty(properties);

        Assert.Equal("DIALOG_HELSEFAGLIG", properties[MetadataKeys.MessageInfoMsgType]);
        Assert.Equal("de1a95c0-4d0c-11e7-9598-0800200c9a66", properties[MetadataKeys.MessageInfoMsgId]);
        Assert.Equal("d14ea2f3-d796-4d7b-8271-a4480d2b4035", properties[MetadataKeys.MessageInfoRefToParent]);
        Assert.Equal("a14ea2f3-d796-4d7b-8271-a4480d2b4037", properties[MetadataKeys.MessageInfoRefConversation]);

        Assert.Equal("http://www.kith.no/xmlstds/dialog/2013-01-23", properties[MetadataKeys.MessageInfoSchemaNamespace]);

        Assert.Equal("3", properties[MetadataKeys.AttachmentInfoCount]);
        Assert.Equal("218", properties[MetadataKeys.AttachmentInfoTotalSizeInBytes]);
        Assert.Equal("True", properties[MetadataKeys.AttachmentInfoHasExternalReference]);

        Assert.Equal("56704", properties[MetadataKeys.SenderLvl1Id]);
        Assert.Equal("258521", properties[MetadataKeys.SenderLvl2Id]);
        Assert.Equal("59", properties[MetadataKeys.ReceiverLvl1Id]);
        Assert.Equal("90998", properties[MetadataKeys.ReceiverLvl2Id]);
    }

    [Fact]
    public void WhenApprec10gIsValid_ThenAllPropsShouldBeAdded()
    {
        var payload = XDocument.Load("Files/MetadataTests_Apprec10.xml");
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.NotNull(properties);
        Assert.NotEmpty(properties);

        Assert.Equal("APPREC", properties[MetadataKeys.MessageInfoMsgType]);
        Assert.Equal("A76D8934-D4BA-4A22-8F5D-5DF5E3FC5756", properties[MetadataKeys.MessageInfoMsgId]);
        Assert.Equal("de1a95c0-4d0c-11e7-9598-0800200c9a66", properties[MetadataKeys.MessageInfoOriginalMsgId]);
        Assert.Equal("DIALOG_HELSEFAGLIG", properties[MetadataKeys.MessageInfoOriginalMsgType]);
        Assert.Equal("http://www.kith.no/xmlstds/apprec/2004-11-21", properties[MetadataKeys.MessageInfoSchemaNamespace]);

        Assert.Equal("59", properties[MetadataKeys.SenderLvl1Id]);
        Assert.Equal("90998", properties[MetadataKeys.SenderLvl2Id]);
        Assert.Equal("56704", properties[MetadataKeys.ReceiverLvl1Id]);
        Assert.Equal("258521", properties[MetadataKeys.ReceiverLvl2Id]);
    }

    [Fact]
    public void WhenApprec11gIsValid_ThenAllPropsShouldBeAdded()
    {
        var payload = XDocument.Load("Files/MetadataTests_Apprec11.xml");
        var properties = MetadataHelper.ExtractMessageProperties(payload);
        Assert.NotNull(properties);
        Assert.NotEmpty(properties);

        Assert.Equal("APPREC", properties[MetadataKeys.MessageInfoMsgType]);
        Assert.Equal("C829A5F9-5BBF-4376-A37F-ADE4B399916C", properties[MetadataKeys.MessageInfoMsgId]);
        Assert.Equal("de1a95c0-4d0c-11e7-9598-0800200c9a66", properties[MetadataKeys.MessageInfoOriginalMsgId]);
        Assert.Equal("DIALOG_HELSEFAGLIG", properties[MetadataKeys.MessageInfoOriginalMsgType]);
        Assert.Equal("http://www.kith.no/xmlstds/apprec/2012-02-15", properties[MetadataKeys.MessageInfoSchemaNamespace]);

        Assert.Equal("56704", properties[MetadataKeys.SenderLvl1Id]);
        Assert.Equal("258521", properties[MetadataKeys.SenderLvl2Id]);
        Assert.Equal("59", properties[MetadataKeys.ReceiverLvl1Id]);
        Assert.Equal("90998", properties[MetadataKeys.ReceiverLvl2Id]);
    }
}



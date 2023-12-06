namespace Helsenorge.Messaging.Amqp;

public static class MetadataKeys
{
    public const string MessageInfoMsgType = "X-MessageInfo-Type";
    public const string MessageInfoMsgId = "X-MessageInfo-MsgId";
    public const string MessageInfoRefToParent = "X-MessageInfo-RefToParent";
    public const string MessageInfoRefConversation = "X-MessageInfo-RefToConversation";
    public const string MessageInfoSchemaNamespace = "X-MessageInfo-SchemaNamespace";
    public const string MessageInfoOriginalMsgId = "X-MessageInfo-OriginalMsgId";
    public const string MessageInfoOriginalMsgType = "X-MessageInfo-OriginalMsgType";

    public const string SenderLvl1Id = "X-Sender-Lvl1Id";
    public const string SenderLvl2Id = "X-Sender-Lvl2Id";
    public const string ReceiverLvl1Id = "X-Receiver-Lvl1Id";
    public const string ReceiverLvl2Id = "X-Receiver-Lvl2Id";

    public const string AttachmentInfoCount = "X-AttachmentInfo-Count";
    public const string AttachmentInfoTotalSizeInBytes = "X-AttachmentInfo-TotalSizeInBytes";
    public const string AttachmentInfoHasExternalReference = "X-AttachmentInfo-HasExternalReference";
}
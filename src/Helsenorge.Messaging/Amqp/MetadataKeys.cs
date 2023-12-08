namespace Helsenorge.Messaging.Amqp;

public static class MetadataKeys
{
    /// <summary>
    /// Identifier/Key used when adding MsgType from MsgHead into f.ex a Dictionary
    /// </summary>
    public const string MessageInfoMsgType = "X-MessageInfo-Type";
    /// <summary>
    /// Identifier/Key used when adding MsgId from MsgHead into f.ex a Dictionary
    /// </summary>
    public const string MessageInfoMsgId = "X-MessageInfo-MsgId";
    /// <summary>
    /// Identifier/Key used when adding RefToParent from MsgHead into f.ex a Dictionary
    /// </summary>
    public const string MessageInfoRefToParent = "X-MessageInfo-RefToParent";
    /// <summary>
    /// Identifier/Key used when adding RefToConversation from MsgHead into f.ex a Dictionary
    /// </summary>
    public const string MessageInfoRefConversation = "X-MessageInfo-RefToConversation";
    /// <summary>
    /// Identifier/Key used when adding SchemaNamespace from a payload into f.ex a Dictionary
    /// </summary>
    public const string MessageInfoSchemaNamespace = "X-MessageInfo-SchemaNamespace";
    /// <summary>
    /// Identifier/Key used when adding OriginalMsgId from an Apprec into f.ex a Dictionary
    /// </summary>
    public const string MessageInfoOriginalMsgId = "X-MessageInfo-OriginalMsgId";
    /// <summary>
    /// Identifier/Key used when adding OriginalMsgType from an Apprec into f.ex a Dictionary
    /// </summary>
    public const string MessageInfoOriginalMsgType = "X-MessageInfo-OriginalMsgType";

    /// <summary>
    /// Identifier/Key used when adding level 1 sender identifier/herid from a payload into f.ex a Dictionary
    /// </summary>
    public const string SenderLvl1Id = "X-Sender-Lvl1Id";
    /// <summary>
    /// Identifier/Key used when adding level 2 sender identifier/herid from a payload into f.ex a Dictionary
    /// </summary>
    public const string SenderLvl2Id = "X-Sender-Lvl2Id";
    /// <summary>
    /// Identifier/Key used when adding level 1 receiver identifier/herid from a payload into f.ex a Dictionary
    /// </summary>
    public const string ReceiverLvl1Id = "X-Receiver-Lvl1Id";
    /// <summary>
    /// Identifier/Key used when adding level 2 receiver identifier/herid from a payload into f.ex a Dictionary
    /// </summary>
    public const string ReceiverLvl2Id = "X-Receiver-Lvl2Id";

    /// <summary>
    /// Identifier/Key used when adding the number of attachments in a payload into f.ex a Dictionary
    /// </summary>
    public const string AttachmentInfoCount = "X-AttachmentInfo-Count";
    /// <summary>
    /// Identifier/Key used when adding the total size of all attachments in a payload into f.ex a Dictionary
    /// </summary>
    public const string AttachmentInfoTotalSizeInBytes = "X-AttachmentInfo-TotalSizeInBytes";
    /// <summary>
    /// Identifier/Key used when adding info if FileReference in MsgHead is used into f.ex a Dictionary
    /// </summary>
    public const string AttachmentInfoHasExternalReference = "X-AttachmentInfo-HasExternalReference";
}
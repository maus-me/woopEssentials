using System;
using ProtoBuf;

namespace WoopEssentials.Config;

[ProtoContract]
public class Mail
{
    [ProtoMember(1)]
    public string SenderName { get; set; }

    [ProtoMember(2)]
    public string SenderUid { get; set; }

    [ProtoMember(3)]
    public string Message { get; set; }

    [ProtoMember(4)]
    public DateTime SentTime { get; set; }

    [ProtoMember(5)]
    public bool IsRead { get; set; }

    public Mail()
    {
        SenderName = string.Empty;
        SenderUid = string.Empty;
        Message = string.Empty;
        SentTime = DateTime.Now;
        IsRead = false;
    }

    public Mail(string senderName, string senderUid, string message)
    {
        SenderName = senderName;
        SenderUid = senderUid;
        Message = message;
        SentTime = DateTime.Now;
        IsRead = false;
    }
}
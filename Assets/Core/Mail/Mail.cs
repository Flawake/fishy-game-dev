using System;

public struct Mail
{
    public Guid senderUuid;
    public Guid receiverUuid;
    public int mailUid;
    public DateTime sendTime;
    public string title;
    public string message;
    public bool read;

    public Mail(Guid _senderUuid, Guid _receiverUuid, int _mailUid, DateTime _sendTime, string _title, string _message)
    {
        senderUuid = _senderUuid;
        receiverUuid = _receiverUuid;
        mailUid = _mailUid;
        sendTime = _sendTime;
        title = _title;
        message = _message;
        read = false;
    }

    public Mail(Guid _receiver, string _title, string _message)
    {
        senderUuid = Guid.Empty;
        receiverUuid = _receiver;
        mailUid = 0;
        sendTime = DateTime.MinValue;
        title = _title;
        message = _message;
        read = false;
    }
}
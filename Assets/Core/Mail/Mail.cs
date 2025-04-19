using System;
[Serializable]
public struct Mail
{
    public Guid mailUuid;
    public Guid prevMailUuid;
    public Guid senderUuid;
    public Guid receiverUuid;
    public DateTime sendTime;
    public string title;
    public string message;
    public bool read;

    public Mail(Guid _senderUuid, Guid _prevMailUid, Guid _receiverUuid, Guid _mailUuid, DateTime _sendTime, string _title, string _message)
    {
        senderUuid = _senderUuid;
        prevMailUuid = _prevMailUid;
        receiverUuid = _receiverUuid;
        mailUuid = _mailUuid;
        sendTime = _sendTime;
        title = _title;
        message = _message;
        read = false;
    }

    public Mail(Guid _receiver, string _title, string _message)
    {
        senderUuid = Guid.Empty;
        prevMailUuid = Guid.Empty;
        receiverUuid = _receiver;
        mailUuid = Guid.Empty;
        sendTime = DateTime.MinValue;
        title = _title;
        message = _message;
        read = false;
    }

    public static Mail Empty() {
        return new Mail(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, DateTime.MinValue, "", "");
    }
}
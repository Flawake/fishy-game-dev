using Mirror;
using System;
using UnityEngine;

public class MailSystem : NetworkBehaviour
{
    [SerializeField] Guid receiverUuid;

    void LoadMail(Mail mail)
    {

    }

    public void ResetUuid() {
        receiverUuid = Guid.Empty;
    }

    public void SetupNewMail(Guid _receiverUuid)
    {
        receiverUuid = _receiverUuid;
        transform.GetComponentInChildren<MailGuiManager>().OpenMailGUI();
    }

    public void ClientSendMail(string title, string message) {
        if(receiverUuid == Guid.Empty)
        {
            Debug.LogWarning("Could not send mail, receiver is unknown");
        }
        Mail newMail = new Mail(
          receiverUuid,
          title,
          message
        );
        receiverUuid = Guid.Empty;
        CmdSendMail(newMail);
    }

    [Command]
    void CmdSendMail(Mail mailToSend, NetworkConnectionToClient sender = null)
    {
        //Go from playerName to playerUUID
        //Check if the receiving player is inside this server.
        //if yes, send mail.
        //if no, check if player is in different server and send mail to there.
        //Put message in the database (with the uuid instead of player names).
        //mailToSend.senderUuid = clientConnection.connec
        mailToSend.senderUuid = sender.identity.GetComponent<PlayerData>().GetUuid();
        mailToSend.mailUid = sender.identity.GetComponent<PlayerData>().GetAndIncreaeLastItemUID() + 1;
        mailToSend.sendTime = DateTime.Now;
        DatabaseCommunications.AddMail(mailToSend);
        if(GameNetworkManager.connUUID.TryGetValue(mailToSend.receiverUuid, out NetworkConnectionToClient conn))
        {
            TargetReceiveMail(conn, mailToSend);
        }
        else
        {
            Debug.LogWarning("TODO: Send mail to intermediate server that forwards the mail to the server that contains the receiving player");
        }
    }

    [TargetRpc]
    void TargetReceiveMail(NetworkConnectionToClient target, Mail mail) {
        NetworkClient.connection.identity.GetComponent<PlayerData>().ClientAddMail(mail);
        Debug.Log("Received mail: ");
        Debug.Log("title: " + mail.title);
        Debug.Log("messsage: " + mail.message);
    }
}

using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MailWrapper
{
    public Mail[] mails;
}
public class MailSystem : NetworkBehaviour
{
    [SerializeField] Guid receiverUuid;

    [SerializeField]
    List<Mail> playerMails = new List<Mail>();

    [Command]
    void CmdGetMails()
    {
        RpcSyncMails(playerMails);
    }

    [TargetRpc]
    void RpcSyncMails(List<Mail> _playerMails)
    {
        playerMails.Clear();
        foreach (Mail mail in _playerMails)
        {
            playerMails.Add(mail);
        }
    }

    public override void OnStartLocalPlayer()
    {
        CmdGetMails();
        base.OnStartLocalPlayer();
    }

    public bool ParseMails(string jsonMails)
    {
        try
        {
            MailWrapper mails = JsonUtility.FromJson<MailWrapper>(jsonMails);
            foreach (Mail mail in mails.mails)
            {
                playerMails.Add(mail);
            }
            return true;
        } catch (Exception e)
        {
            Debug.LogWarning(e);
            return false;
        }
    }

    void LoadMails(List<Mail> mails)
    {
        playerMails = mails;
    }

    void AddMail(Mail mail) {

    }

    [Server]
    public void ServerAddMail(Mail mail)
    {
        playerMails.Add(mail);
    }

    [Client]
    public void ClientAddMail(Mail mail)
    {
        playerMails.Add(mail);
    }

    public List<Mail> GetPlayerMails() {
        return playerMails;
    }

    public void ResetUuid() {
        receiverUuid = Guid.Empty;
    }

    public Guid GenerateMailGuid()
    {
        return Guid.NewGuid();
    }

    public void SetupNewMail(Guid _receiverUuid)
    {
        receiverUuid = _receiverUuid;
        transform.GetComponentInChildren<MailGuiManager>().OpenSendMailGUI();
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
        mailToSend.mailUuid = GenerateMailGuid();
        mailToSend.senderUuid = sender.identity.GetComponent<PlayerData>().GetUuid();
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
        // We need to call the function on the actual player object.  
        // The targetRPC ensures the function runs on the correct connection,  
        // but it executes on the object where the function was originally called—  
        // which is the other player's client.

        NetworkClient.connection.identity.GetComponentInChildren<MailSystem>().ClientAddMail(mail);
    }
}

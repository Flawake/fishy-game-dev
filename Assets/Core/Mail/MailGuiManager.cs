using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class MailGuiManager : MonoBehaviour
{
    [SerializeField] GameObject sendMailBackground;
    [SerializeField] TMP_InputField titleField;
    [SerializeField] TMP_InputField messageField;

    MailSystem mailSystem;

    public void Close() {
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        mailSystem.ResetUuid();
        titleField.text = "";
        messageField.text = "";
        sendMailBackground.SetActive(false);
        CloseShowMail();
        CloseMailInbox();
    }

    public void CloseSendMail() {
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        mailSystem.ResetUuid();
        titleField.text = "";
        messageField.text = "";
        sendMailBackground.SetActive(false);
    }

    public void OpenSendMailGUI() {
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        titleField.text = "";
        messageField.text = "";
        sendMailBackground.SetActive(true);
    }

    public void ClickSendButton() {
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        mailSystem.ClientSendMail(titleField.text, messageField.text);
        CloseSendMail();
    }

#region viewMail
    [SerializeField] TMP_Text titelText;
    [SerializeField] TMP_Text messageText;
    [SerializeField] GameObject showMailBackground;

    Mail currentLoadedMail;

    public void CloseShowMail() {
        currentLoadedMail = Mail.Empty();
        showMailBackground.SetActive(false);
    }

    public void OpenShowMailGUI(Mail mail) {
        showMailBackground.SetActive(true);
        ShowMail(mail);
    }

    void ShowMail(Mail mail) {
        currentLoadedMail = mail;
        titelText.text = mail.title;
        messageText.text = mail.message;
    }

    public void ClickReplyButton() {
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        mailSystem.SetupNewMail(currentLoadedMail.senderUuid);
        CloseShowMail();
    }
#endregion

#region inbox
    [SerializeField] GameObject mailInboxHolder;
    [SerializeField] GameObject mailPreviewPrefab;
    [SerializeField] Transform mailPreviewHolderTransform;

    public void CloseMailInbox() {
        mailInboxHolder.SetActive(false);
    }

    public void ToggleMailBox() {
        if (mailInboxHolder.activeSelf || 
            sendMailBackground.activeSelf || 
            showMailBackground.activeSelf
        ) {
            Close();
            return;
        }
        
        mailInboxHolder.SetActive(true);
        List<Mail> playerMails = GetComponentInParent<MailSystem>().GetPlayerMails();
        LoadMailPreviews(playerMails);
    }
    public void LoadMailPreviews(List<Mail> mailCollection) {
        //Clear every mail before loading new ones
        foreach(Transform mail in mailPreviewHolderTransform) {
            Destroy(mail.gameObject);
        }
        foreach(Mail mail in mailCollection) {
            GameObject mailPreview = Instantiate(mailPreviewPrefab, mailPreviewHolderTransform);
            mailPreview.GetComponent<MailPreview>().InitMailPreview(mail);
        }
    }
#endregion
}

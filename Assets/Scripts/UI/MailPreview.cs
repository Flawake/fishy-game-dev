using TMPro;
using UnityEngine;

public class MailPreview : MonoBehaviour
{
    [SerializeField] TMP_Text senderName;
    [SerializeField] TMP_Text mailTitle;
    [SerializeField] TMP_Text mailMessage;

    Mail mail;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void InitMailPreview(Mail _mail)
    {
        mail = _mail;
        mailTitle.text = mail.title;
        senderName.text = mail.senderUuid.ToString();
        mailMessage.text = mail.message;

        if(mail.read) {
            mailTitle.fontStyle = FontStyles.Bold;
        }
        else {
            mailTitle.fontStyle = FontStyles.Normal;
        }
    }

    public void ClickMailPreview() {
        GetComponentInParent<MailGuiManager>().OpenShowMailGUI(mail);
    }
}

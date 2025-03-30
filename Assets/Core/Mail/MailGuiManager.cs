using TMPro;
using UnityEngine;

public class MailGuiManager : MonoBehaviour
{
    [SerializeField] GameObject background;
    [SerializeField] TMP_InputField titleField;
    [SerializeField] TMP_InputField messageField;

    MailSystem mailSystem;

    public void Close() {
        background.SetActive(false);
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        mailSystem.ResetUuid();
        titleField.text = "";
        messageField.text = "";
    }

    public void OpenMailGUI() {
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        titleField.text = "";
        messageField.text = "";
        background.SetActive(true);
    }

    public void ClickSendButton() {
        if(mailSystem == null) {
            mailSystem = transform.GetComponentInParent<MailSystem>();
        }
        mailSystem.ClientSendMail(titleField.text, messageField.text);
        Close();
    }
}

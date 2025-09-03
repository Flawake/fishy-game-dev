using UnityEngine;
using UnityEngine.UI;

public class SendChatButtonManager : MonoBehaviour
{
    [SerializeField]
    Button sendChatButton;

    public void TextInputChanged(string text)
    {
        if (text.Length == 0)
        {
            sendChatButton.enabled = false;
        }
        else
        {
            sendChatButton.enabled = true;
        }
    }
}

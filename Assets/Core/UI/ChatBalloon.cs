using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class ChatBalloon : NetworkBehaviour
{

    [SerializeField]
    TextMeshPro textBalloonText;
    [SerializeField]
    SpriteRenderer backgroundSpriteRenderer;
    [SerializeField]
    SpriteRenderer arrowSpriteRenderer;
    [SerializeField]
    GameObject textBalloonGraphics;
    [SerializeField]
    PlayerData playerData;

    ChatHistory history;

    float totalTimeChatVisible = 0;
    //Min 6 seconds. And 0.3s/character
    float fadeChatAfterTime = 10;
    float minTimeChatVisible = 6f;
    float timePerCharacter = 0.2f;

    bool shouldFade = false;

    const float fadeTimeSec = 2.5f;
    const float textVisibleAlpha = 255/255;
    const float backgroundVisibleAlpha = 255/180;

    private static readonly Regex _escapeRegex = new Regex("(<.*?>)", RegexOptions.Compiled);

    private void Update()
    {
        if (NetworkServer.active || textBalloonGraphics.activeInHierarchy == false)
        {
            return;
        }

        totalTimeChatVisible += Time.deltaTime;
        FadeText();
    }

    private void Start()
    {
        if (NetworkServer.active)
        {
            return;
        }

        history = NetworkClient.localPlayer.GetComponentInChildren<ChatHistory>();
    }

    void FadeText()
    {
        if(totalTimeChatVisible > fadeChatAfterTime && shouldFade == false)
        {
            shouldFade = true;
            StartCoroutine(FadeTextBalloon());
        }
        if(backgroundSpriteRenderer.color.a < 0.01f)
        {
            shouldFade = false;
            textBalloonGraphics.SetActive(false);
        }
    }

    IEnumerator FadeTextBalloon()
    {
        while(shouldFade)
        {
            //fade chat out in 2.5seconds, based on if the alpha was 1, altough it was not. fix this
            float newAlpha = backgroundSpriteRenderer.color.a - ((1f / fadeTimeSec) * Time.deltaTime * backgroundVisibleAlpha);
            float newAlphaT = textBalloonText.color.a - ((1f / fadeTimeSec) * Time.deltaTime * backgroundVisibleAlpha);
            backgroundSpriteRenderer.color = new Color(0, 0, 0, newAlpha);
            arrowSpriteRenderer.color = new Color(0, 0, 0, newAlpha);
            textBalloonText.color = new Color(255, 255, 255, newAlphaT);
            yield return null;
        }
    }

    void SetText(string text, string userName, string chatColor)
    {
        if(NetworkServer.active)
        {
            return;
        }

        textBalloonText.SetText(text);
        textBalloonText.ForceMeshUpdate();
        Bounds textSizeBounds = textBalloonText.textBounds;
        Vector2 textSize = textSizeBounds.size;
        //Only used as last resort, this should never be executed
        if (text.Length == 0 || textSize.x > 10 || textSize.y > 10)
        {
            textSize = new Vector2(1f, 1f);
        }

        Vector2 padding = new Vector2(0.2f, 0.1f);
        backgroundSpriteRenderer.size = textSize + padding;
        backgroundSpriteRenderer.color = new Color(0, 0, 0, backgroundVisibleAlpha);
        arrowSpriteRenderer.color = new Color(0, 0, 0, backgroundVisibleAlpha);
        textBalloonText.color = new Color(255, 255, 255, textVisibleAlpha);
        totalTimeChatVisible = 0;
        shouldFade = false;
        fadeChatAfterTime = math.max(minTimeChatVisible, timePerCharacter * text.Length);

        history.AddChatHistory(text, userName, chatColor);
    }

    public void SendChatMessage(string message)
    {
        string userName = playerData.GetUsername();
        string chatColor = $"#{playerData.GetChatColorAsRGBAString()}";
        message = FilterText(message);
        textBalloonGraphics.SetActive(true);
        SetText(message, userName, chatColor);
        CmdSendMessage(message);
    }

    string FilterText(string message)
    {
        //We should not remove \n and \r here since those will not be used when printing in the text balloon
        //We should only look at those when printing the chat history
        message = TruncateString(message, PlayerInfoUIManager.GetMaxChatMessageLength());
        //Easter egg, set text to ":(" if the user tries to cheat
        if (message.Length == 0)
        {
            return ":(";
        }

        //Check if message only consists of spaces
        bool messageValid = false;
        foreach (char s in message)
        {
            if (s != ' ')
            {
                messageValid = true;
            }
            if(s < 32 || s > 126) {
                messageValid = false;
                break;
            }
        }
        if (!messageValid)
        {
            return ":(";
        }
        return message;
    }

    [Command]
    void CmdSendMessage(string message)
    {
        string userName = playerData.GetUsername();
        string chatColor = $"#{playerData.GetChatColorAsRGBAString()}";
        message = FilterText(message);
        RpcReceiveChatMessage(message, userName, chatColor);
    }

    [ClientRpc(includeOwner = false)]
    void RpcReceiveChatMessage(string message, string userName, string chatColor)
    {
        Debug.Log("Received text");
        textBalloonGraphics.SetActive(true);
        SetText(message, userName, chatColor);
    }

    string TruncateString(string message, int maxLength)
    {
        return message.Length > maxLength ? message.Substring(0, maxLength) : message;
    }

    public static string SanitizeTMPString(string str)
    {
        str = str.Replace("\n", "").Replace("\r", "");
        return _escapeRegex.Replace(str, "<noparse>$1</noparse>").Replace("</noparse></noparse>", "</noparse></<b></b>noparse>");
    }
}

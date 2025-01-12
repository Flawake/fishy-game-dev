using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;

public class PlayerInfoUIManager : MonoBehaviour
{
    FishingManager fishingManager;
    PlayerData playerData;

    [SerializeField]
    Canvas UICanvas;
    [SerializeField]
    Image itemHolderRodSprite;
    [SerializeField]
    Image itemHolderBaitSprite;
    [SerializeField]
    TMP_Text coins;
    [SerializeField]
    TMP_Text bucks;
    [SerializeField]
    TMP_Text levelField;
    [SerializeField]
    Slider levelProgress;

    [SerializeField]
    TMP_InputField chatInput;

    Camera cam;
    ChatBalloon chatInterface;

    static readonly int maxChatMessageLength = 200;

    private void Start()
    {
#if(UNITY_EDITOR)
        //Done for debugging if I forget about this code and change it in the inspector by accident.
        if(chatInput.characterLimit != maxChatMessageLength)
        {
            Debug.LogError("chatInput.characterLimit != maxChatMessageLength \n You should not change the caracter limit in the inspector");
        }
#endif
        chatInput.characterLimit = maxChatMessageLength;
        if (!NetworkClient.localPlayer)
        {
            return;
        }
        fishingManager = GetComponentInParent<FishingManager>();
        fishingManager.selectedRodChanged += UpdateSelectedRodImage;
        fishingManager.selectedBaitChanged += UpdateSelectedBaitImage;
        playerData = GetComponentInParent<PlayerData>();
        playerData.CoinsAmountChanged += FishCoinsChanged;
        playerData.BucksAmountChanged += FishBucksChanged;
        playerData.XPAmountChanged += XpAmountChanged;
        XpAmountChanged();
        UpdateSelectedRodImage();
        UpdateSelectedBaitImage();
        HideCanvas();
    }

    public static int GetMaxChatMessageLength()
    {
        return maxChatMessageLength;
    }

    void EnsureCameraFound()
    {
        if(cam == null)
        {
            cam = transform.parent.GetComponentInChildren<Camera>();
        }
    }

    void EnsureChatBalloonFound()
    {
        if (chatInterface == null)
        {
            chatInterface = transform.parent.GetComponentInChildren<ChatBalloon>();
        }
    }

    void FishCoinsChanged()
    {
        coins.text = playerData.GetFishCoins().ToString();
    }

    void FishBucksChanged()
    {
        bucks.text = playerData.GetFishBucks().ToString();
    }

    void XpAmountChanged()
    {
        int xp = playerData.GetXp();
        Debug.Log("XP: " + xp);
        (int level, int xpBeginLevel, int curXP, int xpEndLevel) = LevelMath.XpToLevel(xp);
        float progress = (float)curXP / (xpEndLevel - xpBeginLevel);
        levelField.text = level.ToString();
        levelProgress.value = progress;
    }

    //We're hiding the canvan by changing the rendering mode and then disabling the camera
    public void HideCanvas()
    {
        UICanvas.renderMode = RenderMode.ScreenSpaceCamera;
        EnsureCameraFound();
        UICanvas.worldCamera = cam;
        cam.enabled = false;
    }

    //We're hiding the canvan by changing the rendering mode and then enabling the camera
    public void ShowCanvas()
    {
        UICanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        EnsureCameraFound();
        cam.enabled = true;
    } 

    public void UpdateSelectedRodImage()
    {
        if (fishingManager == null)
        {
            fishingManager = GetComponentInParent<FishingManager>();
            if (fishingManager == null)
            {
                return;
            }
        }
        itemHolderRodSprite.sprite = fishingManager.selectedRod.sprite;
    }

    public void UpdateSelectedBaitImage()
    {
        if (fishingManager == null)
        {
            fishingManager = GetComponentInParent<FishingManager>();
            if (fishingManager == null)
            {
                return;
            }
        }
        itemHolderBaitSprite.sprite = fishingManager.selectedBait.sprite;
    }

    public void OpenWorldMap()
    {
        HideCanvas();
        FindObjectOfType<AudioListener>().enabled = false;

        GameNetworkManager.SetEventSystemActive(SceneManager.GetActiveScene().name, false);

        SceneManager.LoadScene("WorldMap", LoadSceneMode.Additive);
    }

    public void SendChat()
    {
        EnsureChatBalloonFound();
        if(chatInput.text.Length > 0)
        {
            chatInterface.SendChatMessage(chatInput.text);
            chatInput.text = "";
        }
    }

    public void ReceiveChat()
    {
        EnsureChatBalloonFound();
        chatInterface.SendChatMessage(chatInput.text);
        chatInput.text = "";
    }
}

using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UserData;

public class FishInfoUIManager : MonoBehaviour
{
    [SerializeField]
    GameObject caughtFishInfo;
    [SerializeField]
    GameObject uncaughtFishInfo;

    [SerializeField]
    CaughtFishInfo caughtFishInfoManager;
    [SerializeField]
    UncaughtFishInfo uncaughtFishInfoManager;

    [SerializeField]
    TMP_Text fishName;
    [SerializeField]
    Image fishimage;
    [SerializeField]
    TMP_Text fishBait;
    [SerializeField]
    TMP_Text fishAreas;
    [SerializeField]
    TMP_Text maxCaughtLength;
    [SerializeField]
    TMP_Text amountCaught;

    FishConfiguration curFish;

    public void CloseFishInfo()
    {
        caughtFishInfo.SetActive(false);
        uncaughtFishInfo.SetActive(false);
    }

    public void OpenFishInfo(int fishID)
    {
        curFish = ItemsInGame.getFishByID(fishID);
        if (curFish == null)
        {
            Debug.LogWarning($"Tries to show information about a fish with an ID of {fishID}, but this fish does not seem to be in the game");
            return;
        }
        StatFish statFish = NetworkClient.localPlayer.GetComponentInChildren<PlayerFishdexFishes>().GetStatFish(fishID);
        if (statFish == null)
        {
            uncaughtFishInfo.SetActive(true);
            uncaughtFishInfoManager.ShowFishInfo(fishID);
            fishimage.color = Color.black;
            amountCaught.text = "0";
        }
        else
        {
            caughtFishInfo.SetActive(true);
            caughtFishInfoManager.ShowFishInfo(fishID);
            fishimage.color = Color.white;
            maxCaughtLength.text = statFish.maxCaughtLength.ToString();
            amountCaught.text = statFish.amount.ToString();
        }
        fishName.text = curFish.name;
        fishimage.sprite = curFish.fishImage;
        caughtFishInfo.SetActive(true);
    }

    public int CurrentFishinfoFishID()
    {
        if (caughtFishInfo.activeInHierarchy == false)
        {
            return -1;
        }
        return curFish.id;
    }
}

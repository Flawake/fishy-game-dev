using Mirror;
using ItemSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishInfoUIManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text fishName;
    [SerializeField]
    TMP_Text fishDescription;
    [SerializeField]
    Image fishimage;
    [SerializeField]
    GameObject possibleBaitsContainer;
    [SerializeField]
    GameObject caughtWithBaitContainer;
    [SerializeField]
    GameObject possibleAreasContainer;
    [SerializeField]
    GameObject caughtInAreasContainer;
    [SerializeField]
    TMP_Text maxCaughtLength;
    [SerializeField]
    TMP_Text amountCaught;
    [SerializeField]
    GameObject[] stars;
    [SerializeField]
    GameObject baitImagePrefab;
    [SerializeField]
    GameObject areaImagePrefab;

    ItemDefinition curFish;

    private void showStars(int starCount)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            if (i < starCount)
            {
                stars[i].SetActive(true);
            }
            else
            {
                stars[i].SetActive(false);
            }
        }
    }

    private void ClearContainers()
    {
        foreach (Transform child in caughtWithBaitContainer.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in caughtInAreasContainer.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in possibleBaitsContainer.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in possibleAreasContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void FillContainers(StatFish statFish)
    {
        for (int i = 0; i < statFish.baitsCaught.Length - 1; i++)
        {
            ItemDefinition bait = ItemRegistry.Get(statFish.baitsCaught[i]);
            GameObject baitObject = Instantiate(baitImagePrefab, caughtWithBaitContainer.transform);
            baitObject.GetComponent<Image>().sprite = bait.Icon;
        }
    }

    public void LoadFishInfo(int fishID)
    {
        curFish = ItemRegistry.Get(fishID);
        FishBehaviour curFishBehaviour = curFish.GetBehaviour<FishBehaviour>();
        if (curFish == null || curFishBehaviour == null)
        {
            Debug.LogWarning($"Could not show information about a fish that should have had ID: {fishID}");
            return;
        }
        ClearContainers();

        StatFish statFish = NetworkClient.localPlayer.GetComponentInChildren<PlayerFishdexFishes>().GetStatFish(fishID);
        if (statFish == null)
        {
            fishimage.color = Color.black;
            maxCaughtLength.text = "-";
            amountCaught.text = "0 x";
        }
        else
        {
            fishimage.color = Color.white;
            maxCaughtLength.text = statFish.maxCaughtLength.ToString() + "cm";
            amountCaught.text = statFish.amount.ToString() + " x";

            FillContainers(statFish);
        }
        fishName.text = curFish.DisplayName;
        fishDescription.text = curFish.Description;
        fishimage.sprite = curFish.Icon;

        showStars(FishEnumConfig.RarityToInt(curFishBehaviour.Rarity));
    }
}

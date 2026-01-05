using Mirror;
using ItemSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

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

    private void ShowStars(int starCount)
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

    private void FillContainers(int fishID)
    {
        StatFish statFish = NetworkClient.localPlayer.GetComponentInChildren<PlayerFishdexFishes>().GetStatFish(fishID);
        if (statFish != null)
        {
            // baits caught builder
            for (int i = 0; i < statFish.baitsCaught.Length; i++)
            {
                ItemDefinition bait = ItemRegistry.Get(statFish.baitsCaught[i]);
                if (bait == null)
                {
                    continue;
                }
                GameObject baitObject = Instantiate(baitImagePrefab, caughtWithBaitContainer.transform);
                baitObject.GetComponent<Image>().sprite = bait.Icon;
            }

            // areas caught builder
            for (int i = 0; i < statFish.areasCaught.Length; i++)
            {
                Area areaCaught = (Area)statFish.areasCaught[i];
                for (int j = 0 ; j < GlobalConnector.areaImageConnector.Length; j++)
                {
                    if (GlobalConnector.areaImageConnector[j].Area == areaCaught)
                    {
                        GameObject areaObject = Instantiate(areaImagePrefab, caughtInAreasContainer.transform);
                        areaObject.GetComponentInChildren<Image>().sprite = GlobalConnector.areaImageConnector[j].AreaImage;
                        break;
                    }
                }
            }
        }

        ItemDefinition fish = ItemRegistry.Get(fishID);
        FishBehaviour fishBehaviour = fish.GetBehaviour<FishBehaviour>();
        if (fishBehaviour != null)
        {
            // effective bait builder
            ItemDefinition[] items = ItemRegistry.GetFullItemsList();
            foreach (ItemDefinition item in items)
            {
                BaitBehaviour baitBehaviour = item.GetBehaviour<BaitBehaviour>();
                if (baitBehaviour != null && fishBehaviour.IsBaitEffective(baitBehaviour.BaitType))
                {
                    GameObject baitObject = Instantiate(baitImagePrefab, possibleBaitsContainer.transform);
                    baitObject.GetComponent<Image>().sprite = item.Icon;
                }
            }

            // Areas swimming builder
            for (int i = 0; i < GlobalConnector.areaImageConnector.Length; i++)
            {
                if (fishBehaviour.ActiveInArea(GlobalConnector.areaImageConnector[i].Area))
                {
                    GameObject areaObject = Instantiate(areaImagePrefab, possibleAreasContainer.transform);
                    areaObject.GetComponentInChildren<Image>().sprite = GlobalConnector.areaImageConnector[i].AreaImage;
                    break;
                }
            }
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
        }
        FillContainers(fishID);
        fishName.text = curFish.DisplayName;
        fishDescription.text = curFish.Description;
        fishimage.sprite = curFish.Icon;

        ShowStars(FishEnumConfig.RarityToInt(curFishBehaviour.Rarity));
    }
}

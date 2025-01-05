using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishdexFishUIBuilder : MonoBehaviour
{
    int fishID;
    [SerializeField]
    TMP_Text nameField;
    [SerializeField]
    Image fishImage;
    [SerializeField]
    TMP_Text rarity;

    public void BuildFishdexFish(FishConfiguration fish)
    {
        fishID = fish.id;
        nameField.text = fish.name;
        fishImage.sprite = fish.fishImage;
        rarity.text = FishEnumConfig.rarityToString(fish.rarity);

        if (NetworkClient.localPlayer.GetComponentInChildren<PlayerFishdexFishes>().ContainsFish(fish.id))
        {
            fishImage.color = Color.white;
        }
        else
        {
            fishImage.color = Color.black;
        }
    }

    public void ShowFishInfo()
    {
        Debug.Log("ShowFishInfo");
        FishInfoUIManager fishInfoShowManager = NetworkClient.localPlayer.GetComponentInChildren<FishInfoUIManager>();
        fishInfoShowManager.OpenFishInfo(fishID);
    }
}

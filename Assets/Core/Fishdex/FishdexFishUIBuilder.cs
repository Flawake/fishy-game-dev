using Mirror;
using ItemSystem;
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

    FishInfoUIManager fishInfoUI;

    private void Start()
    {
        fishInfoUI = NetworkClient.localPlayer.GetComponentInChildren<FishInfoUIManager>();
    }

    public void BuildFishdexFish(ItemDefinition fish)
    {
        FishBehaviour fishBehaviour = fish.GetBehaviour<FishBehaviour>();
        if (fishBehaviour == null)
        {
            return;
        }
        fishID = fish.Id;
        nameField.text = fish.name;
        fishImage.sprite = fish.Icon;
        rarity.text = FishEnumConfig.RarityToString(fishBehaviour.Rarity);

        if (NetworkClient.localPlayer.GetComponentInChildren<PlayerFishdexFishes>().ContainsFish(fish.Id))
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
        if (fishInfoUI.CurrentFishinfoFishID() == fishID)
        {
            fishInfoUI.CloseFishInfo();
            return;
        }
        fishInfoUI.OpenFishInfo(fishID);
    }
}

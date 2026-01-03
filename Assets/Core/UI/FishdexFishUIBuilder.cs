using Mirror;
using ItemSystem;
using UnityEngine;
using UnityEngine.UI;

public class FishdexFishUIBuilder : MonoBehaviour
{
    ItemDefinition thisFish;

    [SerializeField]
    Image fishImage;
    [SerializeField]
    Image sleeve;

    FishInfoUIManager fishInfoUI;

    private void Start()
    {
        fishInfoUI = NetworkClient.localPlayer.GetComponentInChildren<FishInfoUIManager>();
    }

    public void BuildFishdexFish(ItemDefinition fish)
    {
        thisFish = fish;
        FishBehaviour fishBehaviour = fish.GetBehaviour<FishBehaviour>();

        if (fishBehaviour == null)
        {
            return;
        }
        fishImage.sprite = fish.Icon;
        
        sleeve.color = FishEnumConfig.RarityToColor(fishBehaviour.Rarity);

        fishImage.color = NetworkClient.localPlayer
                            .GetComponentInChildren<PlayerFishdexFishes>()
                            .ContainsFish(fish.Id)
                        ? Color.white
                        : Color.black;

    }

    // Called from button ingame
    public void ShowFishInfo()
    {
        GetComponentInParent<FishdexUIManager>().ShowFishInfo(thisFish);
    }
}

using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishdexFishUIBuilder : MonoBehaviour
{
    [SerializeField]
    TMP_Text nameField;
    [SerializeField]
    Image fishImage;
    [SerializeField]
    TMP_Text rarity;

    PlayerFishdexFishes playerFishes;
    public void BuildFishdexFish(FishConfiguration fish)
    {
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
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using NewItemSystem;

public class CaughtDialogData : MonoBehaviour
{
    [SerializeField]
    Image fishSprite;

    [SerializeField]
    TMP_Text nameField;
    [SerializeField]
    TMP_Text rarityField;
    [SerializeField]
    TMP_Text lengthField;
    [SerializeField]
    TMP_Text xpField;

    public void SetData(CurrentFish fishdata)
    {
        ItemDefinition fish = ItemRegistry.Get(fishdata.id);
        nameField.text = fish.name;
        fishSprite.sprite = fish.Icon;
        rarityField.text = FishEnumConfig.RarityToString(fishdata.rarity);
        rarityField.color = FishEnumConfig.RarityToColor(fishdata.rarity);
        lengthField.text = fishdata.length.ToString();
        xpField.text = fishdata.xp.ToString() + " XP";

    }

    //Called from in game button
    public void CloseCaughtDialog()
    {
        this.gameObject.SetActive(false);
    }
}

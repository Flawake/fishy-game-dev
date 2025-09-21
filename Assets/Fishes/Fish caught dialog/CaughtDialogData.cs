using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ItemSystem;

public class CaughtDialogData : MonoBehaviour
{
    [SerializeField]
    Image fishSprite;

    [SerializeField]
    TMP_Text nameField;
    [SerializeField]
    TMP_Text lengthField;
    [SerializeField]
    TMP_Text xpField;

    [SerializeField]
    GameObject[] stars;

    public void SetData(CurrentFish fishdata)
    {
        ItemDefinition fish = ItemRegistry.Get(fishdata.id);
        nameField.text = fish.DisplayName;
        fishSprite.sprite = fish.Icon;
        lengthField.text = "Length: " + fishdata.length.ToString() + " cm";
        xpField.text = fishdata.xp.ToString() + " XP";
        SetStars(FishEnumConfig.RarityToInt(fishdata.rarity));

    }

    void ResetStars() {
        foreach(GameObject star in stars) {
            star.SetActive(false);
        }
    }

    void SetStars(int rating) {
        ResetStars();
        for(int starIndex = 0; starIndex < rating; starIndex++) {
            stars[starIndex].SetActive(true);
        }
    }

    //Called from in game button
    public void CloseCaughtDialog()
    {
        this.gameObject.SetActive(false);
    }
}

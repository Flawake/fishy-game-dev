using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ItemSystem;

public class CaughtDialogData : MonoBehaviour
{
    ItemDefinition curFish;

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

    [SerializeField]
    FishdexUIManager fishdex;

    public void SetData(CurrentFish fishdata)
    {
        curFish = ItemRegistry.Get(fishdata.id);
        nameField.text = curFish.DisplayName;
        fishSprite.sprite = curFish.Icon;
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
        gameObject.SetActive(false);
    }

    // Called from button in game
    public void viewInFishdexButton()
    {
        fishdex.OpenFishdex();
        fishdex.ShowFishInfo(curFish);
        CloseCaughtDialog();
    }
}

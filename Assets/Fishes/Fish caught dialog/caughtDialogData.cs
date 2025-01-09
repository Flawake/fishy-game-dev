using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class caughtDialogData : MonoBehaviour
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
    TMP_Text weightField;
    [SerializeField]
    TMP_Text xpField;

    public void setData(CurrentFish fishdata)
    {
        InGameItems itemsInGame = GameObject.Find("NetworkManager").GetComponent<InGameItems>();

        fishSprite.sprite = Array.Find(ItemsInGame.fishesInGame, element => element.id == fishdata.id).fishImage;

        nameField.text = "";
        rarityField.text = FishEnumConfig.RarityToString(fishdata.rarity);
        lengthField.text = fishdata.length.ToString();
        weightField.text = ((float)fishdata.weight / 100).ToString();
        xpField.text = fishdata.xp.ToString();

    }

    //Called from in game button
    public void CloseCaughtDialog()
    {
        this.gameObject.SetActive(false);
    }
}

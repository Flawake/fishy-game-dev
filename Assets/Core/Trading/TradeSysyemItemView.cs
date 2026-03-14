using TMPro;
using TradeSystem;
using UnityEngine;
using UnityEngine.UI;

public class TradeSystemItemView : MonoBehaviour
{
    TradableItem item;
    [SerializeField]
    Image itemPreviewImage;
    [SerializeField]
    TMP_Text amountField;
    [SerializeField]
    GameObject removeButton;

    public TradeSystemItemView SetTradableItem(TradableItem _item)
    {
        item = _item;
        itemPreviewImage.sprite = item.GetSprite();
        amountField.text = item.Amount.ToString();
        return this;
    }

    public void EnableRemoveButton()
    {
        removeButton.SetActive(true);
    }

    // Called from button in game
    public void AddItem()
    {
        GetComponentInParent<Trading>().SelectItemToTrade(item);
    }

    // Called from button in game
    public void RemoveItem()
    {
        // Add item with amount of 0 to remove it from the trade
        item.Amount = 0;
        GetComponentInParent<Trading>().AddItemToTrade(item, true);
    }
}

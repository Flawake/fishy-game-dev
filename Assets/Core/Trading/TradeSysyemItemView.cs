using TradeSystem;
using UnityEngine;
using UnityEngine.UI;

public class TradeSystemItemView : MonoBehaviour
{
    TradableItem item;
    [SerializeField]
    Image itemPreviewImage;
    public void SetTradableItem(TradableItem _item)
    {
        item = _item;
        itemPreviewImage.sprite = item.GetSprite();
    }

    // Called from button in game
    public void AddItem()
    {
        GetComponentInParent<Trading>().SelectItemToTrade(item);
    }
}

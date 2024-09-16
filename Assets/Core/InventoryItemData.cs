using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class InventoryItemData : MonoBehaviour
{
    [SerializeField]
    Image spriteHolder;
    
    [SerializeField]
    TMP_Text itemAmountField;

    InventoryUIManager inventoryManager;

    ItemObject item;

    private void Start()
    {
        inventoryManager = GetComponentInParent<InventoryUIManager>();
    }

    void EnsureInventoryManager()
    {
        if (inventoryManager == null)
        {
            inventoryManager = GetComponentInParent<InventoryUIManager>();
        }
    }

    //Called from game
    public void InventoryItemClicked()
    {
        EnsureInventoryManager();
        inventoryManager.ShowItemInfo(item);
    }

    public void SetInventoryItemData(ItemObject _item)
    {
        spriteHolder.sprite = _item.sprite;
        item = _item;

        int amount;
        if (item is rodObject rod)
        {
            amount = rod.throwIns;
        }
        else if (item is baitObject bait)
        {
            amount = bait.throwIns;
        }
        else if (item is FishObject fish)
        {
            amount = fish.amount;
        }
        else
        {
            return;
        }
        itemAmountField.text = amount.ToString();
    }
}

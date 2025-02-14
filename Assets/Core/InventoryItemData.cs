using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;
using Unity.VisualScripting;

public class InventoryItemData : MonoBehaviour
{
    [SerializeField]
    Image spriteHolder;
    [SerializeField]
    GameObject itemSelectedMark;

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

    public void SetInventoryItemData(ItemObject _item, bool itemSelected)
    {
        spriteHolder.sprite = _item.sprite;
        item = _item;
        itemSelectedMark.SetActive(itemSelected);
    }
}

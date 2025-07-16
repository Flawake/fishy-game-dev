using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;
using Unity.VisualScripting;
using NewItemSystem;

public class InventoryItemData : MonoBehaviour
{
    [SerializeField]
    Image spriteHolder;
    [SerializeField]
    GameObject itemSelectedMark;

    InventoryUIManager inventoryManager;

    ItemInstance item;

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

    public void SetInventoryItemData(ItemInstance _item, bool itemSelected)
    {
        spriteHolder.sprite = _item.def.Icon;
        item = _item;
        itemSelectedMark.SetActive(itemSelected);
    }
}

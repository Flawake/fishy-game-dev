using Mirror;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    FishingManager fishingManager;
    playerController controller;
    ItemObject item;

    [SerializeField]
    GameObject useItemButton;
    [SerializeField]
    TMP_Text itemNameText;
    [SerializeField]
    TMP_Text itemInfoText;
    [SerializeField]
    TMP_Text itemAmountText;
    [SerializeField]
    Image itemPreviewImage;
    [SerializeField]
    GameObject inventoryUI;
    [SerializeField]
    GameObject inventoryItemPrefab;
    [SerializeField]
    GameObject itemHolder;
    [SerializeField]
    GameObject itemPreviewSelectedItemMark;
    [SerializeField]
    GameObject tabSelectionUnder;
    [SerializeField]
    GameObject tabSelectionUpper;
    [SerializeField]
    GameObject[] selectMenuButtons;
    [SerializeField]
    Sprite selectedButtonSprite;
    [SerializeField]
    Sprite unselectedButtonSprite;

    [Serializable]
    private enum ItemFiler
    {
        all = 0,
        rods = 1,
        baits = 2,
        Fishes = 3
    }

    private void Start()
    {
        fishingManager = GetComponentInParent<FishingManager>();
        controller = GetComponentInParent<playerController>();
    }

    //called by a button in the game
    public void ClickSelectNewItem(int filter)
    {
        ToggleBackPack((ItemFiler)filter);
        ChangeBackPackMenu(filter);
    }

    public void ToggleBackPack()
    {
        ToggleBackPack(ItemFiler.all);
    }

    void ToggleBackPack(ItemFiler filter)
    {
        if (inventoryUI.activeInHierarchy == false)
        {
            //0 is show all
            ChangeBackPackMenu(0);
            inventoryUI.SetActive(true);
            EnsurePlayerController();
            controller.IncreaseObjectsPreventingMovement();
            controller.IncreaseObjectsPreventingFishing();
        }
        else
        {
            inventoryUI.SetActive(false);
            EnsurePlayerController();
            controller.DecreaseObjectsPreventingMovement();
            controller.DecreaseObjectsPreventingFishing();
        }
    }

    public void CloseBackPack()
    {
        EnsurePlayerController();
        controller.DecreaseObjectsPreventingMovement();
        controller.DecreaseObjectsPreventingFishing();
        inventoryUI.SetActive(false);
    }

    public void ChangeBackPackMenu(int index)
    {
        for (int i = 0; i < selectMenuButtons.Length; i++)
        {
            selectMenuButtons[i].GetComponent<Image>().sprite = unselectedButtonSprite;
            selectMenuButtons[i].transform.SetParent(tabSelectionUnder.transform);
            if (index == i)
            {
                selectMenuButtons[i].GetComponent<Image>().sprite = selectedButtonSprite;
                selectMenuButtons[i].transform.SetParent(tabSelectionUpper.transform);
            }
        }
        BuildBackPackItems((ItemFiler)index + 1);
    }

    void BuildBackPackItems(ItemFiler filter)
    {
        //Remove all previous items
        var children = new List<GameObject>();
        foreach (Transform child in itemHolder.transform)
        {
            children.Add(child.gameObject);
        }
        children.ForEach(child => Destroy(child));

        //Rebuild content using the current inventory
        //TODO: keep a reference to the inventory
        PlayerInventory inventory = GetComponentInParent<PlayerInventory>();
        bool firstItemSet = false;
        if(filter == ItemFiler.all || filter == ItemFiler.rods)
        {
            firstItemSet = AddContainerToInventory(inventory.rodContainer, filter, firstItemSet);
        }
        if (filter == ItemFiler.all || filter == ItemFiler.baits)
        {
            firstItemSet = AddContainerToInventory(inventory.baitContainer, filter, firstItemSet);
        }
        if (filter == ItemFiler.all || filter == ItemFiler.Fishes)
        {
            firstItemSet = AddContainerToInventory(inventory.fishContainer, filter, firstItemSet);
        }
    }

    private bool AddContainerToInventory(SyncList<InventoryItem> container, ItemFiler filter, bool firstItemSet)
    {
        for (int i = 0; i < container.Count; i++)
        {
            //ItemType type = container[i].item.type;
            ItemObject itemObject = container[i].item;
            bool itemSelected = false;
            if(itemObject == null)
            {
                Debug.LogWarning("Item in a inventorycontainer is null");
                continue;
            }
            ItemType type = itemObject.type;
            if (filter != 0)
            {
                if (type == ItemType.rod && filter != ItemFiler.rods)
                {
                    continue;
                }
                else if (type == ItemType.bait && filter != ItemFiler.baits)
                {
                    continue;
                }
                else if (type == ItemType.fish && filter != ItemFiler.Fishes)
                {
                    continue;
                }
            }

            if (itemObject is rodObject rod) {
                if (fishingManager.GetSelectedRod().uid == rod.uid) { 
                    itemSelected = true;
                }
            }
            if (itemObject is baitObject bait)
            {
                if (fishingManager.GetSelectedBait().id == bait.id)
                {
                    itemSelected = true;
                }
            }

            GameObject inventoryItem = Instantiate(inventoryItemPrefab, itemHolder.transform, false);
            InventoryItemData invItemData = inventoryItem.GetComponent<InventoryItemData>();
            invItemData.SetInventoryItemData(container[i].item, itemSelected);
            //Show item info of first item in inventory, just to fill up empty space
            if (!firstItemSet)
            {
                invItemData.InventoryItemClicked();
                firstItemSet = true;
            }
        }
        return firstItemSet;
    }

    public void ShowItemInfo(ItemObject _item)
    {
        item = _item;
        string name;
        int amount;
        itemPreviewSelectedItemMark.SetActive(false);
        if (_item is rodObject rod)
        {
            name = rod.name;
            amount = rod.throwIns;

            if (fishingManager.GetSelectedRod().uid == rod.uid)
            {
                itemPreviewSelectedItemMark.SetActive(true);
            }
        }
        else if (_item is baitObject bait)
        {
            name = bait.name;
            amount = bait.throwIns;
            if (fishingManager.GetSelectedBait().id == bait.id)
            {
                itemPreviewSelectedItemMark.SetActive(true);
            }
        }
        else if (_item is FishObject fish)
        {
            name = fish.name;
            amount = fish.amount;
        }
        else
        {
            return;
        }

        itemInfoText.text = _item.description;
        itemNameText.text = name;
        itemAmountText.text = amount.ToString();
        itemPreviewImage.sprite = _item.sprite;

        if (_item.type == ItemType.rod || _item.type == ItemType.bait)
        {
            useItemButton.SetActive(true);
        }
        else
        {
            useItemButton.SetActive(false);
        }
    }

    //Called from game
    public void InventoryItemClicked()
    {
        ShowItemInfo(item);
    }

    //Called from game (inventory's use button)
    public void UseSelectedItem()
    {
        if (fishingManager == null)
        {
            fishingManager = GetComponentInParent<FishingManager>();
            if (fishingManager == null)
            {
                return;
            }
        }
        if (item is rodObject rod)
        {
            fishingManager.CmdSelectNewRod(rod, false);
        }
        else if (item is baitObject bait)
        {
            fishingManager.CmdSelectNewBait(bait, false);
        }
        CloseBackPack();
    }

    void EnsurePlayerController()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<playerController>();
        }
    }
}

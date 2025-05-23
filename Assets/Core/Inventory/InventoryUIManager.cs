using Mirror;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    PlayerController controller;
    ItemObject item;
    PlayerData playerData;

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
    MenuButtonStruct[] menuButtons;
    [SerializeField]
    Sprite selectedButtonSprite;
    [SerializeField]
    Sprite unselectedButtonSprite;

    //We can't show dictionaries in the inspector, so we put in into an array in the inspector and put it into a dict in the awake()
    readonly Dictionary<string, GameObject> selectMenuButtons = new();

    [Serializable]
    public enum ItemFiler
    {
        All = 0,
        Rods = 1,
        Baits = 2,
        Fishes = 3
    }

    [Serializable]
    public struct MenuButtonStruct
    {
        public string itemFilterName;
        public GameObject button;
    }

    ItemFiler StringToFilter(string filter) {
        if (Enum.TryParse<ItemFiler>(filter, true, out var result)) {
            return result;
        }
        throw new ArgumentException($"Invalid filter value: {filter}", nameof(filter));
    }

    private void Awake()
    {
        foreach(MenuButtonStruct menuButton in menuButtons)
        {
            selectMenuButtons.Add(menuButton.itemFilterName, menuButton.button);
        }
    }

    private void Start()
    {
        playerData = GetComponentInParent<PlayerData>();
        controller = GetComponentInParent<PlayerController>();
    }

    //called by a button in the game
    public void ClickSelectNewItem(string filter)
    {
        ToggleBackPack();
        ChangeBackPackMenu(filter);
    }

    public void ToggleBackPack()
    {
        ToggleBackPack(ItemFiler.Rods);
    }

    public void ToggleBackPack(ItemFiler filter)
    {
        if (inventoryUI.activeInHierarchy == false)
        {
            //0 is show all
            ChangeBackPackMenu(filter);
            inventoryUI.SetActive(true);
            EnsurePlayerController();
        }
        else
        {
            inventoryUI.SetActive(false);
            EnsurePlayerController();
        }
    }

    public void CloseBackPack()
    {
        EnsurePlayerController();
        inventoryUI.SetActive(false);
    }

    public void ChangeBackPackMenu(string menu)
    {
        ChangeBackPackMenu(StringToFilter(menu));
    }

    void ChangeBackPackMenu(ItemFiler itemFilter)
    {
        foreach (ItemFiler filter in Enum.GetValues(typeof(ItemFiler)))
        {
            if (selectMenuButtons.TryGetValue(filter.ToString(), out GameObject obj))
            {
                if (itemFilter == filter)
                {
                    obj.GetComponent<Image>().sprite = selectedButtonSprite;
                    obj.transform.SetParent(tabSelectionUpper.transform);
                }
                else
                {
                    obj.GetComponent<Image>().sprite = unselectedButtonSprite;
                    obj.transform.SetParent(tabSelectionUnder.transform);
                }
            }
        }
        BuildBackPackItems(itemFilter);
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
        if(filter == ItemFiler.All || filter == ItemFiler.Rods)
        {
            firstItemSet = AddContainerToInventory(inventory.rodContainer, filter, firstItemSet);
        }
        if (filter == ItemFiler.All || filter == ItemFiler.Baits)
        {
            firstItemSet = AddContainerToInventory(inventory.baitContainer, filter, firstItemSet);
        }
        if (filter == ItemFiler.All || filter == ItemFiler.Fishes)
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
                if (type == ItemType.Rod && filter != ItemFiler.Rods)
                {
                    continue;
                }
                else if (type == ItemType.Bait && filter != ItemFiler.Baits)
                {
                    continue;
                }
                else if (type == ItemType.Fish && filter != ItemFiler.Fishes)
                {
                    continue;
                }
            }

            if (itemObject is rodObject rod) {
                if (playerData.GetSelectedRod() != null && playerData.GetSelectedRod().uuid == rod.uuid) { 
                    itemSelected = true;
                }
            }
            if (itemObject is baitObject bait)
            {
                if (playerData.GetSelectedBait() != null && playerData.GetSelectedBait().id == bait.id)
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

            if (playerData.GetSelectedRod() != null && playerData.GetSelectedRod().uuid == rod.uuid)
            {
                itemPreviewSelectedItemMark.SetActive(true);
            }
        }
        else if (_item is baitObject bait)
        {
            name = bait.name;
            amount = bait.throwIns;
            if (playerData.GetSelectedBait() != null &&  playerData.GetSelectedBait().id == bait.id)
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

        if (_item.type == ItemType.Rod || _item.type == ItemType.Bait)
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
        if (playerData == null)
        {
            playerData = GetComponentInParent<PlayerData>();
            if (playerData == null)
            {
                return;
            }
        }
        if (item is rodObject rod)
        {
            playerData.CmdSelectNewRod(rod);
        }
        else if (item is baitObject bait)
        {
            playerData.CmdSelectNewBait(bait);
        }
        CloseBackPack();
    }

    void EnsurePlayerController()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<PlayerController>();
        }
    }
}

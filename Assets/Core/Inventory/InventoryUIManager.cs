using Mirror;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using NewItemSystem;

public class InventoryUIManager : MonoBehaviour
{
    ItemInstance selectedItem;
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
        }
        else
        {
            inventoryUI.SetActive(false);
        }
    }

    public void CloseBackPack()
    {
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
        RebuildInventory(itemFilter);
    }

    void RebuildInventory(ItemFiler filter)
    {
        // Remove previous UI entries
        var children = new List<GameObject>();
        foreach (Transform child in itemHolder.transform)
        {
            children.Add(child.gameObject);
        }
        children.ForEach(child => Destroy(child));

        InventoryItemData firstItem = null;

        PlayerInventory inventory = GetComponentInParent<PlayerInventory>();
        foreach (ItemInstance inst in inventory.items)
        {
            if (!MatchesFilter(inst, filter)) continue;

            GameObject go = Instantiate(inventoryItemPrefab, itemHolder.transform, false);
            InventoryItemData data = go.GetComponent<InventoryItemData>();
            data.SetInventoryItemData(
                inst,
                inst.uuid == playerData.GetSelectedRod()?.uuid || inst.uuid == playerData.GetSelectedBait()?.uuid
            );

            if (firstItem == null)
            {
                firstItem = data;
            }
        }
        firstItem?.InventoryItemClicked();
    }

    bool MatchesFilter(ItemInstance inst, ItemFiler filter)
    {
        if (filter == ItemFiler.All) return true;

        bool isRod = inst.def.GetBehaviour<RodBehaviour>() != null;
        bool isBait = inst.def.GetBehaviour<BaitBehaviour>() != null;
        bool isFish = inst.def.GetBehaviour<FishBehaviour>() != null;

        return (filter == ItemFiler.Rods && isRod) ||
               (filter == ItemFiler.Baits && isBait) ||
               (filter == ItemFiler.Fishes && isFish);
    }

    public void ShowItemInfo(ItemInstance inst)
    {
        selectedItem = inst;
        string name = inst.def.DisplayName;
        int amount = inst.GetState<StackState>()?.currentAmount ?? -10;
        itemPreviewSelectedItemMark.SetActive(false);
        itemInfoText.text = inst.def.DisplayName;
        itemNameText.text = name;
        itemAmountText.text = amount.ToString();
        itemPreviewImage.sprite = inst.def.Icon;

        bool equippable = inst.def.GetBehaviour<RodBehaviour>() != null || inst.def.GetBehaviour<BaitBehaviour>() != null;
        if (equippable)
        {
            useItemButton.SetActive(true);
        }
        else
        {
            useItemButton.SetActive(false);
        }
    }

    public void InventoryItemClicked()
    {
        ShowItemInfo(selectedItem);
    }

    // Called from button ingame
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

        if (selectedItem.def.GetBehaviour<RodBehaviour>() != null)
        {
            playerData.CmdSelectNewRod(selectedItem);
        }
        else if (selectedItem.def.GetBehaviour<BaitBehaviour>() != null)
        {
            playerData.CmdSelectNewBait(selectedItem);
        }
        CloseBackPack();
    }
}


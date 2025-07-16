using System.Collections.Generic;
using System.Linq;
using NewItemSystem;
using UnityEngine;

public class StoreUIManager : MonoBehaviour
{
    PlayerController controller;

    [SerializeField]
    GameObject storeUI;
    [SerializeField]
    GameObject StoreItemPrefab;
    [SerializeField]
    GameObject storeItemsHolder;
    [SerializeField]

    private void Start()
    {
        controller = GetComponentInParent<PlayerController>();
    }

    public void ToggleStore()
    {
        if(storeUI.activeInHierarchy == false)
        {
            storeUI.SetActive(true);
            ShowRodsPage();
            EnsurePlayerController();
        }
        else
        {
            storeUI.SetActive(false);
        }
    }

    //Called from button in game
    public void CloseStore()
    {
        EnsurePlayerController();
        storeUI.SetActive(false);
    }

    //Called from button in game
    public void ShowRodsPage()
    {
        List<ItemDefinition> rodsInStore = new();
        foreach (ItemDefinition item in ItemRegistry.GetFullItemsList())
        {
            if (item.GetBehaviour<RodBehaviour>() != null && item.GetBehaviour<ShopBehaviour>() != null)
            {
                rodsInStore.Add(item);
            }
        }
        BuildStorePage(rodsInStore.ToArray());
    }

    //Called from button in game
    public void ShowBaitsPage()
    {
        List<ItemDefinition> baitsInStore = new();
        foreach (ItemDefinition item in ItemRegistry.GetFullItemsList())
        {
            if (item.GetBehaviour<BaitBehaviour>() != null && item.GetBehaviour<ShopBehaviour>() != null)
            {
                baitsInStore.Add(item);
            }
        }
        BuildStorePage(baitsInStore.ToArray());
    }

    //Called from button in game
    public void ShowExtrasPage()
    {
        Debug.LogWarning("NotImplementedException");
        //BuildStorePage(ItemsInGame.storeItemMisc);
    }

    private void BuildStorePage(ItemDefinition[] itemObjects)
    {
        //Remove all previous items
        var children = new List<GameObject>();
        foreach (Transform child in storeItemsHolder.transform)
        {
            children.Add(child.gameObject);
        }
        children.ForEach(child => Destroy(child));

        foreach (ItemDefinition storeItem in itemObjects)
        {
            GameObject storeItemHolder = Instantiate(StoreItemPrefab, storeItemsHolder.transform);
            if(!storeItemHolder.TryGetComponent<StoreItemData>(out StoreItemData itemData))
            {
                continue;
            }
            itemData.SetStoreItemData(storeItem);
        }
    }

    void EnsurePlayerController()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<PlayerController>();
        }
    }
}

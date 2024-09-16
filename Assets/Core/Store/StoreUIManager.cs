using System.Collections.Generic;
using UnityEngine;

public class StoreUIManager : MonoBehaviour
{
    playerController controller;

    [SerializeField]
    GameObject storeUI;
    [SerializeField]
    GameObject StoreItemPrefab;
    [SerializeField]
    GameObject storeItemsHolder;
    [SerializeField]

    private void Start()
    {
        controller = GetComponentInParent<playerController>();
    }

    public void ToggleStore()
    {
        if(storeUI.activeInHierarchy == false)
        {
            storeUI.SetActive(true);
            ShowRodsPage();
            EnsurePlayerController();
            controller.IncreaseObjectsPreventingMovement();
            controller.IncreaseObjectsPreventingFishing();
        }
        else
        {
            storeUI.SetActive(false);
            controller.DecreaseObjectsPreventingMovement();
            controller.IncreaseObjectsPreventingFishing();
        }
    }

    //Called from button in game
    public void CloseStore()
    {
        EnsurePlayerController();
        controller.DecreaseObjectsPreventingMovement();
        controller.DecreaseObjectsPreventingFishing();
        storeUI.SetActive(false);
    }

    //Called from button in game
    public void ShowRodsPage()
    {
        BuildStorePage(ItemsInGame.storeItemRods);
    }

    //Called from button in game
    public void ShowBaitsPage()
    {
        BuildStorePage(ItemsInGame.storeItemBaits);
    }

    //Called from button in game
    public void ShowExtrasPage()
    {
        BuildStorePage(ItemsInGame.storeItemMisc);
    }

    private void BuildStorePage(StoreItemObject[] itemObjects)
    {
        //Remove all previous items
        var children = new List<GameObject>();
        foreach (Transform child in storeItemsHolder.transform)
        {
            children.Add(child.gameObject);
        }
        children.ForEach(child => Destroy(child));

        foreach (StoreItemObject storeItem in itemObjects)
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
            controller = GetComponentInParent<playerController>();
        }
    }
}

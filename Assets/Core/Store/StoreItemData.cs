using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemData : MonoBehaviour
{
    [SerializeField]
    Image itemImageContainer;
    [SerializeField]
    TMP_Text itemName;
    [SerializeField]
    TMP_Text itemDescription;
    [SerializeField]
    TMP_Text itemCostText;
    [SerializeField]
    GameObject buyCoinsButton;
    [SerializeField]
    GameObject buyBucksButton;
    [SerializeField]
    TMP_Text buyCoinsText;
    [SerializeField]
    TMP_Text buyBucksText;


    StoreItemObject storeItem;

    StoreUIManager storeUIManager;
    StoreManager storeManager;

    int priceFishBux;
    int priceFishCoins;

    // Start is called before the first frame update
    void Start()
    {
        storeUIManager = GetComponentInParent<StoreUIManager>();
        storeManager = GetComponentInParent<StoreManager>();
    }

    private void EnsureStoreUIManager()
    {
        if (storeUIManager == null)
        {
            storeUIManager = GetComponentInParent<StoreUIManager>();
        }
    }

    private void EnsureStoreManager()
    {
        if (storeManager == null)
        {
            storeManager = GetComponentInParent<StoreManager>();
        }
    }

    //Called from game
    public void BuyUsingCoins()
    {
        storeManager.BuyItem(storeItem, StoreManager.CurrencyType.coins);
    }

    //Called from game
    public void BuyUsingBucks()
    {
        storeManager.BuyItem(storeItem, StoreManager.CurrencyType.bucks);
    }

    public void SetStoreItemData(StoreItemObject item)
    {
        SetStoreItemData(item, item.itemPriceFishCoins, item.itemPriceFishBucks);
    }

    //Add a interface open where we can set the price manually, maybe for discount for some reason.
    public void SetStoreItemData(StoreItemObject item, int priceCoins, int priceBucks)
    {
        string name;
        if (item.itemObject is rodObject rod)
        {
            name = rod.name;
        }
        else if (item.itemObject is baitObject bait)
        {
            name = bait.name;
        }
        else {
            Debug.LogWarning($"SetStoreItemData not implemented for{item.itemObject}");
            return;
        }

        storeItem = item;
        itemName.text = name;
        itemDescription.text = item.itemObject.description;
        itemImageContainer.sprite = item.itemObject.sprite;
        if(priceCoins > 0)
        {
            buyCoinsButton.SetActive(true);
            buyCoinsText.text = priceCoins.ToString();
        }
        if (priceBucks > 0)
        {
            buyBucksButton.SetActive(true);
            buyBucksText.text = priceBucks.ToString();
        }

        priceFishCoins = priceCoins;
        priceFishBux = priceBucks;
    }
}

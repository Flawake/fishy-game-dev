using NewItemSystem;
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


    ItemDefinition storeItem;

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
        EnsureStoreManager();
        storeManager.BuyItem(storeItem, StoreManager.CurrencyType.coins);
    }

    //Called from game
    public void BuyUsingBucks()
    {
        EnsureStoreManager();
        storeManager.BuyItem(storeItem, StoreManager.CurrencyType.bucks);
    }

    public void SetStoreItemData(ItemDefinition item)
    {
        SetStoreItemData(item, item.GetBehaviour<ShopBehaviour>().PriceCoins, item.GetBehaviour<ShopBehaviour>().PriceBucks);
    }

    //Add a interface open where we can set the price manually, maybe for discount for some reason.
    public void SetStoreItemData(ItemDefinition item, int priceCoins, int priceBucks)
    {
        storeItem = item;
        itemName.text = item.DisplayName;
        itemDescription.text = item.Description;
        itemImageContainer.sprite = item.Icon;
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

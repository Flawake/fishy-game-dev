using ItemSystem;
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
    StoreManager storeManager;

    int priceFishBux;
    int priceFishCoins;

    // Start is called before the first frame update
    void Start()
    {
        storeManager = GetComponentInParent<StoreManager>();
    }


    //Called from game
    public void BuyUsingCoins()
    {
        storeManager.BuyItem(storeItem, StoreManager.CurrencyType.COINS);
    }

    //Called from game
    public void BuyUsingBucks()
    {
        storeManager.BuyItem(storeItem, StoreManager.CurrencyType.BUCKS);
    }

    public void SetStoreItemData(ItemDefinition item)
    {
        SetStoreItemData(item, item.GetBehaviour<ShopBehaviour>().PriceCoins, item.GetBehaviour<ShopBehaviour>().PriceBucks);
    }
    
    public void SetStoreItemData(ItemDefinition item, int priceCoins, int priceBucks)
    {
        storeItem = item;
        itemName.text = item.DisplayName;
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

        if (item.GetBehaviour<RodBehaviour>() != null) {
            ShowRodInformation(item);
        } 
        else if (item.GetBehaviour<BaitBehaviour>() != null) {
            ShowBaitInformation(item);
        } 
        else {
            itemDescription.text = "<color=\"black\">" + item.Description;
        }
    }

    private void ShowRodInformation(ItemDefinition rod)
    {
        RodBehaviour rodBehaviour = rod.GetBehaviour<RodBehaviour>();
        string durability = "Infinite";
        if (!rod.InfiniteUse)
        {
            DurabilityBehaviour durabilityBehaviour = rod.GetBehaviour<DurabilityBehaviour>();
            if (durabilityBehaviour == null) {
               durability = "Error"; 
            }
            else {
                durability = durabilityBehaviour.MaxDurability.ToString();
            }
        }

        string strength = rodBehaviour.Strength.ToString();
        string reach = rodBehaviour.ThrowDistance.ToString();
        itemDescription.text = $"<color=\"black\">Throw ins: <color=\"orange\">{durability} \n <color=\"black\">for fish till: <color=\"orange\">{strength} CM \n\r <color=\"black\">Reach: <color=\"orange\">{reach}";
    }

    private void ShowBaitInformation(ItemDefinition bait)
    {
        BaitBehaviour baitBehaviour = bait.GetBehaviour<BaitBehaviour>();

        string durability = "Infinite";
        if (!bait.InfiniteUse)
        {
            ShopBehaviour shopBehaviour = bait.GetBehaviour<ShopBehaviour>();
            if (shopBehaviour == null) {
               durability = "Error"; 
            }
            else {
                durability = shopBehaviour.Amount.ToString();
            }
        }

        string baitType = baitBehaviour.BaitType.ToString();

        itemDescription.text = $"<color=\"black\">Throw ins: <color=\"orange\">{durability} \n <color=\"black\">for fish that bite on: <color=\"orange\">{baitType}";
    }
}

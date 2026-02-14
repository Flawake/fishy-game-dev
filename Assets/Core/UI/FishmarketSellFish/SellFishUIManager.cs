using System.Collections.Generic;
using System.Linq;
using ItemSystem;
using Mirror;
using TMPro;
using UnityEngine;

public class SellFishUIManager : MonoBehaviour
{
    [SerializeField] GameObject sellFishUI;
    [SerializeField] GameObject sellFishObjectHolder;
    [SerializeField] GameObject sellFishObjectPrefab;
    [SerializeField] TMP_Text MoneyAmountText;

    private void BuildSellFishUI()
    {
        // Remove old instances
        foreach(Transform t in sellFishObjectHolder.transform)
        {
            Destroy(t.gameObject);
        }
        
        List<ItemInstance> items = NetworkClient.localPlayer.GetComponentInChildren<PlayerInventory>().GetItems();
        items.Where(item => item.HasBehaviour<FishBehaviour>())
        .ToList()
        .ForEach(item =>
        {
            GameObject sellFishObject = Instantiate(sellFishObjectPrefab, sellFishObjectHolder.transform);
            sellFishObject.GetComponent<SellableFishUIManager>().BuildSellFishObject(item);
        });

        gameObject.GetComponentInParent<PlayerData>().BucksAmountChanged += UpdateFishbucksAmount;
        UpdateFishbucksAmount();
    }

    public void OpenSellFishUI()
    {
        sellFishUI.SetActive(true);
        BuildSellFishUI();
    }

    // Called from button in game
    public void CloseSellFishUI()
    {
        sellFishUI.SetActive(false);
    }

    private void UpdateFishbucksAmount()
    {
        MoneyAmountText.text = gameObject.GetComponentInParent<PlayerData>().GetFishBucks().ToString();
    }
}

using System;
using ItemSystem;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellableFishUIManager : MonoBehaviour
{
    ItemInstance _thisFishItem;

    [SerializeField] private Image fishImage;
    [SerializeField] private TMP_Text fishNameText;
    [SerializeField] private TMP_Text sellPriceText;
    [SerializeField] private TMP_Text sellAmountText;

    private int _baseSellPrice;

    private int _sellAmount;

    /*
        FishBehaviour curFishBehaviour = curFish.GetBehaviour<FishBehaviour>();
        if (curFish == null || curFishBehaviour == null)
        {
            Debug.LogWarning($"Could not show information about a fish that should have had ID: {fishID}");
            return;
        }
        ClearContainers();

        StatFish statFish = NetworkClient.localPlayer.GetComponentInChildren<PlayerFishdexFishes>().GetStatFish(fishID);
        if (statFish == null)
        {
    */

    public void BuildSellFishObject(ItemInstance fishItem)
    {
        _thisFishItem = fishItem;
        FishBehaviour curFishBehaviour = fishItem.def.GetBehaviour<FishBehaviour>();
        if (curFishBehaviour == null)
        {
            Debug.LogWarning($"Could not show information about a fish that should have had ID: {fishItem.def.Id}");
            return;
        }

        UpdateSellAmount(1);

        _baseSellPrice = curFishBehaviour.MinMartketPrice;
        sellPriceText.text = _baseSellPrice.ToString();

        fishImage.sprite = fishItem.def.Icon;
        fishNameText.text = fishItem.def.DisplayName;
    }

    private void UpdateSellAmount(int amount)
    {
        _sellAmount = amount;
        sellAmountText.text = $"[ {_sellAmount} ]";

        sellPriceText.text = (_sellAmount * _baseSellPrice).ToString();
    }

    // Called from button ingame
    public void IncreaseSellAmount()
    {
        int newAmount = (_sellAmount % _thisFishItem.GetState<StackState>().currentAmount) + 1;
        UpdateSellAmount(newAmount);
    }

    // Called from button ingame
    public void DecreaseSellAmount()
    {
        int stackSize = _thisFishItem.GetState<StackState>().currentAmount;
        int newAmount = ((_sellAmount - 2 + stackSize) % stackSize) + 1;
        UpdateSellAmount(newAmount);
    }

    public void MaxSellAmount() {
        UpdateSellAmount(_thisFishItem.GetState<StackState>().currentAmount);
    }

    // Called from button ingame
    public void SellFish()
    {
        GetComponentInParent<SellFish>().CmdSellFish(_thisFishItem.def.Id, _sellAmount);
        GetComponentInParent<PlayerData>().ClientChangeFishBucksAmount(_sellAmount * _baseSellPrice);
        if (GetComponentInParent<PlayerInventory>().GetFishByDefinitionId(_thisFishItem.def.Id).GetState<StackState>().currentAmount <= 0)
        {
            Destroy(transform);
        }
    }
}

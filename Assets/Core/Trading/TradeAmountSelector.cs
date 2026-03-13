using System;
using ItemSystem;
using TMPro;
using TradeSystem;
using Unity.Mathematics;
using UnityEngine;

public class TradeAmountSelector : MonoBehaviour
{
    [SerializeField]
    TMP_InputField inputField;
    TradableItem item;
    int maxTradeAmount = 1;

    void OnEnable()
    {
        inputField.text = 1.ToString();
    }

    public void SetItem(TradableItem tradeItem)
    {
        item = tradeItem;
        if (tradeItem.Type == TradableItemType.Bucks)
        {
            maxTradeAmount = GetComponentInParent<PlayerData>().GetFishBucks();
        }
        else if (tradeItem.Type == TradableItemType.Item)
        {
            StackState stack = item.ItemInst.GetState<StackState>();
            if (stack != null)
            {
                maxTradeAmount = stack.currentAmount;
            }
        }
        else
        {
            throw new NotImplementedException(tradeItem.ItemInst.ToString());
        }
    }

    int ClampAmount()
    {
        int amount = int.Parse(inputField.text);
        amount = math.min(amount, maxTradeAmount);
        amount = math.max(amount, 0);
        inputField.text = amount.ToString();
        return amount;
    }

    // Calles from action in game
    public void AmountFieldDeselected()
    {
        ClampAmount();
    }

    // Called from button in game
    public void AddItems()
    {
        item.SetAmount(ClampAmount());
        GetComponentInParent<Trading>().AddItemToTrade(item, true);
        gameObject.SetActive(false);
    }
}

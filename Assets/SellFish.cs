using System;
using System.Collections.Generic;
using ItemSystem;
using Mirror;
using UnityEngine;

public class SellFish : NetworkBehaviour
{
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    PlayerDataSyncManager syncManager;
    [SerializeField]
    PlayerData playerData;

    [Command]
    public void CmdSellFish(int fishID, int sellAmount)
    {
        ItemInstance fish = inventory.GetFishByDefinitionId(fishID);

        if (fish == null)
        {
            Debug.Log("Provided fish to sell was null");
            return;
        }

        if (fish.def.InfiniteUse || fish.def.IsStatic)
        {
            Debug.Log("Why is this fish marked infiniteUse or static? Could not sell it");
            return;
        }

        StackState stackState = fish.GetState<StackState>();
        if (stackState == null)
        {
            Debug.LogWarning($"Could not get the stack state for fish id {fishID}");
            return;
        }

        if (stackState.currentAmount < sellAmount)
        {
            Debug.LogWarning($"Not enough fish with ID {fishID} to sell");
            return;
        }

        FishBehaviour curFishBehaviour = fish.def.GetBehaviour<FishBehaviour>();
        if (curFishBehaviour == null)
        {
            Debug.LogWarning($"Could not show information about a fish that should have had ID: {fish.def.Id}");
            return;
        }

        int moneyAmount = curFishBehaviour.MinMartketPrice * sellAmount;

        syncManager.ChangeFishBucksAmount(moneyAmount, false);
        if(!syncManager.ServerRemoveAmountFromStack(fish, sellAmount, false))
        {
            // Remove money from client when selling the fish fails
            syncManager.ChangeFishBucksAmount(-moneyAmount, true);
        }
        inventory.RemoveAmountFromStack(fish, sellAmount);
    }

    public int GetFishAmount()
    {
        int value = 0;
        List<ItemInstance> inventoryFishes = inventory.GetFishes();
        foreach (ItemInstance fish in inventoryFishes)
        {
            FishBehaviour fishBehaviour = fish.def.GetBehaviour<FishBehaviour>();
            StackState fishStack = fish.GetState<StackState>();
            value += fishBehaviour.MinMartketPrice * fishStack.currentAmount;
        }

        return value;
    }

    public int GetTotalFishValue()
    {
        int value = 0;
        List<ItemInstance> inventoryFishes = inventory.GetFishes();
        foreach (ItemInstance fish in inventoryFishes)
        {
            FishBehaviour fishBehaviour = fish.def.GetBehaviour<FishBehaviour>();
            StackState fishStack = fish.GetState<StackState>();
            value += fishBehaviour.MinMartketPrice * fishStack.currentAmount;
        }

        return value;
    }

    public void SellAllFishAtMarket()
    {
        CmdSellAllFishAtMarket();
        List<ItemInstance> inventoryFishes = inventory.GetFishes();
        foreach (ItemInstance fish in inventoryFishes)
        {

            playerData.ClientChangeFishBucksAmount(fish.def.GetBehaviour<FishBehaviour>().MinMartketPrice * fish.GetState<StackState>().currentAmount);
        }
    }

    [Command]
    private void CmdSellAllFishAtMarket()
    {
        List<ItemInstance> inventoryFishes = inventory.GetFishes();
        foreach (ItemInstance fish in inventoryFishes)
        {
            syncManager.ChangeFishBucksAmount(fish.def.GetBehaviour<FishBehaviour>().MinMartketPrice * fish.GetState<StackState>().currentAmount, false);
            inventory.RemoveItem(fish.uuid);
        }
    }
}

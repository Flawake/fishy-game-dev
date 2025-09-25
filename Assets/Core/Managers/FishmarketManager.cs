using System;
using ItemSystem;
using Mirror;
using UnityEngine;

public class FishmarketManager : NetworkBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerDataSyncManager syncManager;

    [Client]
    void ClientSellFish(Guid itemUuid, int quantity)
    {
        ItemInstance itemToSell = inventory.GetItem(itemUuid);
        if (itemToSell == null || itemToSell.def.GetBehaviour<FishBehaviour>() == null || !syncManager.CanSellItem(itemToSell) || itemToSell.GetState<StackState>().currentAmount < quantity)
        {
            return;
        }
        int price = CalculateFishPrice(itemToSell, quantity, 1);
        inventory.ClientAddItem().ServerConsumeFromStack(itemToSell, quantity);
        playerData.ClientChangeFishBucksAmount(price);
        CmdRequestSellFish(itemUuid, quantity);
    }
    
    [Command]
    void CmdRequestSellFish(Guid itemUuid, int quantity, NetworkConnectionToClient conn = null)
    {
        ItemInstance itemToSell = inventory.GetItem(itemUuid);
        if (itemToSell == null || itemToSell.def.GetBehaviour<FishBehaviour>() == null || !syncManager.CanSellItem(itemToSell) || itemToSell.GetState<StackState>().currentAmount < quantity)
        {
            return;
        }

        int price = CalculateFishPrice(itemToSell, quantity, 1);
        syncManager.ServerConsumeFromStack(itemToSell, quantity);
        syncManager.ChangeFishBucksAmount(price);


    }

    [Command]
    void CmdRequestMassSell()
    {
        
    }

    int CalculateFishPrice(ItemInstance item, int quantity, int reputation)
    {
        return 1 * reputation;
    }
}

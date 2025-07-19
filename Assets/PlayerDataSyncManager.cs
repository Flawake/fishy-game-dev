using System;
using UnityEngine;
using NewItemSystem;
using Mirror;

//Item manager should manage the syncronisation of items between the server and client.
public class PlayerDataSyncManager : MonoBehaviour
{
    [SerializeField]
    PlayerData playerData;
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    PlayerFishdexFishes fishdexFishes;

    [Server]
    public void ChangeFishCoinsAmount(int amount)
    {
        DatabaseCommunications.ChangeFishCoinsAmount(amount, playerData.GetUuid());
        playerData.ChangeFishCoinsAmount(amount);
    }

    [Server]
    public void ChangeFishBucksAmount(int amount)
    {
        DatabaseCommunications.ChangeFishBucksAmount(amount, playerData.GetUuid());
        playerData.ChangeFishBucksAmount(amount);
    }

    [Server]
    public void AddXP(int amount)
    {
        DatabaseCommunications.AddXP(amount, playerData.GetUuid());
        playerData.AddXp(amount);
    }

    [Server]
    public void AddItem(ItemInstance item)
    {
        AddItem(item, null, false);
    }

    [Server]
    public void AddItem(ItemInstance item, CurrentFish fish, bool fromCaught)
    {
        // First update the local inventory (this may merge stacks and change quantities).
        if (fish != null && fromCaught)
        {
            fishdexFishes.AddStatFish(fish);
            DatabaseCommunications.AddStatFish(fish, playerData.GetUuid());
        }

        inventory.AddItem(item);
        DatabaseCommunications.AddOrUpdateItem(item, playerData.GetUuid());
    }

    [Server]
    public void DestroyItem(ItemInstance item)
    {
        inventory.RemoveItem(item.uuid);
        DatabaseCommunications.DestroyItem(item, playerData.GetUuid());
    }

    internal void ChangeFishCoinsAmount(int? v)
    {
        throw new NotImplementedException();
    }

    internal void ChangeFishBucksAmount(int? v)
    {
        throw new NotImplementedException();
    }
}

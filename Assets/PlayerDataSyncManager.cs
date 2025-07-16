using System;
using UnityEngine;
using NewItemSystem;

//Item manager should manage the syncronisation of items between the server and client.
public class PlayerDataSyncManager : MonoBehaviour
{
    [SerializeField]
    PlayerData playerData;
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    PlayerFishdexFishes fishdexFishes;

    public void ChangeFishCoinsAmount(int amount) {
        DatabaseCommunications.ChangeFishCoinsAmount(amount, playerData.GetUuid());
        playerData.ChangeFishCoinsAmount(amount);
    }

    public void ChangeFishBucksAmount(int amount)
    {
        DatabaseCommunications.ChangeFishBucksAmount(amount, playerData.GetUuid());
        playerData.ChangeFishBucksAmount(amount);
    }

    public void AddXP(int amount) {
        DatabaseCommunications.AddXP(amount, playerData.GetUuid());
        playerData.AddXp(amount);
    }

    public void AddItem(ItemObject item)
    {
        AddItem(item, null, false);
    }

    public void AddItem(ItemObject item, CurrentFish fish, bool fromCaugh) {
         // Determine if we already hold a matching item to preserve persistent uuid
         if (inventory.ContainsItem(item, out Guid? existingUuid) && existingUuid.HasValue) {
             item.uuid = existingUuid.Value;
         }

        // First update the local inventory (this may merge stacks and change quantities).
        if (fish != null && fromCaugh) {
            fishdexFishes.AddStatFish(fish);
            DatabaseCommunications.AddStatFish(fish, playerData.GetUuid());
        }

        inventory.AddItem(item);

        // Retrieve the canonical reference now stored in the inventory so that the DB
        // gets the correct, up-to-date stack/durability values.
        ItemObject reference;
        if (item is rodObject)
            reference = inventory.GetRodByUID(item.uuid);
        else if (item is baitObject bait)
            reference = inventory.GetBaitByID(bait.id);
        else if (item is FishObject fishObj)
            reference = inventory.GetItemByID(fishObj.id, FishObject.AsString());
        else
            reference = item; // fallback – shouldn't happen for now

        var inst = LegacyItemAdapter.From(reference);
        if (inst != null)
            DatabaseCommunications.AddOrUpdateItem(inst, playerData.GetUuid());
    }

    public void DestroyItem(ItemObject item) {
        inventory.RemoveItem(item);
        var inst = LegacyItemAdapter.From(item);
        if (inst != null)
            DatabaseCommunications.DestroyItem(inst, playerData.GetUuid());
    }
}

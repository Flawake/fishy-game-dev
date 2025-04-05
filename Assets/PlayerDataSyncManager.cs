using UnityEngine;

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
        DatabaseCommunications.ChangeFishCoinsAmount(amount, playerData.GetUuidAsString());
        playerData.ChangeFishCoinsAmount(amount);
    }

    public void ChangeFishBucksAmount(int amount)
    {
        DatabaseCommunications.ChangeFishBucksAmount(amount, playerData.GetUuidAsString());
        playerData.ChangeFishBucksAmount(amount);
    }

    public void AddXP(int amount) {
        DatabaseCommunications.AddXP(amount, playerData.GetUuidAsString());
        playerData.AddXp(amount);
    }

    public void AddItem(ItemObject item)
    {
        AddItem(item, null, false);
    }

    public void AddItem(ItemObject item, CurrentFish fish, bool fromCaugh) {
        bool asNewItem = !inventory.ContainsItem(item);
        DatabaseCommunications.AddItem(item, asNewItem, playerData.GetUuidAsString());
        if (fish != null && fromCaugh) {
            fishdexFishes.AddStatFish(fish);
            DatabaseCommunications.AddStatFish(fish, playerData.GetUuidAsString());
        }
        inventory.AddItem(item);
    }

    public void DestroyItem(ItemObject item) {
        inventory.RemoveItem(item);
        DatabaseCommunications.DestroyItem(item, playerData.GetUuidAsString());
    }
}

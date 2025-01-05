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
    [SerializeField]
    DatabaseCommunications database;

    public void ChangeFishCoinsAmount(int amount) {
        database.ChangeFishCoinsAmount(amount);
        playerData.ChangeFishCoinsAmount(amount);
    }

    public void ChangeFishBucksAmount(int amount)
    {
        database.ChangeFishBucksAmount(amount);
        playerData.ChangeFishBucksAmount(amount);
    }

    public void AddXP(int amount) {
        database.AddXP(amount);
        playerData.AddXp(amount);
    }

    public void AddItem(ItemObject item)
    {
        AddItem(item, null, false);
    }

    public void AddItem(ItemObject item, CurrentFish fish, bool fromCaugh) {
        bool asNewItem = !inventory.ContainsItem(item);
        database.AddItem(item, asNewItem);
        if (fish != null && fromCaugh) {
            fishdexFishes.AddStatFish(fish);
            database.AddStatFish(fish);
        }
        inventory.AddItem(item);
    }

    public void DestroyItem(ItemObject item) {
        inventory.RemoveItem(item);
        database.DestroyItem(item);
    }
}

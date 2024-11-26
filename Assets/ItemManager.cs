using UnityEngine;

//Item manager should manage the syncronisation of items between the server and client.
public class ItemManager : MonoBehaviour
{
    [SerializeField]
    PlayerData playerData;
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    DatabaseCommunications database;

    public void ChangeFishCoinsAmount(int amount) {
        database.ChangeFishBucksAmount(amount);
        playerData.ChangeFishBucksAmount(amount);
    }

    public void ChangeFishBucksAmount(int amount)
    {
        database.ChangeFishBucksAmount(amount);
        playerData.ChangeFishBucksAmount(amount);
    }

    public void AddItem(ItemObject item)
    {
        AddItem(item, null);
    }

    public void AddItem(ItemObject item, CurrentFish fish) {
        bool asNewItem = !inventory.ContainsItem(item);
        database.AddItem(item, asNewItem);
        if (fish != null) {
            database.AddStatFish(fish);
        }
        inventory.AddItem(item);
    }

    public void DestroyItem(ItemObject item) {
        inventory.RemoveItem(item);
        database.DestroyItem(item);
    }
}

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
        if (inventory.ContainsItem(item))
        {
            DatabaseCommunications.IncreaseItem(item, playerData.GetUuid());  
        }
        else
        {
            DatabaseCommunications.AddNewItem(item, playerData.GetUuid());   
        }
        if (fish != null && fromCaugh) {
            fishdexFishes.AddStatFish(fish);
            DatabaseCommunications.AddStatFish(fish, playerData.GetUuid());
        }
        inventory.AddItem(item);
    }

    public void DestroyItem(ItemObject item) {
        inventory.RemoveItem(item);
        DatabaseCommunications.DestroyItem(item, playerData.GetUuid());
    }
}

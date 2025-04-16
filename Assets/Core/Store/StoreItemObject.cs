using UnityEngine;

[CreateAssetMenu(fileName = "StoreItem", menuName = "Store")]
public class StoreItemObject : ScriptableObject
{
    public ItemObject itemObject;
    public int itemPriceFishCoins;
    public int itemPriceFishBucks;
    
    public StoreItemObject Clone()
    {
        StoreItemObject clone = Instantiate(this);
        if (itemObject != null)
        {
            clone.itemObject = Instantiate(itemObject);
        }
        return clone;
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "StoreItem", menuName = "Store")]
public class StoreItemObject : ScriptableObject
{
    public ItemObject itemObject;
    public int itemPriceFishCoins;
    public int itemPriceFishBucks;
}

using ItemSystem;
using Mirror;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows how much is left of the equipped rod and bait. Two things move it: the player equipping
/// something else, and the equipped thing wearing down, so it listens for both rather than polling.
/// </summary>
public class ItemQualityDisplay : MonoBehaviour
{
    [SerializeField]
    GameObject RodQuality;
    [SerializeField]
    TMP_Text RodQualityText;
    [SerializeField]
    GameObject BaitQuality;
    [SerializeField]
    TMP_Text BaitQualityText;

    PlayerData playerData;
    PlayerInventory inventory;

    void Start()
    {
        // Every player carries this prefab, but only the local one has a HUD worth updating.
        if (!NetworkClient.localPlayer)
        {
            return;
        }

        playerData = GetComponentInParent<PlayerData>();
        inventory = GetComponentInParent<PlayerInventory>();

        playerData.selectedRodChanged += Refresh;
        playerData.selectedBaitChanged += Refresh;
        inventory.Changed += Refresh;

        Refresh();
    }

    void OnDestroy()
    {
        if (playerData != null)
        {
            playerData.selectedRodChanged -= Refresh;
            playerData.selectedBaitChanged -= Refresh;
        }
        if (inventory != null)
        {
            inventory.Changed -= Refresh;
        }
    }

    void Refresh()
    {
        Show(playerData.GetSelectedRod(), RodQuality, RodQualityText);
        Show(playerData.GetSelectedBait(), BaitQuality, BaitQualityText);
    }

    /// <summary>
    /// The same rule the backpack uses: durability for something that wears out, stack size
    /// otherwise, and nothing at all for an item that never runs down.
    /// </summary>
    static void Show(ItemInstance item, GameObject holder, TMP_Text text)
    {
        if (item == null || item.def.IsStatic || item.def.InfiniteUse)
        {
            holder.SetActive(false);
            return;
        }

        int quality = item.GetState<DurabilityState>()?.remaining
                   ?? item.GetState<StackState>()?.currentAmount
                   ?? 1;

        holder.SetActive(true);
        text.text = quality.ToString();
    }
}

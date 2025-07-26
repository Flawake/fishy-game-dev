using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NewItemSystem;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    //Not all variables that should be synced between the client and the player are a syncVar
    //For example: no player except yourself should know how much fishcoins or fishbucks you have.

    //Variables that are not synced between ALL players
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    PlayerDataSyncManager playerDataManager;
    [SerializeField]
    PlayerFishdexFishes fishdexFishes;
    [SerializeField]
    int availableFishCoins;
    [SerializeField]
    int availableFishBucks;
    private HashSet<Guid> friendlist = new HashSet<Guid>();
    // bool to indicate wether the reqeust was a sent or a receiver request.
    private Dictionary<Guid, bool> pendingFriendRequests = new Dictionary<Guid, bool>();
    
    private readonly Dictionary<SpecialEffectType, (float value, DateTime expiry)> activeSpecialEffects = new Dictionary<SpecialEffectType, (float value, DateTime expiry)>();

    //Variables that are synced between ALL players
    [SyncVar, SerializeField]
    string playerName;
    [SyncVar(hook = nameof(XpUpdatedClient)), SerializeField]
    int playerXp;
    [SyncVar, SerializeField]
    bool showInventory;
    [SyncVar, SerializeField]
    int lastItemUID;
    [SyncVar, SerializeField]
    Color32 chatColor;
    [SyncVar, SerializeField]
    double playerStartPlayingTime;
    [SyncVar, SerializeField]
    ulong playerPlayTimeAtStart;

    public event Action CoinsAmountChanged;
    public event Action BucksAmountChanged;
    public event Action XPAmountChanged;

    //TODO: fishao used bezier curves for it's line, I might need to do the same.
    //com.ax3.display.rod
    //com.ax3.fishao.locations.entries.FieldChat
    //rod radius: 200, 300, 500, small, normal, big
    //Keep this order of syncvars, the dirty bits rely on it.

    //We don't want to sync the rod since we want the player to have the selectedRod be a reference to a rod in the inventory.
    //We only need the id to do this. So the ID is synced and the selectedItem is found from the inventory
    [SyncVar(hook = nameof(SelectedRodChanged))]
    string selectedRodUidStr;

    public Guid SelectedRodUid
    {
        get => Guid.TryParse(selectedRodUidStr, out var guid) ? guid : Guid.Empty;
        set => selectedRodUidStr = value.ToString();
    }
    
    // Default the value to -1 (no bait has that value) To trigger a change when the player selected the bait with id 0
    [SyncVar(hook = nameof(SelectedBaitChanged))]
    int selectedBaitId = -1;

    private ItemInstance selectedRod;
    private ItemInstance selectedBait;

    public event Action selectedRodChanged;
    public event Action selectedBaitChanged;


    [SerializeField, SyncVar]
    Guid uuid;
    bool uuidSet = false;

    [Server]
    public void SetUuid(Guid playerUuid)
    {
        if (uuidSet)
        {
            Debug.LogWarning("Trying to set UUID again, a players UUID should only be set once");
            return;
        }
        uuid = playerUuid;
        uuidSet = true;
    }

    public Guid GetUuid()
    {
        return uuid;
    }

    public string GetUuidAsString()
    {
        return GetUuid().ToString();
    }
    
    [Client]
    public void ClientRequestUseEffect(ItemInstance item) {
        if (item.def.GetBehaviour<SpecialBehaviour>() == null)
        {
            Debug.LogWarning($"Item with ID {item.def.Id} has no known special effect");
            return;
        }
        CmdRequestUseEffect(item);
    }

    [Command]
    private void CmdRequestUseEffect(ItemInstance danglingItem)
    {
        ItemInstance itemReference = inventory.GetItem(danglingItem.uuid);
        
        if (itemReference == null)
        {
            Debug.LogWarning("Item was not present in the inventory");
            return;
        }
        
        if (itemReference.def.GetBehaviour<SpecialBehaviour>() != null)
        {
            ServerAddNewSpecialEffect(itemReference);
        }
        else
        {
            Debug.LogWarning($"Item with ID {danglingItem.def.Id} has no known special effect");
        }
    }
    
    [Server]
    private void ServerAddNewSpecialEffect(ItemInstance itemReference) 
    {
        SpecialBehaviour specialEffect = itemReference.def.GetBehaviour<SpecialBehaviour>();
        if (specialEffect == null)
        {
            Debug.LogWarning($"Item with ID {itemReference.def.Id} has no known special effect");
            return;
        }
        
        // Consume the item from stack
        playerDataManager.ServerConsumeFromStack(itemReference);
        
        // Apply the special effect
        ApplySpecialEffect(specialEffect.EffectType, specialEffect.EffectValue, specialEffect.DurationSeconds);
        
        // Notify client
        TargetAddNewSpecialEffect(specialEffect);
    }

    [TargetRpc]
    private void TargetAddNewSpecialEffect(SpecialBehaviour specialEffect)
    {
        ApplySpecialEffect(specialEffect.EffectType, specialEffect.EffectValue, specialEffect.DurationSeconds);
    }



    [Server]
    private void ApplySpecialEffect(SpecialEffectType effectType, float value, float durationSeconds)
    {
        DateTime expiry = DateTime.UtcNow.AddSeconds(durationSeconds);
        activeSpecialEffects[effectType] = (value, expiry);
    }

    public Dictionary<SpecialEffectType, (float value, DateTime expiry)> GetActiveSpecialEffects()
    {
        RemoveExpiredSpecialEffects();
        return activeSpecialEffects;
    }

    private void RemoveExpiredSpecialEffects()
    {
        var expiredEffects = new List<SpecialEffectType>();
        
        foreach (var effect in activeSpecialEffects)
        {
            if (effect.Value.expiry <= DateTime.UtcNow)
            {
                expiredEffects.Add(effect.Key);
            }
        }
        
        foreach (var expiredEffect in expiredEffects)
        {
            activeSpecialEffects.Remove(expiredEffect);
            Debug.Log($"Special effect {expiredEffect} has expired");
        }
    }

    /// <summary>
    /// Gets the current luck multiplier from active special effects
    /// </summary>
    /// <returns>The luck multiplier (1.0 = no bonus)</returns>
    public float GetLuckMultiplier()
    {
        RemoveExpiredSpecialEffects();
        
        if (activeSpecialEffects.TryGetValue(SpecialEffectType.LuckBoost, out var luckEffect))
        {
            return luckEffect.value;
        }
        
        return 1.0f; // No luck boost
    }

    /// <summary>
    /// Gets the current wait time reduction from active special effects
    /// </summary>
    /// <returns>The wait time reduction in seconds</returns>
    public float GetWaitTimeReduction()
    {
        RemoveExpiredSpecialEffects();
        
        if (activeSpecialEffects.TryGetValue(SpecialEffectType.WaitTimeReduction, out var waitEffect))
        {
            return waitEffect.value;
        }
        
        return 0f; // No wait time reduction
    }

    [Server]
    public void SetUsername(string username)
    {
        playerName = username;
    }

    public string GetUsername()
    {
        return playerName;
    }

    [Server]
    public void SelectNewRod(ItemInstance newRod, bool fromDatabase)
    {
        if (!fromDatabase)
        {
            if (newRod != null)
            {
                DatabaseCommunications.SelectOtherItem(newRod, GetUuid());
            }
        }
        selectedRod = newRod;
        SelectedRodUid = newRod.uuid;
    }

    [Command]
    public void CmdSelectNewRod(ItemInstance newRod)
    {
        if (selectedRod != null)
        {
            if (selectedRod.uuid == newRod.uuid)
            {
                return;
            }
        }

        //The newRod variable might be a newly crafted rod, so get it as a reference from the inventory, then the inventory get's updated when this specific item is updated
        ItemInstance rodInventoryReference;
        if (newRod.def.MaxStack == 1)
        {
            rodInventoryReference = inventory.GetRodByUuid(newRod.uuid);
        }
        else
        {
            //Probably never needed, but catch it and warn just in case, why do rods have the stackable flag at all???
            Debug.LogWarning("Why does this rod have a stacksize of more than one???");
            return;
        }

        if (rodInventoryReference == null)
        {
            Debug.Log("rodInventoryReference is null");
            return;
        }

        if (!rodInventoryReference.HasBehaviour<RodBehaviour>())
        {
            Debug.Log("Given item has no RodBehaviour attached");
        }

        SelectNewRod(rodInventoryReference, false);
    }

    [Server]
    public void SelectNewBait(ItemInstance newBait, bool fromDatabase)
    {
        if (!fromDatabase)
        {
            if (newBait != null)
            {
                DatabaseCommunications.SelectOtherItem(newBait, GetUuid());
            }
        }
        selectedBait = newBait;
        selectedBaitId = newBait.def.Id;
    }

    [Command]
    public void CmdSelectNewBait(ItemInstance newBait)
    {
        if (selectedBait != null)
        {
            if (selectedBait.def.Id == newBait.def.Id)
            {
                return;
            }
        }

        //The newBait variable might be a newly crafted bait, so get it as a reference from the inventory, then the inventory get's updated when this specific item is updated
        ItemInstance baitInventoryReference;
        if (newBait.def.MaxStack > 1)
        {
            baitInventoryReference = inventory.GetBaitByDefinitionId(newBait.def.Id);
        }
        else
        {
            //Probably never needed, but catch it and warn just in case
            Debug.LogWarning("Can't get bait that is not stackable, should be got with the uid");
            return;
        }

        if (baitInventoryReference == null)
        {
            Debug.Log("baitInventoryReference is null");
            return;
        }

        SelectNewBait(baitInventoryReference, false);
    }

    void SelectedRodChanged(string oldValue, string newValue)
    {
        if (!isLocalPlayer)
        {
            return;
        }
        
        Guid _ = Guid.TryParse(oldValue, out var o) ? o : Guid.Empty;
        Guid newRodUuid = Guid.TryParse(newValue, out var n) ? n : Guid.Empty;
        StartCoroutine(SelectedRodChangedCoroutine(newRodUuid));
    }

    //Inventory might not yet be synced so it may not yet have the item available in the inventory
    IEnumerator SelectedRodChangedCoroutine(Guid newRodUuid) {
        ItemInstance newRod;
        while ((newRod = inventory.GetRodByUuid(newRodUuid)) == null)
        {
            yield return new WaitForSeconds(0.05f);
        }
        selectedRod = newRod;
        selectedRodChanged?.Invoke();
    }

    void SelectedBaitChanged(int _, int newBaitId)
    {
        if (!isLocalPlayer)
        {
            return;
        }
        StartCoroutine(SelectedBaitChangedCoroutine(newBaitId));
    }

    IEnumerator SelectedBaitChangedCoroutine(int newBaitId)
    {
        ItemInstance newBait;
        while ((newBait = inventory.GetBaitByDefinitionId(newBaitId))  == null)
        {
            yield return new WaitForSeconds(0.05f);
        }
        selectedBait = newBait;
        selectedBaitChanged?.Invoke();
    }

    public ItemInstance GetSelectedRod()
    {
        return selectedRod;
    }

    public ItemInstance GetSelectedBait()
    {
        return selectedBait;
    }

    //TODO: make a function for in the syncManager, this should do the DB calls.
    [Server]
    public void ChangeRodQuality(ItemInstance rodReference, int amount)
    {
        if (rodReference.def.GetBehaviour<RodBehaviour>() == null)
        {
            Debug.LogWarning("Rod reference doesn't have a RodBehaviour attached");
            return;
        }
        if (rodReference.def.IsStatic)
        {
            return;
        }
        
        // Use the sync manager to handle durability and database updates
        bool success = playerDataManager.ServerTryUseItem(rodReference);

        // Check if the rod broke (durability reached 0)
        if (success)
        {
            DurabilityState durabilityState = rodReference.GetState<DurabilityState>();
            if (durabilityState != null && durabilityState.remaining <= 0)
            {
                // Automatically replace with default rod (Bamboo Rod with ID 1000)
                ItemInstance defaultRod = inventory.GetRodByDefinitionId(1000);
                if (defaultRod == null)
                {
                    Debug.LogWarning("Default rod (Bamboo Rod) not found in inventory, cannot replace broken rod");
                    return;
                }
                
                // Check if the broken rod is currently selected
                bool isCurrentlySelected = (selectedRod != null && selectedRod.uuid == rodReference.uuid);
                
                // If the broken rod was selected, automatically select the default rod
                if (isCurrentlySelected)
                {
                    SelectNewRod(defaultRod, false);
                    Debug.Log($"Rod broke! Automatically replaced with {defaultRod.def.DisplayName}");
                }
            }
        }
    }

    [TargetRpc]
    private void RpcChangeBaitStats(ItemInstance bait, int amount)
    {
        //baitObject here is a nely created bait, we need to get the bait reference from the player inventory.
        ItemInstance inventoryBait = inventory.GetBaitByDefinitionId(bait.def.Id);
        inventoryBait.GetState<DurabilityState>().remaining += amount;
    }

    /// <summary>
    /// Uses bait by consuming one from the stack (for stackable baits)
    /// </summary>
    /// <param name="baitReference">The bait to use</param>
    [Server]
    public void UseBait(ItemInstance baitReference)
    {
        if (baitReference.def.GetBehaviour<BaitBehaviour>() == null)
        {
            Debug.LogWarning("Bait reference doesn't have a BaitBehaviour attached");
            return;
        }
        if (baitReference.def.IsStatic)
        {
            return;
        }
        
        // Use the sync manager to handle stack consumption and database updates
        bool success = playerDataManager.ServerConsumeFromStack(baitReference);

        // Check if the bait stack is now empty
        if (success)
        {
            StackState stackState = baitReference.GetState<StackState>();
            if (stackState != null && stackState.currentAmount <= 0)
            {
                // Automatically replace with default bait (Hook with ID 0)
                ItemInstance defaultBait = inventory.GetBaitByDefinitionId(0);
                if (defaultBait == null)
                {
                    Debug.LogWarning("Default bait (Hook) not found in inventory, cannot replace empty bait stack");
                    return;
                }
                
                // Check if the empty bait is currently selected
                bool isCurrentlySelected = (selectedBait != null && selectedBait.def.Id == baitReference.def.Id);
                
                // If the empty bait was selected, automatically select the default bait
                if (isCurrentlySelected)
                {
                    SelectNewBait(defaultBait, false);
                    Debug.Log($"Bait stack empty! Automatically replaced with {defaultBait.def.DisplayName}");
                }
            }
        }
    }

    /// <summary>
    /// Command to request using bait from client
    /// </summary>
    /// <param name="baitReference">The bait to use</param>
    [Command]
    public void CmdUseBait(ItemInstance baitReference)
    {
        if (baitReference == null)
        {
            Debug.LogWarning("Cannot use null bait reference");
            return;
        }

        // Get the bait reference from inventory to ensure we're working with the correct instance
        ItemInstance inventoryBait = inventory.GetBaitByDefinitionId(baitReference.def.Id);
        if (inventoryBait == null)
        {
            Debug.LogWarning("Bait not found in inventory");
            return;
        }

        UseBait(inventoryBait);
    }

    //The show inventory on profile setting, this is not yet implemented
    [Server]
    void SetShowInventory(bool show)
    {
        showInventory = show;
    }

    bool GetShowInventory()
    {
        return showInventory;
    }

    [Server]
    public bool ParsePlayerData(string jsonPlayerData, Guid userID)
    {
        try
        {
            UserData playerData = JsonUtility.FromJson<UserData>(jsonPlayerData);
            inventory.SaveInventory(playerData);
            fishdexFishes.SaveFishStats(playerData);

            SetUuid(userID);
            SetFishCoins(playerData.coins);
            SetFishBucks(playerData.bucks);
            SetXp(playerData.xp);
            SetStartPlayTime();
            SetTotalPlayTimeAtStart(playerData.total_playtime);
            ServerSetFriendList(playerData.friends);
            ServerSetFriendRequests(playerData.friend_requests);
            SetShowInventory(false);
            
            // Ensure player has default rod and bait
            EnsureDefaultItems();
            
            // If no rod is selected, automatically select the default rod
            if (selectedRod == null)
            {
                ItemInstance defaultRod = inventory.GetRodByDefinitionId(1000);
                if (defaultRod != null)
                {
                    SelectNewRod(defaultRod, false);
                }
            }
            
            // If no bait is selected, automatically select the default bait
            if (selectedBait == null)
            {
                ItemInstance defaultBait = inventory.GetBaitByDefinitionId(0);
                if (defaultBait != null)
                {
                    SelectNewBait(defaultBait, false);
                }
            }
        } catch (Exception e)
        {
            Debug.LogWarning(e);
            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Ensures the player always has the default rod (Bamboo Rod) and default bait (Hook) in their inventory
    /// </summary>
    [Server]
    private void EnsureDefaultItems()
    {
        // Check if player has default rod (Bamboo Rod - ID 1000)
        ItemInstance defaultRod = inventory.GetRodByDefinitionId(1000);
        if (defaultRod == null)
        {
            // Create and add default rod
            ItemDefinition rodDef = ItemRegistry.Get(1000);
            if (rodDef != null)
            {
                ItemInstance newRod = new ItemInstance { def = rodDef, uuid = Guid.NewGuid() };
                playerDataManager.AddItem(newRod);
                Debug.Log("Added default Bamboo Rod to player inventory");
            }
        }
        
        // Check if player has default bait (Hook - ID 0)
        ItemInstance defaultBait = inventory.GetBaitByDefinitionId(0);
        if (defaultBait == null)
        {
            // Create and add default bait
            ItemDefinition baitDef = ItemRegistry.Get(0);
            if (baitDef != null)
            {
                ItemInstance newBait = new ItemInstance { def = baitDef, uuid = Guid.NewGuid() };
                playerDataManager.AddItem(newBait);
                Debug.Log("Added default Hook to player inventory");
            }
        }
    }

    Guid GuidFromBytes(byte[] scrambled_uuid_bytes)
    {
        byte[] reorderedBytes = new byte[16];
        // First 4 bytes (little-endian)
        reorderedBytes[0] = scrambled_uuid_bytes[3];
        reorderedBytes[1] = scrambled_uuid_bytes[2];
        reorderedBytes[2] = scrambled_uuid_bytes[1];
        reorderedBytes[3] = scrambled_uuid_bytes[0];
        // Next 2 bytes (little-endian)
        reorderedBytes[4] = scrambled_uuid_bytes[5];
        reorderedBytes[5] = scrambled_uuid_bytes[4];
        // Next 2 bytes (little-endian)
        reorderedBytes[6] = scrambled_uuid_bytes[7];
        reorderedBytes[7] = scrambled_uuid_bytes[6];
        // Remaining 8 bytes (big-endian)
        Array.Copy(scrambled_uuid_bytes, 8, reorderedBytes, 8, 8);
        return new Guid(reorderedBytes);
    }

    [Server]
    public void SetRandomColor()
    {
        switch (UnityEngine.Random.Range(0, 6))
        {
            //TODO, can the devisions be done at compile time?
            case 0:
                //Red
                SetChatColor(new Color(188f / 255f, 6f / 255f, 6f / 255f, 255f / 255f));
                break;
            case 1:
                //Dark green
                SetChatColor(new Color(34f / 255f, 117f / 255f, 56f / 255f, 255f / 255f));
                break;
            case 2:
                //Dark blue
                SetChatColor(new Color(37f / 255f, 56f / 255f, 138f / 255f, 255f / 255f));
                break;
            case 3:
                //Darker Cyan
                SetChatColor(new Color(54f / 255f, 149f / 255f, 168f / 255f, 255f / 255f));
                break;
            case 4:
                //Magenta
                SetChatColor(new Color(214f / 255f, 49f / 255f, 156f / 255f, 255f / 255f));
                break;
            case 5:
                //Purple
                SetChatColor(new Color(140f / 255f, 50f / 255f, 161f / 255f, 255f / 255f));
                break;
            default:
                UnityEngine.Debug.LogWarning("Random color did not return a color, defaulting to black");
                SetChatColor(Color.black);
                break;
        }
    }

    [Server]
    public bool GuidInFriendList(Guid userID)
    {
        return friendlist.Contains(userID);
    }

    [Server]
    public bool FriendrequestSendToGuid(Guid userID)
    {
        if (pendingFriendRequests.TryGetValue(userID, out bool requestSent))
        {
            return requestSent;
        }

        return false;
    }

    public HashSet<Guid> GetFriendList()
    {
        return friendlist;
    }

    public Dictionary<Guid, bool> GetPendingFriendRequests()
    {
        return pendingFriendRequests;
    }

    public bool FriendrequestReceivedFromGuid(Guid userID)
    {
        if (pendingFriendRequests.TryGetValue(userID, out bool requestSent))
        {
            return !requestSent;
        }

        return false;
    }

    [Server]
    private void ServerSetFriendList(UserData.Friend[] friends)
    {
        foreach (UserData.Friend friend in friends)
        {
            if (friend.UserOne != GetUuid() && friend.UserTwo != GetUuid())
            {
                Debug.LogWarning($"Got a friend in the friend list where user_one nor user_two matches the player uuid: user_one {friend.UserOne}, user_two {friend.UserTwo}, player_uuid {GetUuid()}");
            }

            friendlist.Add(friend.UserOne == GetUuid() ? friend.UserTwo : friend.UserOne);
        }
    }

    [Command]
    private void CmdGetFriendList() {
        RpcSetFriendList(friendlist);
    }

    [TargetRpc]
    private void RpcSetFriendList(HashSet<Guid> newFriendlist)
    {
        friendlist = newFriendlist;
    }

    // Callable from both server and client
    public void AddToFriendList(Guid userID)
    {
        friendlist.Add(userID);
        if (isServer)
        {
            ClientAddToFriendList(userID);
        }
    }

    [TargetRpc]
    private void ClientAddToFriendList(Guid userID)
    {
        friendlist.Add(userID);
    }

    // Callable from both server and client
    public void RemoveFromFriendList(Guid userID)
    {
        friendlist.Remove(userID);
        if (isServer)
        {
            ClientRemoveFromFriendList(userID);
        }
    }

    [TargetRpc]
    private void ClientRemoveFromFriendList(Guid userID)
    {
        friendlist.Remove(userID);
    }

    [Server]
    private void ServerSetFriendRequests(UserData.FriendRequest[] requests)
    {
        foreach (UserData.FriendRequest request in requests)
        {
            pendingFriendRequests.Add(
                request.UserOne == GetUuid() ? request.UserTwo : request.UserOne,
                request.RequestSenderId == GetUuid()
                );
        }
    }

    [Command]
    private void CmdGetFriendRequestList() {
        ClientSetFriendRequestList(pendingFriendRequests);
    }

    [TargetRpc]
    private void ClientSetFriendRequestList(Dictionary<Guid, bool> newFriendRequestlist)
    {
        pendingFriendRequests = newFriendRequestlist;
    }


    public void AddNewFriendRequest(Guid userID, bool requestSend)
    {
        pendingFriendRequests.Add(userID, requestSend);
        if (isServer)
        {
            ClientAddToFriendRequestList(userID, requestSend);
        }
    }

    [TargetRpc]
    private void ClientAddToFriendRequestList(Guid userID, bool requestSend)
    {
        pendingFriendRequests.Add(userID, requestSend);
    }

    // Callable from both server and client
    public void RemoveFromFriendRequestList(Guid userID)
    {
        pendingFriendRequests.Remove(userID);
        if (isServer)
        {
            ClientRemoveFromFriendRequestList(userID);
        }
    }

    // This will most likely get called twice, once from the client itself and once from the server.
    // But we need the interface in both cases and it doesn't really matter except for a few wasted clock cycles
    [TargetRpc]
    private void ClientRemoveFromFriendRequestList(Guid userID)
    {
        pendingFriendRequests.Remove(userID);
    }

    [Server]
    public void SetChatColor(Color32 color)
    {
        chatColor = color;
    }

    public Color32 GetChatColor()
    {
        return chatColor;
    }

    public string GetChatColorAsRGBAString()
    {
        return ColorUtility.ToHtmlStringRGBA(chatColor);
    }

    [Server]
    public void SetXp(int xp)
    {
        playerXp = xp;
    }

    [Server]
    public void AddXp(int xp)
    {
        SetXp(GetXp() + xp);
    }

    public int GetXp()
    {
        return playerXp;
    }

    void XpUpdatedClient(int _old, int _new)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        XPAmountChanged?.Invoke();
    }

    [Server]
    public void ChangeFishCoinsAmount(int Amount)
    {
        SetFishCoins(GetFishCoins() + Amount);
    }

    [Server]
    private void SetFishCoins(int newAmount)
    {
        availableFishCoins = newAmount;
        //isServer does check if this object has also been spawned on clients
        if (isServer)
        {
            TargetSetFishCoins(newAmount);
        }
    }

    [Command]
    public void CmdGetFishCoins()
    {
        TargetSetFishCoins(availableFishCoins);
    }

    [TargetRpc]
    private void TargetSetFishCoins(int newAmount)
    {
        availableFishCoins = newAmount;
        CoinsAmountChanged?.Invoke();
    }

    public int GetFishCoins()
    {
        return availableFishCoins;
    }

    [Server]
    public void ChangeFishBucksAmount(int Amount)
    {
        SetFishBucks(GetFishBucks() + Amount);
    }

    [Server]
    private void SetFishBucks(int newAmount)
    {
        availableFishBucks = newAmount;
        //isServer does also check if this object has been spawned on clients
        if (isServer)
        {
            TargetSetFishBucks(newAmount);
        }
    }

    [Command]
    public void CmdGetFishBucks()
    {
        TargetSetFishBucks(availableFishBucks);
    }

    [TargetRpc]
    private void TargetSetFishBucks(int newAmount)
    {
        availableFishBucks = newAmount;
        BucksAmountChanged?.Invoke();
    }

    public int GetFishBucks()
    {
        return availableFishBucks;
    }


    bool startPlaytimeSet = false;
    [Server]
    void SetStartPlayTime()
    {
        if(startPlaytimeSet)
        {
            Debug.LogWarning("playerStartPlayingTime was already set");
            return;
        }
        startPlaytimeSet = true;
        playerStartPlayingTime = NetworkTime.time;
    }

    bool totalPlaytimeSet = false;
    [Server]
    void SetTotalPlayTimeAtStart(ulong playtime)
    {
        if(totalPlaytimeSet)
        {
            Debug.LogWarning("playerPlayTimeAtStart was already set");
            return;
        }
        totalPlaytimeSet = true;
        playerPlayTimeAtStart = playtime;
    }

    ulong GetPlayTime()
    {
        return GetPlayTimeSinceStart() + playerPlayTimeAtStart;
    }

    ulong GetPlayTimeSinceStart()
    {
        return (ulong)(NetworkTime.time - playerStartPlayingTime);
    }

    private void Start()
    {
        //Retrieve coins and bucks amount from server when spawned
        if(isLocalPlayer)
        {
            CmdGetFishCoins();
            CmdGetFishBucks();
            CmdGetFriendList();
            CmdGetFriendRequestList();
        }
    }
}

using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
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
    
    [SyncVar(hook = nameof(SelectedBaitChanged))]
    int selectedBaitId;

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
        //TODO: ask the database what the new rod is to see if the write has gone correctly
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

    [TargetRpc]
    private void RpcChangeRodStats(ItemInstance rod, int amount) {
        //rodObject here is a nely created rod, we need to get the rod reference from the player inventory.
        ItemInstance inventoryRod = inventory.GetRodByUuid(rod.uuid);
        inventoryRod.GetState<DurabilityState>().remaining += amount;
    }

    //TODO: make a function for in the syncManager, this should do the DB calls.
    [Server]
    public void ChangeRodQuality(ItemInstance rod, int amount)
    {
        RodBehaviour rodBehaviour = rod.def.GetBehaviour<RodBehaviour>();
        DurabilityBehaviour durabilityBehaviour = rod.def.GetBehaviour<DurabilityBehaviour>();
        if (rodBehaviour == null)
        {
            Debug.Log("rod has no rodBehaviour");
            return;
        }
        // Bait has infinite durability or can't be removed from inventory
        if (durabilityBehaviour == null || rod.def.IsStatic)
        {
            return;
        }
        rod.GetState<DurabilityState>().remaining += amount;
        RpcChangeRodStats(rod, amount);

        if (rod.GetState<DurabilityState>().remaining <= 0)
        {
            //Remove item from database and select new one
            //Item with id -1, this should be the standard beginners rod
            ItemInstance rodItem = inventory.GetRodByDefinitionId(0);
            if (rodItem == null)
            {
                Debug.LogWarning("rodItem returned was null, TODO: disconnect player");
                return;
            }
            //TODO: only change to the new rod when the rod goes out of the water.
            SelectNewRod(rodItem, false);
            playerDataManager.DestroyItem(rod);
        }
        else
        {
            if (amount < 0)
            {
                DatabaseCommunications.AddOrUpdateItem(rod, GetUuid());
            }
            else
            {
                Debug.LogWarning("There's no function avalable to add extra quality to the rod");
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

    [Server]
    public void ChangeBaitQuality(ItemInstance bait, int amount)
    {
        BaitBehaviour baitBehaviour = bait.def.GetBehaviour<BaitBehaviour>();
        DurabilityBehaviour durabilityBehaviour = bait.def.GetBehaviour<DurabilityBehaviour>();
        if (baitBehaviour == null)
        {
            Debug.Log("bait has no baitBehaviour");
            return;
        }
        // Bait has infinite durability or can't be removed from inventory
        if (durabilityBehaviour == null || bait.def.IsStatic)
        {
            return;
        }
        bait.GetState<DurabilityState>().remaining += amount;
        RpcChangeBaitStats(bait, amount);

        if (bait.GetState<DurabilityState>().remaining <= 0)
        {
            //Remove item from database and select new one
            //Item with id 1000, this should be the standard beginners rod
            ItemInstance baitItem = inventory.GetBaitByDefinitionId(1000);
            if (baitItem == null)
            {
                Debug.LogWarning("newBait returned was null, TODO: disconnect player");
                return;
            }
            //TODO: only change to the new rod when the rod goes out of the water.
            SelectNewBait(baitItem, false);
            playerDataManager.DestroyItem(bait);
        }
        else
        {
            if (amount < 0)
            {
                DatabaseCommunications.AddOrUpdateItem(bait, GetUuid());
            }
            else
            {
                Debug.LogWarning("There's no function avalable to add extra quality to the rod");
            }
        }
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
        } catch (Exception e)
        {
            Debug.LogWarning(e);
            return false;
        }
        return true;
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

using Mirror;
using System;
using System.Collections;
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
    int selectedRodUid;
    [SyncVar(hook = nameof(SelectedBaitChanged))]
    int selectedBaitId;

    rodObject selectedRod;
    baitObject selectedBait;

    public event Action selectedRodChanged;
    public event Action selectedBaitChanged;


    [SerializeField]
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
    public void SelectNewRod(rodObject newRod, bool fromDatabase)
    {
        if (!fromDatabase)
        {
            DatabaseCommunications.SelectOtherItem(newRod, GetUuidAsString());
        }
        //TODO: ask the database what the new rod is to see if the write has succeeded
        selectedRod = newRod;
        selectedRodUid = newRod.uid;
    }

    [Command]
    public void CmdSelectNewRod(rodObject newRod)
    {
        if (selectedRod != null)
        {
            if (selectedRod.uid == newRod.uid)
            {
                return;
            }
        }

        //The newRod variable might be a newly crafted rod, so get it as a reference from the inventory, then the inventory get's updated when this specific item is updated
        rodObject rodInventoryReference;
        if (newRod.stackable)
        {
            rodInventoryReference = inventory.GetRodByUID(newRod.uid);
        }
        else
        {
            //Probably never needed, but catch it and warn just in case, why do rods have the stackable flag at all???
            Debug.LogWarning("Why does this rod have the stackable flag set???");
            return;
        }

        if (rodInventoryReference == null)
        {
            Debug.Log("rodInventoryReference is null");
            return;
        }

        SelectNewRod(rodInventoryReference, false);
    }

    [Server]
    public void SelectNewBait(baitObject newBait, bool fromDatabase)
    {
        if (!fromDatabase)
        {
            DatabaseCommunications.SelectOtherItem(newBait, GetUuidAsString());
        }
        //TODO: ask the database what the new rod is to see if the write has gone correctly
        selectedBait = newBait;
        selectedBaitId = newBait.id;
    }

    [Command]
    public void CmdSelectNewBait(baitObject newBait)
    {
        if (selectedBait != null)
        {
            if (selectedBait.id == newBait.id)
            {
                return;
            }
        }

        //The newBait variable might be a newly crafted bait, so get it as a reference from the inventory, then the inventory get's updated when this specific item is updated
        baitObject baitInventoryReference;
        if (newBait.stackable)
        {
            baitInventoryReference = inventory.GetBaitByID(newBait.id);
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

    void SelectedRodChanged(int _, int newRodUid)
    {
        if (!isLocalPlayer)
        {
            return;
        }
        StartCoroutine(SelectedRodChangedCoroutine(newRodUid));
    }

    //Inventory might not yet be synced so it may not yet have the item available in the inventory
    IEnumerator SelectedRodChangedCoroutine(int newRodUid) {
        rodObject newRod;
        while ((newRod = inventory.GetRodByUID(newRodUid)) == null)
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
        baitObject newBait;
        while ((newBait = inventory.GetBaitByID(newBaitId))  == null)
        {
            yield return new WaitForSeconds(0.05f);
        }
        selectedBait = newBait;
        selectedBaitChanged?.Invoke();
    }

    public rodObject GetSelectedRod()
    {
        return selectedRod;
    }

    public baitObject GetSelectedBait()
    {
        return selectedBait;
    }

    [TargetRpc]
    private void RpcChangeRodStats(rodObject rod, int amount) {
        //rodObject here is a nely created rod, we need to get the rod reference from the player inventory.
        rodObject inventoryRod = inventory.GetRodByUID(rod.uid);
        inventoryRod.throwIns += amount;
    }

    //TODO: make a function for in the syncManager, this should do the DB calls.
    [Server]
    public void ChangeRodQuality(rodObject rod, int amount)
    {
        if (rod.durabilityIsInfinite || rod.uid < 0)
        {
            return;
        }
        rod.throwIns += amount;
        RpcChangeRodStats(rod, amount);

        if (rod.throwIns <= 0)
        {
            //Remove item from database and select new one
            //Item with id -1, this should be the standard beginners rod
            ItemObject rodItem = inventory.GetRodByID(-1);
            rodObject newRod = null;
            if (rodItem != null)
            {
                newRod = rodItem as rodObject;
            }
            if (newRod == null)
            {
                Debug.LogWarning("newRod returned was null");
                return;
            }
            //TODO: only change to the new rod when the rod goes out of the water.
            SelectNewRod(newRod, false);
            playerDataManager.DestroyItem(rod);
        }
        else
        {
            if (amount < 0)
            {
                DatabaseCommunications.ReduceItem(rod, Mathf.Abs(amount), GetUuidAsString());
            }
            else
            {
                Debug.LogWarning("There's no function avalable to add extra quality to the rod");
            }
        }
    }

    [TargetRpc]
    private void RpcChangeBaitStats(baitObject bait, int amount)
    {
        //baitObject here is a nely created bait, we need to get the bait reference from the player inventory.
        baitObject inventoryBait = inventory.GetBaitByID(bait.id);
        inventoryBait.throwIns += amount;
    }

    [Server]
    public void ChangeBaitQuality(baitObject bait, int amount)
    {
        if (bait.durabilityIsInfinite || bait.id < 0)
        {
            return;
        }
        bait.throwIns += amount;
        RpcChangeBaitStats(bait, amount);

        if (bait.throwIns <= 0)
        {
            //Remove item from database and select new one
            //Item with id -1, this should be the standard beginners rod
            ItemObject baitItem = inventory.GetBaitByID(-1);
            baitObject newBait = null;
            if (baitItem != null)
            {
                newBait = baitItem as baitObject;
            }
            if (newBait == null)
            {
                Debug.LogWarning("newBait returned was null");
                return;
            }
            //TODO: only change to the new rod when the rod goes out of the water.
            SelectNewBait(newBait, false);
            playerDataManager.DestroyItem(bait);
        }
        else
        {
            if (amount < 0)
            {
                DatabaseCommunications.ReduceItem(bait, Mathf.Abs(amount), GetUuidAsString());
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
    public void SetLastitemUID(int num)
    {
        lastItemUID = num;
    }

    [Server]
    public void IncreaseLastitemUID(int amount)
    {
        lastItemUID += amount;
    }

    public int GetLastitemUID()
    {
        return lastItemUID;
    }

    [Server]
    public bool ParsePlayerData(string jsonPlayerData)
    {
        try
        {
            UserData playerData = JsonUtility.FromJson<UserData>(jsonPlayerData);
            inventory.SaveInventory(playerData);
            fishdexFishes.SaveFishStats(playerData);

            SetUuid(GuidFromBytes(playerData.uuid));
            SetFishCoins(playerData.stats.coins);
            SetFishBucks(playerData.stats.bucks);
            SetXp(playerData.stats.xp);
            SetStartPlayTime();
            SetTotalPlayTimeAtStart(playerData.stats.playtime);
            SetLastitemUID(playerData.lastItemUID);
            SetShowInventory(playerData.showInv);
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
        }
    }
}
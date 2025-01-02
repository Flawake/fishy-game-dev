using Mirror;
using System;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    //Not all variables that should be synced between the client and the player are a syncVar
    //For example: no player except yourself should know how much fishcoins or fishbucks you have.

    //Variables that are not synced between ALL players
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    int availableFishCoins;
    [SerializeField]
    int availableFishBucks;

    //Variables that are synced between ALL players
    [SyncVar, SerializeField]
    string playerName;
    [SyncVar, SerializeField]
    int playerXp;
    [SyncVar, SerializeField]
    bool showInventory;
    [SyncVar, SerializeField]
    int lastItemUID;
    [SyncVar, SerializeField]
    Color32 chatColor;

    public event Action CoinsAmountChanged;
    public event Action BucksAmountChanged;

    [SerializeField]
    Guid uuid;
    bool uuidSet = false;
    [Server]
    public void SetUuid(Guid playerUuid)
    {
        if(uuidSet)
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
            SetUuid(new Guid(playerData.uuid));
            SetFishCoins(playerData.stats.coins);
            SetFishBucks(playerData.stats.bucks);
            SetXp(playerData.stats.xp);
            SetLastitemUID(playerData.lastItemUID);
            SetShowInventory(playerData.showInv);
        } catch (Exception e)
        {
            Debug.LogWarning(e);
            return false;
        }
        return true;
    }

    [Server]
    public void SetRandomColor()
    {
        switch(UnityEngine.Random.Range(0, 6))
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
        playerXp += xp;
    }

    public int GetXp()
    {
        return playerXp;
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
        if(isServer)
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
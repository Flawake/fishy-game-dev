using System.Collections;
using UnityEngine;
using Mirror;
using System;

public class FishingManager : NetworkBehaviour
{
    //TODO: fishao used bezier curves for it's line, I might need to do the same.
    //com.ax3.display.rod
    //com.ax3.fishao.locations.entries.FieldChat
    //rod radius: 200, 300, 500, small, normal, big
    //Keep this order of syncvars, the dirty bits rely on it.
    [SyncVar(hook = nameof(SelectedRodChanged))]
    public rodObject selectedRod;                       //bit 1(1 << 0)
    [SyncVar(hook = nameof(SelectedBaitChanged))]
    public baitObject selectedBait;                     //bit2 (1 << 1)

    //script classes
    [SerializeField] DrawLineOthers networkedPlayerLine;
    [SerializeField] playerController player;
    [SerializeField] FishingLine localPlayerLine;
    [SerializeField] fishFight fishFight;                   //change
    [SerializeField] caughtDialogData caughtData;           //change
    [SerializeField] ItemManager itemManager;
    [SerializeField] DatabaseCommunications database;
    [SerializeField] PlayerInventory inventory;
    [SerializeField] RodAnimator rodAnimator;

    //gameObjects
    [SerializeField] Camera playerCamera;
    [SerializeField] Collider2D playerCollider;
    [SerializeField] GameObject fishFightDialog;            //change
    [SerializeField] GameObject caughtDialog;               //change

    //Variables
    public bool isFishing = false;
    public bool fightStarted = false;

    bool fishGenerated = false;

    [SyncVar]
    CurrentFish currentFish;
    [SyncVar]
    System.Diagnostics.Stopwatch startedFishFightTime = new System.Diagnostics.Stopwatch();

    System.Diagnostics.Stopwatch startedFishingTime = new System.Diagnostics.Stopwatch();

    //count in ms, since this is more precise
    int minFishingTimeMs;

    //Time till the fishing result can be send to the player
    int timeTillResultsSeconds = int.MaxValue;

    public bool nearWater = false;

    public event Action selectedRodChanged;
    public event Action selectedBaitChanged;

    public enum EndFishingReason
    {
        caughtFish,
        lostFish,
        noFishGenerated,
        stoppedFishing,
    }

    private void Start()
    {
        if(!isLocalPlayer) {
            return; 
        }
        //TODO: don't make this code dependent on string paths
        fishFightDialog = GameObject.Find("Player(Clone)/Canvas(Clone)/Fish fight dialog");
        caughtDialog = GameObject.Find("Player(Clone)/Canvas(Clone)/Fish caught dialog");
        fishFight = fishFightDialog.GetComponent<fishFight>();
        caughtData = caughtDialog.GetComponent<caughtDialogData>();
        if( fishFightDialog == null || caughtDialog == null)
        {
            Debug.LogError("Could not find a canvas dialog");
        }
    }

    private void Update()
    {
        if (isServer) {
            ProgressFishing();
        }
    }

    void SelectedRodChanged(rodObject oldRod, rodObject newRod)
    {
        if (oldRod != null)
        {
            if (oldRod.uid == newRod.uid)
            {
                //return, this action is used to update the bait image. But this is not needed since the bait did not change
                inventory.GetRodByUID(newRod.uid).throwIns = newRod.throwIns;
                return;
            }
        }
        selectedRodChanged?.Invoke();
    }

    void SelectedBaitChanged(baitObject oldBait, baitObject newBait)
    {
        if(oldBait != null)
        {
            if(oldBait.id == newBait.id)
            {
                //return, this action is used to update the bait image. But this is not needed since the bait did not change
                inventory.GetBaitByID(newBait.id).throwIns = newBait.throwIns;
                return;
            }
        }
        selectedBaitChanged?.Invoke();
    }

    /// <summary>
    /// All functions under here are being executed by the client.
    /// </summary>

    [Client]
    public bool ProcessFishing(Vector2 clickedPos)
    {
        if (!IsFishingSpot(clickedPos))
            return false;

        if (!isFishing && player.GetObjectsPreventingFishing() == 0)
        {
            StartFishing(clickedPos);
            isFishing = true;
        }
        else
        {
            EndFishing(EndFishingReason.stoppedFishing);
            isFishing = false;
        }
        return true;
    }

    //This function checks if the position clicked is a fishing spot and if that fishing spot is valid.
    //This is first being done on the client and later on the server.
    //This is to offload the server for clicks that were not meant to be for fishing.

    bool IsFishingSpot(Vector2 clickedPos, out RaycastHit2D water)
    {
        water = new RaycastHit2D();

        float rodThrowDistance = 10f;

        if (Vector2.Distance(transform.position, clickedPos) > rodThrowDistance)
        {
            return false;
        }

        int waterLayer = LayerMask.GetMask("Water");

        RaycastHit2D hit = Physics2D.Raycast(clickedPos, Vector2.zero, float.MaxValue, waterLayer);

        //Needed in the future to test if there are no objects in the way of the line.
        //RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(clickedPos.x - transform.position.x, clickedPos.y - transform.position.y), Vector2.Distance(clickedPos, transform.position), waterLayer);
        if (!hit)
        {
            return false;
        }

        if (!hit.collider.gameObject.GetComponent<playersNearWater>().playersCloseToWater.Contains(this.GetComponent<NetworkIdentity>().netId))
        {
            return false;
        }

        water = hit;
        return true;
    }

    bool IsFishingSpot(Vector2 clickedPos)
    {
        return IsFishingSpot(clickedPos, out _);
    }

    [Client]
    void StartFishing(Vector2 placeToThrow)
    {
        //Throw the line on the localplayer, the position needs to be validated on the
        //server before sent to clients and before a fish is being generated.
        localPlayerLine.StartFishing(placeToThrow);
        Vector2 throwDirection = (placeToThrow - (Vector2)player.transform.position).normalized;
        rodAnimator.ThrowRod(throwDirection);
        player.SetPlayerAnimationForDirection(throwDirection);
        CmdStartFishing(placeToThrow);
        Debug.Log($"bait left: {selectedBait.throwIns}");
    }

    [Client]
    public void EndFishing(EndFishingReason reason)
    {
        StartCoroutine(EndFight());
        localPlayerLine.EndFishing();
        CmdEndFishing();
        if(reason == EndFishingReason.caughtFish)
        {
            CmdRegisterCaughtFish();
        }
        isFishing = false;
    }

    [ClientRpc]
    //Function is called from the server to the client to stop fishing, happens if no fish bait the hook.
    public void RpcEndFishing(EndFishingReason reason)
    {
        StartCoroutine(EndFight());
        localPlayerLine.EndFishing();
        if (reason == EndFishingReason.caughtFish)
        {
            CmdRegisterCaughtFish();
        }
        isFishing = false;
    }

    [ClientRpc]
    void StartFight(CurrentFish currentFish, int _minFishingTimeMs)
    {
        minFishingTimeMs = _minFishingTimeMs;
        if (!isLocalPlayer)
        {
            return;
        }
        fishFightDialog.SetActive(true);
        fishFight.StartFight(currentFish, _minFishingTimeMs / 1000);
    }

    [Client]
    IEnumerator EndFight()
    {
        if (!isLocalPlayer || !fishFightDialog.activeInHierarchy)
        {
            yield break;
        }
        fishFight.EndFight();
        yield return new WaitForSeconds(0.3f);
        fishFightDialog.SetActive(false);
    }

    [TargetRpc]
    void TargetShowCaughtDialog()
    {
        caughtDialog.SetActive(true);
        caughtData.setData(currentFish);
    }

    /// <summary>
    /// All functions under here are being executed by the server.
    /// </summary>
    /// 
    [Server]
    public void SelectNewRod(rodObject newRod, bool fromDatabase)
    {
        if (!fromDatabase)
        {
            database.SelectOtherItem(newRod);
        }
        //TODO: ask the database what the new rod is to see if the write has succeed
        selectedRod = newRod;
    }

    [Command]
    public void CmdSelectNewRod(rodObject newRod, bool fromDatabase)
    {
        if(selectedRod != null)
        {
            if (selectedRod.uid == newRod.uid)
            {
                return;
            }
        }

        //The newBait variable might be a newly crafted bait, so get it as a reference from the inventory, then the inventory get's updated when this specific item is updated
        rodObject rodInventoryReference;
        if (newRod.stackable)
        {
            rodInventoryReference = inventory.GetRodByUID(newRod.uid);
        }
        else
        {
            //Probably never needed, but catch it and warn just in case
            Debug.LogWarning("Can't get bait that is not stackable, should be got with the uid");
            return;
        }

        if (rodInventoryReference == null)
        {
            Debug.Log("baitInventoryReference is null");
            return;
        }

        SelectNewRod(rodInventoryReference, fromDatabase);
    }

    [Server]
    public void SelectNewBait(baitObject newBait, bool fromDatabase)
    {
        if (!fromDatabase)
        {
            database.SelectOtherItem(newBait);
        }
        //TODO: ask the database what the new rod is to see if the write has gone correctly
        selectedBait = newBait;
    }

    [Command]
    public void CmdSelectNewBait(baitObject newBait, bool fromDatabase)
    {
        if(selectedBait != null)
        {
            if (selectedBait.uid == newBait.uid)
            {
                return;
            }
        }

        //The newBait variable might be a newly crafted bait, so get it as a reference from the inventory, then the inventory get's updated when this specific item is updated
        baitObject baitInventoryReference;
        if(newBait.stackable)
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

        SelectNewBait(baitInventoryReference, fromDatabase);
    }

    [Command]
    void CmdRegisterCaughtFish() {
        if (startedFishFightTime.ElapsedMilliseconds < minFishingTimeMs)
        {
            Debug.LogWarning("The fishing period was too short. Should be " + minFishingTimeMs + " ms, but was " + startedFishFightTime.ElapsedMilliseconds);
            return;
        }
        else
        {
            TargetShowCaughtDialog();
            FishObject fishObject = ItemObjectGenerator.FishObjectFromMinimal(currentFish.id, 1);
            itemManager.AddItem(fishObject, currentFish);
        }
    }

    [Command]
    void CmdStartFishing(Vector2 placeToThrow)
    {

        if (!IsFishingSpot(placeToThrow, out RaycastHit2D water))
        {
            Debug.LogError("The fishing place is somehow not valid");
            return;
        }

        ReduceSelectedRodQuality(selectedRod);
        ReduceSelectedBaitQuality(selectedBait);

        float xFactor = Mathf.Pow((transform.position.x - placeToThrow.x), 2);
        float yFactor = Mathf.Pow((transform.position.y - placeToThrow.y), 2);
        networkedPlayerLine.lineSegLength = Mathf.Sqrt(xFactor + yFactor) / networkedPlayerLine.lineSegmentsAmount;
        networkedPlayerLine.placeToThrow = placeToThrow;
        networkedPlayerLine.isFishing = true;
        isFishing = true;

        player.RpcSetPlayerAnimationForDirection(placeToThrow - (Vector2)player.transform.position);

        if (water)
        {
            spawnableFishes spawnable = water.collider.gameObject.GetComponent<spawnableFishes>();

            (currentFish, fishGenerated) = spawnable.GenerateFish();

            timeTillResultsSeconds = UnityEngine.Random.Range(5, 11);

            startedFishingTime.Reset();
            startedFishingTime.Start();
        }
        else
        {
            Debug.LogError("Water should never be able to be null, it happened now tough");
        }
    }

    //Don't do a Enumerator with yield return new waitforseconds(), we can't handle the player stopping the fishing progress that way.
    void ProgressFishing() 
    {
        //Only run the function when the player is fishing AND the fight is has already started
        if (!isFishing || fightStarted) {
            return;
        }

        //Check if the rod is in the water for long enough
        if(startedFishingTime.ElapsedMilliseconds < timeTillResultsSeconds * 1000)
        {
            return;
        }

        if (!fishGenerated)
        {
            Debug.Log("No fish could be generated at FishingManager.CmdStartFishing");

            RpcEndFishing(EndFishingReason.noFishGenerated);
            ServerEndFishing();
            return;
        }

        minFishingTimeMs = UnityEngine.Random.Range(6, 11) * 1000;
        startedFishFightTime.Reset();
        startedFishFightTime.Start();
        StartFight(currentFish, minFishingTimeMs);
        fightStarted = true;
    }

    [Command]
    //Tell the server that the fishing should be stopped
    void CmdEndFishing()
    {
        ServerEndFishing();
    }

    [Server]
    void ServerEndFishing() {
        isFishing = false;
        fightStarted = false;
        networkedPlayerLine.isFishing = false;
        networkedPlayerLine.RpcEndedFishing();
    }

    [Server]
    private void ReduceSelectedRodQuality(rodObject rod)
    {
        if(rod.durabilityIsInfinite || rod.uid < 0)
        {
            return;
        }
        if(rod.throwIns == 1)
        {
            //Remove item from database and select new one
            //Item with id 0, this should be the standard beginners rod
            ItemObject rodItem = inventory.GetRodByID(0);
            rodObject newRod = null;
            if(rodItem != null)
            {
                newRod = rodItem as rodObject;
            }
            if(newRod == null)
            {
                Debug.LogWarning("newRod returned was null");
                return;
            }
            //TODO: only change to the new rod when the rod goes out of the water.
            SelectNewRod(newRod, false);
            itemManager.DestroyItem(rod);
        }
        else
        {
            selectedRod.throwIns -= 1;
            SetSyncVarDirtyBit(1 << 0); //selected rod is only updated not changed, so we need to force a update manually
            database.ReduceItem(rod, 1);
        }
    }

    [Server]
    private void ReduceSelectedBaitQuality(baitObject bait)
    {
        if (bait.durabilityIsInfinite || bait.uid < 0)
        {
            return;
        }
        if (bait.throwIns == 1)
        {
            //Remove item from database and select new one
            //Item with id 0, this should be the standard beginners rod
            ItemObject rodItem = inventory.GetBaitByID(0);
            baitObject newBait = null;
            if (rodItem != null)
            {
                newBait = rodItem as baitObject;
            }
            if (newBait == null)
            {
                Debug.LogWarning("newRod returned was null");
                return;
            }
            //TODO: only change to the new bait when the rod goes out of the water.
            SelectNewBait(newBait, false);
            itemManager.DestroyItem(bait);
        }
        else
        {
            selectedBait.throwIns -= 1;
            SetSyncVarDirtyBit(1 << 1); //selected bait is only updated not changed, so we need to force a update manually
            database.ReduceItem(bait, 1);
        }
    }
}

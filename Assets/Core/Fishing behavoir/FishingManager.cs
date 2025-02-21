using System.Collections;
using UnityEngine;
using Mirror;

public class FishingManager : NetworkBehaviour
{
    public struct syncedFishingPos
    {
        public Vector2 fishingPos;
        public bool stardedFishing;
    }

    //script classes
    [SerializeField] playerController player;
    [SerializeField] FishingLine fishingLine;
    [SerializeField] fishFight fishFight;                   //change
    [SerializeField] caughtDialogData caughtData;           //change
    [SerializeField] PlayerDataSyncManager playerDataManager;
    [SerializeField] PlayerInventory inventory;
    [SerializeField] PlayerData playerData;
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

    [SyncVar(hook = nameof(SyncvarThrowRod))]
    syncedFishingPos syncedPlaceToThrow;

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
        ThrowRod(placeToThrow);

        CmdStartFishing(placeToThrow);
    }

    [Client]
    public void EndFishing(EndFishingReason reason)
    {
        StartCoroutine(EndFight());
        fishingLine.EndFishing();
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
        fishingLine.EndFishing();
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
            playerDataManager.AddItem(fishObject, currentFish, true);
            playerDataManager.AddXP(currentFish.xp);
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

        playerData.ChangeRodQuality(playerData.GetSelectedRod(), -1);
        playerData.ChangeBaitQuality(playerData.GetSelectedBait(), -1);

        syncedFishingPos pos;
        pos.stardedFishing = true;
        pos.fishingPos = placeToThrow;
        syncedPlaceToThrow = pos;

        isFishing = true;

        if (water)
        {
            spawnableFishes spawnable = water.collider.gameObject.GetComponent<spawnableFishes>();

            (currentFish, fishGenerated) = spawnable.GenerateFish(playerData.GetSelectedBait().baitType);

            timeTillResultsSeconds = UnityEngine.Random.Range(5, 11);

            startedFishingTime.Reset();
            startedFishingTime.Start();
        }
        else
        {
            Debug.LogError("Water should never be able to be null, it happened now tough");
        }
    }
    void SyncvarThrowRod(syncedFishingPos _, syncedFishingPos newVal) {
        if (isLocalPlayer) {
            return;
        }

        if (newVal.stardedFishing)
        {
            ThrowRod(newVal.fishingPos);
        }
        else {
            fishingLine.EndFishing();
        }
    }

    void ThrowRod(Vector2 placeToThrow)
    {
        //Initialize the fishingline, the play the animation to throw the rod. The rod animation calls a function to actually start throwing the fishing line
        fishingLine.InitThrowFishingLine(placeToThrow);
        Vector2 throwDirection = (placeToThrow - (Vector2)player.transform.position).normalized;

        rodAnimator.ThrowRod(throwDirection);
        player.SetPlayerAnimationForDirection(throwDirection);
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
        fishingLine.RpcEndedFishing();

        syncedFishingPos pos;
        pos.stardedFishing = false;
        pos.fishingPos = Vector2.zero;
        syncedPlaceToThrow = pos;
    }
}

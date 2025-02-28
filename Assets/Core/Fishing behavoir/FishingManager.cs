using System.Collections;
using UnityEngine;
using Mirror;

public class FishingManager : NetworkBehaviour
{
    public struct SyncedFishingPos
    {
        public Vector2 fishingPos;
        public bool stardedFishing;
    }

    //script classes
    [SerializeField] PlayerController player;
    [SerializeField] FishingLine fishingLine;
    [SerializeField] PlayerDataSyncManager playerDataManager;
    [SerializeField] PlayerInventory inventory;
    [SerializeField] PlayerData playerData;
    [SerializeField] RodAnimator rodAnimator;
    FishFight fishFight;
    CaughtDialogData caughtData;

    //gameObjects
    [SerializeField] Camera playerCamera;
    [SerializeField] Collider2D playerCollider;
    GameObject fishFightDialog;
    GameObject caughtDialog;

    //Variables
    public bool isFishing = false;
    public bool fightStarted = false;

    bool fishGenerated = false;

    [SyncVar(hook = nameof(SyncvarThrowRod))]
    SyncedFishingPos syncedPlaceToThrow;

    [SyncVar]
    CurrentFish currentFish;

    [SyncVar]
    float elapsedFishingTime = 0;
    float elapsedFishFightTime = 0;

    //count in ms, since this is more precise
    int minFishingTimeSeconds;

    //Time till the fishing result can be send to the player
    float timeTillResultsSeconds = float.MaxValue;

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
        fishFight = fishFightDialog.GetComponent<FishFight>();
        caughtData = caughtDialog.GetComponent<CaughtDialogData>();
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

        if (!hit.collider.gameObject.GetComponent<PlayersNearWater>().playersCloseToWater.Contains(this.GetComponent<NetworkIdentity>().netId))
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
    void RpcStartFight(CurrentFish currentFish, int minFishingTime)
    {
        minFishingTimeSeconds = minFishingTime;
        if (!isLocalPlayer)
        {
            return;
        }
        fishFightDialog.SetActive(true);
        fishFight.StartFight(currentFish, minFishingTime);
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
        caughtData.SetData(currentFish);
    }

    [Command]
    void CmdRegisterCaughtFish() {
        if (elapsedFishFightTime < minFishingTimeSeconds)
        {
            Debug.LogWarning("The fishing period was too short. Should be " + minFishingTimeSeconds + " s, but was " + elapsedFishFightTime);
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

        SyncedFishingPos pos;
        pos.stardedFishing = true;
        pos.fishingPos = placeToThrow;
        syncedPlaceToThrow = pos;

        isFishing = true;

        if (water)
        {
            SpawnableFishes spawnable = water.collider.gameObject.GetComponent<SpawnableFishes>();

            (currentFish, fishGenerated) = spawnable.GenerateFish(playerData.GetSelectedBait().baitType);

            timeTillResultsSeconds = Random.Range(5, 11);

            elapsedFishingTime = 0;
        }
        else
        {
            Debug.LogError("Water should never be able to be null, it happened now tough");
        }
    }
    void SyncvarThrowRod(SyncedFishingPos _, SyncedFishingPos newVal) {
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
        //Only run the function when the player is fishing.
        if (!isFishing) {
            return;
        }

        if(fightStarted)
        {
            elapsedFishFightTime += Time.deltaTime;
            return;
        }

        elapsedFishingTime += Time.deltaTime;

        //Check if the rod is in the water for long enough
        if(elapsedFishingTime < timeTillResultsSeconds)
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

        minFishingTimeSeconds = Random.Range(6, 11);
        elapsedFishFightTime = 0;
        RpcStartFight(currentFish, minFishingTimeSeconds);
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

        SyncedFishingPos pos;
        pos.stardedFishing = false;
        pos.fishingPos = Vector2.zero;
        syncedPlaceToThrow = pos;
    }
}

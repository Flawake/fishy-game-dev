using System.Collections;
using GlobalCompetitionSystem;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;
using ItemSystem;

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
    [SerializeField] Collider2D fishCollider;
    FishFight fishFight;
    CaughtDialogData caughtData;

    //gameObjects
    [SerializeField] Camera playerCamera;
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
    float fightStartTime = 0;

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

        // Player clicked on a fishing spot, but is currently not allowed to fish because of an active object preventing it.
        if(player.GetObjectsPreventingFishing() != 0)
        {
            return true;
        }

        if (playerData.GetSelectedRod() == null)
        {
            InventoryUIManager inventoryManager = GetComponentInChildren<InventoryUIManager>();
            inventoryManager.ToggleBackPack(InventoryUIManager.ItemFiler.Rods);
            Debug.Log("Open inventory rods page...");
            return true;
        }
        
        if (playerData.GetSelectedBait() == null)
        {
            InventoryUIManager inventoryManager = GetComponentInChildren<InventoryUIManager>();
            inventoryManager.ToggleBackPack(InventoryUIManager.ItemFiler.Baits);
            Debug.Log("Open inventory baits page...");
            return true;
        }

        if (!isFishing)
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

    Vector2 collisionPoint = Vector2.zero;
    Vector2 obstaclePoint = Vector2.zero;
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(collisionPoint, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(obstaclePoint, 0.1f);
    }

    //This function checks if the position clicked is a fishing spot and if that fishing spot is valid.
    //This is first being done on the client and later on the server.
    bool IsFishingSpot(Vector2 clickedPos, out RaycastHit2D water)
    {
        water = new RaycastHit2D();

        float rodThrowDistance = 2.3f;

        if (Vector2.Distance(transform.position, clickedPos) > rodThrowDistance)
        {
            DebugFishingSpot("No fishing spot, distance was too big");
            return false;
        }

        int waterLayer = LayerMask.GetMask("Water");

        RaycastHit2D hit = Physics2D.Raycast(clickedPos, Vector2.zero, float.MaxValue, waterLayer);

        if (!hit)
        {
            DebugFishingSpot("No hit on the water");
            return false;
        }

        // also make sure there are no objects between the player and the water.
        int obstacleLayer = ~LayerMask.GetMask("Water", "Player", "Ignore Raycast");
        // add one to make sure there are no float errors, don't know if it is neccessary tough
        CompositeCollider2D coll = SceneObjectCache.GetWorldCollider(gameObject.scene);
        CompositeCollider2D.GeometryType geoType = coll.geometryType;
        coll.geometryType = CompositeCollider2D.GeometryType.Outlines;
        RaycastHit2D[] hits = Physics2D.RaycastAll(clickedPos, new Vector2(transform.position.x - clickedPos.x, transform.position.y - clickedPos.y), Vector2.Distance(clickedPos, transform.position), obstacleLayer);
        coll.geometryType = geoType;
        foreach (RaycastHit2D obstacle in hits)
        {
            if (Vector2.Distance(obstacle.point, transform.position) > 0.6f)
            {
                obstaclePoint = obstacle.point;
                DebugFishingSpot($"Obstacle in between the player and the fishingplace {obstacle.point}");
                return false;
            }
        }

        // Same raycasthit, but from player to throwdirection to make sure that the collision point is inside the player's fish collider
        hits = Physics2D.RaycastAll(transform.position, new Vector2(clickedPos.x - transform.position.x, clickedPos.y - transform.position.y), rodThrowDistance + 1, obstacleLayer);
        if(hits.Length == 0) {
            DebugFishingSpot("Why are there 0 hits?");
            return false;
        }
        collisionPoint = hits[0].point;
        if(!fishCollider.OverlapPoint(hits[0].point)) {
            DebugFishingSpot("The fishingpoint git is not inside the player fishing collider");
            return false;
        }
        // We are sure that the click was inside a water collider, but the click can still be on an object that is inside the water. We also need to check for that.
        CompositeCollider2D walkable = SceneObjectCache.GetWorldCollider(gameObject.scene);
        // is there a better way then to cycle the GeometryType???
        if(isClient) {
            //walkable.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }
        bool doesOverlap = walkable.OverlapPoint(clickedPos);
        if(isClient) {
            //walkable.geometryType = CompositeCollider2D.GeometryType.Outlines;
        }
        if(!doesOverlap) {
            DebugFishingSpot("The fishingspot is on a walkable area");
            return false;
        }

        if (!hit.collider.gameObject.GetComponent<PlayersNearWater>().GetPlayersNearPuddle().Contains(this.GetComponent<NetworkIdentity>().netId))
        {
            DebugFishingSpot("The player is not even near the water");
            return false;
        }

        water = hit;
        return true;
    }

    void DebugFishingSpot(string message) {
#if UNITY_EDITOR
#if false
        Debug.LogWarning(message);
#endif
#endif
    }

    [Client]
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
        if (Time.time - fightStartTime < minFishingTimeSeconds)
        {
            Debug.LogWarning("The fishing period was too short. Should be " + minFishingTimeSeconds + " s, but was " + (Time.time - fightStartTime));
        }
        else
        {
            TargetShowCaughtDialog();
            CompetitionManager.AddToRunningCompetition(currentFish, playerData);
            ItemDefinition fishDef = ItemRegistry.Get(currentFish.id);
            ItemInstance fishInstance = new ItemInstance(fishDef);
            playerDataManager.AddItem(fishInstance, currentFish, true);
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
        playerData.UseBait(playerData.GetSelectedBait());

        SyncedFishingPos pos;
        pos.stardedFishing = true;
        pos.fishingPos = placeToThrow;
        syncedPlaceToThrow = pos;

        isFishing = true;

        if (water)
        {
            SpawnableFishes spawnable = water.collider.gameObject.GetComponent<SpawnableFishes>();
            FishSpots fishSpots = water.collider.gameObject.GetComponent<FishSpots>();
            
            if (spawnable == null || fishSpots == null)
            {
                Debug.LogError("Missing required components on water object.");
                return;
            }
            
            (currentFish, fishGenerated) = (new CurrentFish(), false);
            
            if (fishSpots.ShouldGeneratefish(placeToThrow))
            {
                // Get luck multiplier from player data
                float luckMultiplier = playerData.GetLuckMultiplier();
                
                (currentFish, fishGenerated) = spawnable.GenerateFish(playerData.GetSelectedBait().def.GetBehaviour<BaitBehaviour>().BaitType, luckMultiplier);
            }

            // Apply wait time multiplier from special effects
            float baseWaitTime = Random.Range(5, 11);
            float waitTimeMultiplier = playerData.GetWaitTimeReduction();
            timeTillResultsSeconds = Mathf.Max(1f, baseWaitTime / waitTimeMultiplier);

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
        if (!isFishing || fightStarted) {
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
            RpcEndFishing(EndFishingReason.noFishGenerated);
            ServerEndFishing();
            return;
        }

        minFishingTimeSeconds = Random.Range(6, 11);
        fightStartTime = Time.time;
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

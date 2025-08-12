using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    float lastVerifiedtime = float.MinValue;
    Vector2? lastVerifiedPosition = null;

    private List<Vector2> nextMoves = null;

    [SerializeField] Rigidbody2D playerRigidbody;
    [SerializeField] Transform playerTransform;
    [SerializeField] Camera playerCamera;
    [SerializeField] Animator playerAnimator;
    [SerializeField] FishingManager fishingManager;
    [SerializeField] GameObject playerCanvasPrefab;
    [SerializeField] BoxCollider2D playerCollider;
    [SerializeField] ViewPlayerStats viewPlayerStats;

    //Speed in units per seconds
    public float movementSpeed = 1.7f;

    private PlayerInputHandler inputHandler;

    GameObject worldBounds;

    bool movementDirty; //true if new position has not yet been send to the server;
    int objectsPreventingMovement = 0;
    int objectsPreventingFishing = 0;

    bool gameOnForeground = true;

    List<Collider2D> objectsCollidingPlayer = new List<Collider2D>();

    public void IncreaseObjectsPreventingMovement()
    {
        objectsPreventingMovement++;
    }

    public void DecreaseObjectsPreventingMovement()
    {
        objectsPreventingMovement--;
    }
    public void IncreaseObjectsPreventingFishing()
    {
        objectsPreventingFishing++;
    }
    public void DecreaseObjectsPreventingFishing()
    {
        objectsPreventingFishing--;
    }

    public int GetObjectsPreventingFishing()
    {
        return objectsPreventingFishing;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // If the player collides with an object, stop its movement
        objectsCollidingPlayer.Add(collision.collider);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // If the player collides with an object, stop its movement
        objectsCollidingPlayer.Remove(collision.collider);
    }

    void Start()
    {
        worldBounds = GameObject.Find("World bounds");
        if (!isLocalPlayer)
        {
            return;
        }
        Instantiate(playerCanvasPrefab, playerTransform);
        EnableGameObjects();
    }

    private void Update()
    {
        if (objectsPreventingFishing < 0)
        {
            objectsPreventingFishing = 0;
            Debug.LogError("objectsPreventingFishing was less then 0, this should not have happened");
        }
        if (objectsPreventingMovement < 0)
        {
            objectsPreventingMovement = 0;
            Debug.LogError("objectsPreventingMovement was less then 0, this should not have happened");
        }
        if (!isServer && isLocalPlayer)
        {
            ClampCamera();
        }
    }

    bool FindWorldBoundsObject()
    {
        worldBounds = GameObject.Find("WorldBounds");
        if (worldBounds == null)
        {
            return false;
        }
        return true;
    }

    void ClampCamera()
    {
        if (worldBounds == null)
        {
            if (!FindWorldBoundsObject())
            {
                return;
            }
        }

        float cameraHeight = playerCamera.orthographicSize;
        float cameraWidth = cameraHeight * playerCamera.aspect;

        float minXCamera = worldBounds.transform.position.x - (worldBounds.transform.lossyScale.x / 2) + cameraWidth;
        float maxXCamera = worldBounds.transform.position.x - (worldBounds.transform.lossyScale.x / 2) + worldBounds.transform.lossyScale.x - cameraWidth;

        float minYCamera = worldBounds.transform.position.y - (worldBounds.transform.lossyScale.y / 2) + cameraHeight;
        float maxYCamera = worldBounds.transform.position.y - (worldBounds.transform.lossyScale.y / 2) + worldBounds.transform.lossyScale.y - cameraHeight;

        //First set the camera position to the player position, then make sure it does not get out of the world bounds.
        //The camera resetting is a trick to make the player get back into the middle of the screen when the players moves away from the world bounds.
        playerCamera.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, playerCamera.transform.position.z);
        playerCamera.transform.position = new Vector3(Mathf.Clamp(playerCamera.transform.position.x, minXCamera, maxXCamera), Mathf.Clamp(playerCamera.transform.position.y, minYCamera, maxYCamera), playerCamera.transform.position.z);
    }

    Vector2 ClampPlayerMovement(Vector2 movementVector)
    {
        if (worldBounds == null)
        {
            if (!FindWorldBoundsObject())
            {
                return movementVector;
            }
        }
        if (objectsPreventingMovement > 0 || fishingManager.isFishing)
        {
            movementVector = Vector2.zero;
            return movementVector;
        }
        float playerWidth = 0.5f;
        float playerHeight = 0.5f;

        //Calculate max world bounds
        float minXPlayer = worldBounds.transform.position.x - (worldBounds.transform.lossyScale.x / 2) + playerWidth;
        float maxXPlayer = worldBounds.transform.position.x - (worldBounds.transform.lossyScale.x / 2) + worldBounds.transform.lossyScale.x - playerWidth;

        float minYPlayer = worldBounds.transform.position.y - (worldBounds.transform.lossyScale.y / 2) + playerHeight;
        float maxYPlayer = worldBounds.transform.position.y - (worldBounds.transform.lossyScale.y / 2) + worldBounds.transform.lossyScale.y - playerHeight;

        float newXposition = transform.position.x;
        float newYposition = transform.position.y;

        //Clamp position and movement vector
        if (transform.position.x <= minXPlayer && movementVector.x < 0)
        {
            movementVector.x = 0;
            newXposition = minXPlayer;
        }
        else if (transform.position.x >= maxXPlayer && movementVector.x > 0)
        {
            movementVector.x = 0;
            newXposition = maxXPlayer;
        }

        if (transform.position.y <= minYPlayer && movementVector.y < 0)
        {
            movementVector.y = 0;
            newYposition = minYPlayer;
        }
        else if (transform.position.y >= maxYPlayer && movementVector.y > 0)
        {
            movementVector.y = 0;
            newYposition = maxYPlayer;
        }

        transform.position = new Vector3(newXposition, newYposition, transform.position.z);
        return movementVector;
    }

    void EnableGameObjects()
    {
        playerCollider.enabled = true;
    }

    public bool IsPointerOverUI(Vector2 mousePos)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = mousePos;

        List<RaycastResult> results = new List<RaycastResult>();

        // Find all active GraphicRaycasters in the scene
        GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
        foreach (GraphicRaycaster raycaster in raycasters)
        {
            if (!raycaster.gameObject.activeInHierarchy)
                continue;

            raycaster.Raycast(pointerData, results);
            if (results.Count > 0)
                return true;
        }

        return false;
    }

    System.Diagnostics.Stopwatch stopwatch;

    private void PathFindRequestCallback(List<Vector2> path)
    {
        nextMoves = path;
        stopwatch.Stop();

        Debug.Log($"Finding path took {stopwatch.Elapsed.TotalMilliseconds} ms");
    }

    [Client]
    private bool CheckNpcClicked(Vector2 clickedPos)
    {
        GameObject clickedNpc = null;
        RaycastHit2D[] hits =  Physics2D.RaycastAll(clickedPos, Vector2.zero, 100);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.transform.gameObject.CompareTag("NPC"))
            {
                clickedNpc = hit.transform.gameObject;
                break;
            }
        }

        if (clickedNpc == null)
        {
            return false;
        }

        NpcDialog npcDialog = clickedNpc.GetComponent<NpcDialog>();
        if (npcDialog == null)
        {
            Debug.Log("Could not find the npcController on the clicked NPC");
            return false;
        }

        npcDialog.StartDialog(playerCamera);
        return true;
    }
    
    public static event Action OnMouseClickedAction;

    //This function is being called from the PlayerController input system. It triggers when the left mouse button in clicked.
    private void ProcessMouseClick(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || !gameOnForeground || !context.performed)
        {
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (NpcDialog.DialogActive && OnMouseClickedAction != null)
        {
            OnMouseClickedAction.Invoke();
            return;
        }
        
        // This helps, but we still need to check for objectsPreventingFishing since this does not account for clicks outside a canvas.
        if (IsPointerOverUI(mousePos))
        {
            return; // UI already handled this
        }

        Vector2 clickedPos = playerCamera.ScreenToWorldPoint(mousePos);
        //Check for mouse click starting at objects with most priority, return if the click has been handled.
        if (CheckNpcClicked(clickedPos))
        {
            return;
        }
        if (viewPlayerStats.ProcesPlayerCheck(clickedPos))
        {
            return;
        }
        if (fishingManager.ProcessFishing(clickedPos) || objectsPreventingFishing > 0)
        {
            return;
        }

        // Click was not on the water or another player and the mouse was not over a ui element. Walk to the clicked position
        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        PathFinding pathFinder = SceneObjectCache.GetPathFinding(gameObject.scene);
        pathFinder.QueueNewPath(transform.position, clickedPos, gameObject, PathFindRequestCallback);
    }
    
    float lastTimeMovedDiagonally = 0;
    Vector2 lastTimeMovedDiagonallyVector = new Vector2();

    float lastSendPositionTime = float.MinValue;
    [SerializeField]
    int totalPositionSendsPerSecond = 10;

    private void FixedUpdate()
    {
        if (!isLocalPlayer || inputHandler == null)
        {
            return;
        }

        if (fishingManager.isFishing || objectsPreventingMovement > 0)
        {
            ApplyAnimation(false);
            return;
        }
        Vector2 dir = inputHandler.MoveAction.ReadValue<Vector2>();

        if (HasVelocity(dir))
        {
            nextMoves = null;
        }

        Vector2 movementVector = CalculateMovementVector(dir);
        movementVector = ClampPlayerMovement(movementVector);

        // We want the player to look in the direction of their input to let the game feel more responsive. This is only locally, but who cares?
        bool isMoving = HasVelocity(movementVector);
        Vector2 animationDirection = isMoving ? movementVector : dir;

        ApplyAnimation(animationDirection, isMoving);

        if (movementVector != Vector2.zero)
        {
            //We need to set this globally to true. The data is only send once every x milliseconds. If the time has not yet passed, the new movement wont be send.
            //We do send the movement in this case a few frames later when the time has passed altough we might not be moving anymore.
            movementDirty = true;
        }
        MovePlayer(movementVector, movementSpeed);

        if (movementDirty && Time.time - lastSendPositionTime > 1f / totalPositionSendsPerSecond)
        {
            movementDirty = false;
            lastSendPositionTime = Time.time;
            CmdSendMoveToServer(transform.position);
        }
    }

    void MovePlayer(Vector2 moveDir, float speed)
    {
        playerRigidbody.linearVelocity = moveDir.normalized * speed;
    }

    void ApplyAnimation(bool hasVelocity)
    {
        ApplyAnimation(Vector2.zero, hasVelocity);
    }

    void ApplyAnimation(Vector2 dir, bool hasVelocity)
    {
        float delayTime = 0.07f;
        dir = dir.normalized;
        if (HasVelocity(dir))
        {
            if (Time.time - lastTimeMovedDiagonally > delayTime || (lastTimeMovedDiagonallyVector != dir && dir.x != 0 && dir.y != 0))
            {
                playerAnimator.SetFloat("Horizontal", dir.x);
                playerAnimator.SetFloat("Vertical", dir.y);
            }

            if (dir.x != 0 && dir.y != 0)
            {
                lastTimeMovedDiagonally = Time.time;
                lastTimeMovedDiagonallyVector = dir;
            }
        }
        else if (Time.time - lastTimeMovedDiagonally < delayTime)
        {
            playerAnimator.SetFloat("Horizontal", lastTimeMovedDiagonallyVector.x);
            playerAnimator.SetFloat("Vertical", lastTimeMovedDiagonallyVector.y);
        }
        playerAnimator.SetFloat("Speed", hasVelocity ? 1 : 0);
    }

    //Function is called while throwing in the rod to make the player face the direction that the line is thrown.
    public void SetPlayerAnimationForDirection(Vector2 dir)
    {
        dir = dir.normalized;
        playerAnimator.SetFloat("Horizontal", dir.x);
        playerAnimator.SetFloat("Vertical", dir.y);
    }

    private bool HasVelocity(Vector2 dir)
    {
        return dir != Vector2.zero;
    }

    public Vector2 CalculateMovementVector(Vector2 dir)
    {
        if (nextMoves != null)
        {
            dir = nextMoves[0] - (Vector2)transform.position;
            // Don't make this value too small or the player will osscilate around this point. ALso don't make it too big or the player will not be properly allinged with the grid
            if (Vector2.Distance(nextMoves[0], transform.position) < 0.1)
            {
                nextMoves.RemoveAt(0);
                if (nextMoves.Count == 0)
                {
                    nextMoves = null;
                }
            }
        }
        Vector2 movementVector = dir.normalized;
        foreach (Collider2D col in objectsCollidingPlayer)
        {
            //Only walk if not walking into a collider
            Vector2 collisionDirection = (col.ClosestPoint(playerCollider.bounds.center) - (Vector2)playerCollider.bounds.center).normalized;
            float angle = Vector2.Angle(dir, collisionDirection);
            if (angle < 50f)
            {
                movementVector = Vector2.zero;
            }
        }
        return movementVector;
    }

    [Client]
    public void MoveOtherPlayerLocally(Vector2 dir, Vector2 targetPos)
    {
        if (isLocalPlayer)
        {
            return;
        }
        ApplyAnimation(dir, HasVelocity(dir));
        dir = ClampPlayerMovement(dir);
        Vector2 newPos = (movementSpeed * Time.deltaTime * dir) + playerRigidbody.position;
        //Clamp the position to the target position if the movement goes beyond the targetposition in this frame.
        playerRigidbody.position = ClampPlayerMovement(transform.position, newPos, targetPos);
    }

    [Client]
    private Vector2 ClampPlayerMovement(Vector2 curPos, Vector2 nextPos, Vector2 targetPos)
    {
        if (curPos.x < targetPos.x && nextPos.x > targetPos.x)
        {
            nextPos.x = targetPos.x;
        }
        else if (curPos.x > targetPos.x && nextPos.x < targetPos.x)
        {
            nextPos.x = targetPos.x;
        }

        if (curPos.y < targetPos.y && nextPos.y > targetPos.y)
        {
            nextPos.y = targetPos.y;
        }
        else if (curPos.y > targetPos.y && nextPos.y < targetPos.y)
        {
            nextPos.y = targetPos.y;
        }
        return nextPos;
    }

    [Server]
    public void ServerTeleportPlayer(Vector2 pos)
    {
        nextMoves = null;
        transform.position = pos;
        lastVerifiedPosition = transform.position;
        lastVerifiedtime = Time.time;
        TargetSetPosition(pos);
    }

    [TargetRpc]
    void TargetSetPosition(Vector2 position)
    {
        Debug.Log("Forced target position");
        nextMoves = null;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    [Command]
    void CmdSendMoveToServer(Vector2 position)
    {

        if (!lastVerifiedPosition.HasValue)
        {
            //preserve the z position
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            lastVerifiedPosition = transform.position;
            lastVerifiedtime = Time.time;
            return;
        }

        Vector2 prevPos = lastVerifiedPosition.Value;
        byte speedRes = CheckSpeedValid(position, prevPos, movementSpeed);

        if (speedRes == 1)
        {
            Debug.Log("Speed was invalid");
            transform.position = new Vector3(prevPos.x, prevPos.y, transform.position.z);
            lastVerifiedPosition = transform.position;
            lastVerifiedtime = Time.time;
            TargetSetPosition(transform.position);
            return;
        }

        bool posValid = CheckNewPosValid(position);

        if (!posValid)
        {
            Debug.Log("Pos was invalid");
            transform.position = new Vector3(prevPos.x, prevPos.y, transform.position.z);
            lastVerifiedPosition = transform.position;
            lastVerifiedtime = Time.time;
            TargetSetPosition(transform.position);
            return;
        }

        if (speedRes == 0 && posValid) {
            lastVerifiedPosition = position;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    System.Diagnostics.Stopwatch speedCheckTimer = new System.Diagnostics.Stopwatch();

    //Anti cheat function, should detect a client doing speed hacking
    [Server]
    byte CheckSpeedValid(Vector2 position, Vector2 prevPos, float speed)
    {
        if(!speedCheckTimer.IsRunning) {
            speedCheckTimer.Start();
            return 255;
        }

        double elapsedSeconds = speedCheckTimer.Elapsed.TotalSeconds;

        //Check for speed hacking
        if (elapsedSeconds < 0.5f)
        {
            return 255;
        }

        //Times 1.2 to account for network latency related issues.
        //float maxAllowedDistance = speed * Mathf.Min(Time.time - lastVerifiedtime, 2f) * 1.4f;
        float maxAllowedDistance = speed * (float)Mathf.Min((float)elapsedSeconds, 2f) * 2f;
        float actualDistance = Vector2.Distance(position, prevPos);


        // Subtract the frame time for extra savety margin.
        speedCheckTimer.Restart();
        lastVerifiedPosition = position;

        if (actualDistance > maxAllowedDistance)
        {
            return 1;
        }

        return 0;
    }

    //Anti cheat function, should detect if a client tries to walk on something unwalkable
    [Server]
    bool CheckNewPosValid(Vector2 position)
    {
        CompositeCollider2D coll = SceneObjectCache.GetWorldCollider(gameObject.scene);

        if (coll == null)
        {
            Debug.LogWarning("No compositeCollider on Root on this scene, can't check if player position is legal");
            transform.position = position;
            return true;
        }

        Vector2 checkPosition = position;
        checkPosition.y += playerCollider.offset.y;

        if (coll.OverlapPoint(checkPosition))
        {
            return false;
        }
        return true;
    }

    //Next function called by MonoBehaviour when the game is switched to the background, now we can't throw in when the game is not in the forground.
    void OnApplicationFocus(bool focusStatus)
    {
        gameOnForeground = focusStatus;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
        {
            return;
        }
        inputHandler = new PlayerInputHandler(ProcessMouseClick);
    }

    private void OnDisable()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        inputHandler?.Dispose();
    }
}

public class PlayerInputHandler
{
    private PlayerControls controls;
    public InputAction MoveAction => controls.Player.Move;

    public PlayerInputHandler(Action<InputAction.CallbackContext> onFishingPerformed)
    {
        controls = new PlayerControls();
        controls.Player.Fishing.performed += onFishingPerformed;
        controls.Player.Fishing.Enable();
        controls.Player.Move.Enable();
    }

    public void Dispose()
    {
        controls.Player.Fishing.Disable();
        controls.Player.Move.Disable();
        controls.Dispose();
    }
}

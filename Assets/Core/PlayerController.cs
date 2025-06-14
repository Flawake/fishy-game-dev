using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    float lastVerifiedtime = float.MinValue;
    Vector2? lastVerifiedPosition = null;

    private List<Vector2> nextMoves = null;

    public Rigidbody2D playerRigidbody;

    [SerializeField] Transform playerTransform;
    [SerializeField] Camera playerCamera;
    [SerializeField] Animator playerAnimator;
    [SerializeField] FishingManager fishingManager;
    [SerializeField] GameObject playerCanvasPrefab;
    [SerializeField] BoxCollider2D playerCollider;
    [SerializeField] ViewPlayerStats viewPlayerStats;
    [SerializeField] PathFinding pathFinding;

    //Speed in units per seconds
    public float movementSpeed = 1.7f;

    PlayerControls playerControls;
    InputAction moveAction;

    GameObject worldBounds;

    Vector2 movementVector;

    bool movementDirty; //true if new position has not yet been send to the server;
    bool hasVelocity;   //set's wether the player has a velocity. needed for the animations

    int objectsPreventingMovement = 0;
    int objectsPreventingFishing = 0;

    bool gameOnForeground = true;

    List<Collider2D> objectsCollidingPlayer = new List<Collider2D>();

    //The scene that the player is in, used to get worldbound and the compositecollider
    Scene locatedScene;

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
        if (!isLocalPlayer) {
            return;
        }
        Instantiate(playerCanvasPrefab, playerTransform);
        EnableGameObjects();
    }

    private void Update()
    {
        if(objectsPreventingFishing < 0)
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
        if(worldBounds == null)
        {
            return false;
        }
        return true;
    }

    void ClampCamera() {
        if (worldBounds == null)
        {
            if(!FindWorldBoundsObject())
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

    Vector2 ClampPlayerMovement(Vector2 movementVector) {
        //TODO: Don't return movementVector but vector2.zero. This is used for testing the scene travel
        if(worldBounds == null)
        {
            if (!FindWorldBoundsObject())
            {
                return movementVector;
            }
        }
        if(objectsPreventingMovement > 0 || fishingManager.isFishing)
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

        float newXposition = this.transform.position.x;
        float newYposition = this.transform.position.y;

        //Clamp position and movement vector
        if (this.transform.position.x <= minXPlayer && movementVector.x < 0)
        {
            movementVector.x = 0;
            newXposition = minXPlayer;
        }
        else if (this.transform.position.x >= maxXPlayer && movementVector.x > 0)
        {
            movementVector.x = 0;
            newXposition = maxXPlayer;
        }

        if (this.transform.position.y <= minYPlayer && movementVector.y < 0)
        {
            movementVector.y = 0;
            newYposition = minYPlayer;
        }
        else if (this.transform.position.y >= maxYPlayer && movementVector.y > 0)
        {
            movementVector.y = 0;
            newYposition = maxYPlayer;
        }

        this.transform.position = new Vector3(newXposition, newYposition, this.transform.position.z);
        return movementVector;
    }

    void EnableGameObjects() {
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

    //This function is being called from the PlayerController input system. It triggers when the left mouse button in clicked.
    public void ProcessMouseClick(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || !gameOnForeground || !context.performed) {
            return; 
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // This helps, but we still need to check for objectsPreventingFishing since this does not account for clicks outside a canvas.
        if (IsPointerOverUI(mousePos)) {
            return; // UI already handled this
        }
        
        Vector2 clickedPos = playerCamera.ScreenToWorldPoint(mousePos);
        //Check for mouse click starting at objects with most priority, return if the click has been handled.
        if (viewPlayerStats.ProcesPlayerCheck(clickedPos)) {
            return;
        }
        if (fishingManager.ProcessFishing(clickedPos) || objectsPreventingFishing > 0) {
            return;
        }
        
        // Click was not on the water or another player and the mouse was not over a ui element. Walk to the clicked position
        nextMoves = pathFinding.FindPath(this.transform.position, clickedPos);

        //We should not return but look what else the click could have been for.
    }

    float lastTimeMovedDiagonally = 0;
    Vector2 lastTimeMovedDiagonallyVector = new Vector2();

    float lastSendPositionTime = float.MinValue;
    [SerializeField]
    int totalPositionSendsPerSecond = 10;

    private void FixedUpdate()
    {
        if(!isLocalPlayer)
        {
            return;
        }

        if(moveAction != null)
        {
            SetMoveVariables(moveAction.ReadValue<Vector2>());
        }
        else
        {
            if (movementVector == null)
            {
                return;
            }
        }

        movementVector = ClampPlayerMovement(movementVector);
        if(movementVector != Vector2.zero)
        {
            //We need to set this globally to true. The data is only send once every x milliseconds. If the time has not yet passed the new movement wont be send.
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

    void ApplyAnimation()
    {
        ApplyAnimation(Vector2.zero);
    }

    void ApplyAnimation(Vector2 dir) 
    {
        float delayTime = 0.07f;
        bool moving = dir != Vector2.zero;
        dir = dir.normalized;
        if (moving)
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
    public void SetPlayerAnimationForDirection(Vector2 dir) {
        dir = dir.normalized;
        playerAnimator.SetFloat("Horizontal", dir.x);
        playerAnimator.SetFloat("Vertical", dir.y);
    }

    public void SetMoveVariables(Vector2 dir)
    {
        if (!isLocalPlayer)
        {
            return;
        }
        if (fishingManager.isFishing || objectsPreventingMovement > 0)
        {
            hasVelocity = false;
            ApplyAnimation();
            return;
        }
        hasVelocity = dir != Vector2.zero;
        if (hasVelocity)
        {
            nextMoves = null;
        }
        else
        {
            if (nextMoves != null)
            {
                dir = nextMoves[0] - (Vector2)transform.position;
                hasVelocity = true;
                if (Vector2.Distance(nextMoves[0], transform.position) < 0.1)
                {
                    if(nextMoves.Count > 0)
                    {
                        nextMoves.RemoveAt(0);
                        if (nextMoves.Count == 0)
                        {
                            nextMoves = null;
                            hasVelocity = false;
                        }
                    }
                }
            }
        }
        movementVector = dir.normalized;
        foreach (Collider2D col in objectsCollidingPlayer)
        {
            //Only walk if not walking into a collider
            Vector2 collisionDirection = (col.ClosestPoint(playerCollider.bounds.center) - (Vector2)playerCollider.bounds.center).normalized;
            float angle = Vector2.Angle(dir, collisionDirection);
            if (angle < 50f)
            {
                movementVector = Vector2.zero;
                hasVelocity = false;
            }
        }
        ApplyAnimation(dir);
    }

    public void MoveOtherPlayerLocally(Vector2 dir, Vector2 targetPos)
    {
        if(isLocalPlayer)
        {
            return;
        }
        hasVelocity = dir != Vector2.zero;
        ApplyAnimation(dir);
        dir = ClampPlayerMovement(dir);
        Vector2 newPos = (movementSpeed * Time.deltaTime * dir) + (Vector2)playerRigidbody.position;
        //Clamp the position to the target position if the movement goes beyond the targetposition in this frame.
        if(transform.position.x < targetPos.x && newPos.x > targetPos.x)
        {
            newPos.x = targetPos.x;
        }
        else if (transform.position.x > targetPos.x && newPos.x < targetPos.x)
        {
            newPos.x = targetPos.x;
        }

        if (transform.position.y < targetPos.y && newPos.y > targetPos.y)
        {
            newPos.y = targetPos.y;
        }
        else if (transform.position.y > targetPos.y && newPos.y < targetPos.y)
        {
            newPos.y = targetPos.y;
        }
        playerRigidbody.position = newPos;
    }

    [Server]
    public void ServerTeleportPlayer(Vector2 pos)
    {
        nextMoves = null;
        transform.position = pos;
        lastVerifiedPosition = transform.position;
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

        //Get current scene to filter for the correct CompositeCollider2D and gameobjects in this scene.
        Scene activeScene = gameObject.scene;
        if (activeScene != locatedScene)
        {
            locatedScene = activeScene;
        }

        if (lastVerifiedPosition == null)
        {
            //preserve the z position
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            lastVerifiedPosition = transform.position;
            lastVerifiedtime = Time.time;
            return;
        }

        Vector2 prevPos = (Vector2)lastVerifiedPosition;

        if (!CheckSpeedValid(position, prevPos))
        {
            Debug.Log("Speed was invalid");
            transform.position = new Vector3(prevPos.x, prevPos.y, transform.position.z);;
            lastVerifiedPosition = transform.position;
            TargetSetPosition(transform.position);
            return;
        }

        Vector2 newValidPos;
        if (!CheckNewPosValid(position, prevPos, out newValidPos)) {
            Debug.Log("Pos was invalid");
            transform.position = new Vector3(newValidPos.x, newValidPos.y, transform.position.z);
            lastVerifiedPosition = transform.position;
            TargetSetPosition(transform.position);
            return;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    //Anti cheat function, should detect a client doing speed hacking
    [Server]
    bool CheckSpeedValid(Vector2 position, Vector2 prevPos) {
        //Check for speed hacking

        if (Time.time - lastVerifiedtime > 0.5f)
        {
            float checkTime = lastVerifiedtime;
            lastVerifiedtime = Time.time;
            lastVerifiedPosition = position;
            Vector2 AbsoluteMovement = new Vector2(Mathf.Abs(position.x - prevPos.x), Mathf.Abs(position.y - prevPos.y));
            float absoluteDistance = Mathf.Sqrt(Mathf.Pow(AbsoluteMovement.x, 2) + Mathf.Pow(AbsoluteMovement.y, 2));
            //Times 1.2 to account for network latency related issues.
            if (absoluteDistance > movementSpeed * Mathf.Min((float)(Time.time - checkTime), 2f) * 1.2)
            {
                return false;
            }
        }
        return true;
    }

    //Anti cheat function, should detect if a client tries to walk on something unwalkable
    [Server]
    bool CheckNewPosValid(Vector2 position, Vector2 prevPos, out Vector2 newValidPos) {
        CompositeCollider2D coll = SceneObjectCache.GetWorldCollider(locatedScene);

        newValidPos = Vector2.zero;

        if (coll == null)
        {
            Debug.LogWarning("No compositeCollider on Root on this scene, can't check if player position is legal");
            transform.position = position;
            return true;
        }

        Vector3 checkPosition = position;
        checkPosition.y += playerCollider.offset.y;
        float maxDistance = 0.06f + (playerCollider.size.y / 2);

        if (coll.OverlapPoint(checkPosition))
        {
            Vector2 closestPoint = coll.ClosestPoint(checkPosition);
            if (Vector2.Distance(closestPoint, checkPosition) > maxDistance)
            {
                newValidPos = closestPoint;
                return false;
            }
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
        playerControls = new PlayerControls();
        playerControls.Player.Fishing.performed += ProcessMouseClick;
        playerControls.Player.Fishing.Enable();
        moveAction = playerControls.Player.Move;
        moveAction.Enable();
    }

    private void OnDisable()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        moveAction.Disable();
        playerControls.Player.Fishing.Disable();
    }
}

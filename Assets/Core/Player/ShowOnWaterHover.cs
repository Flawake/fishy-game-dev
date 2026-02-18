using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class ShowOnWaterHover : MonoBehaviour
{
    [SerializeField] GameObject targetToShow;
    [SerializeField] Camera cam;

    static int WaterLayer;
    NetworkIdentity _localPlayerId;

    void Awake()
    {
        WaterLayer = LayerMask.GetMask("Water");
        _localPlayerId = GetComponentInParent<NetworkIdentity>();
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        if (_localPlayerId == null || !_localPlayerId.isLocalPlayer)
            return;

        if (targetToShow == null || cam == null || Mouse.current == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = cam.ScreenToWorldPoint(mousePos);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, float.MaxValue, WaterLayer);

        bool show = false;
        if (hit.collider != null)
        {
            var playersNearWater = hit.collider.GetComponent<PlayersNearWater>();
            if (playersNearWater != null)
            {
                var playersNearPuddle = playersNearWater.GetPlayersNearPuddle();
                show = playersNearPuddle != null && playersNearPuddle.Contains(_localPlayerId.netId);
            }
        }

        targetToShow.SetActive(show);

        if (show)
            targetToShow.transform.position = new Vector3(worldPos.x, worldPos.y, targetToShow.transform.position.z);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class ShowOnWaterHover : MonoBehaviour
{
    [SerializeField] GameObject targetToShow;
    [SerializeField] Camera cam;
    [SerializeField] FishingManager fishingManager;

    [Header("Shader Parameters")]
    [SerializeField] Color circleColor = new Color(0.5f, 0.5f, 0.5f, 0.15f);
    [SerializeField] Color ringColor = new Color(0f, 0f, 0f, 0.15f);
    [SerializeField, Range(0f, 0.5f)] float radius = 0.4f;
    [SerializeField, Range(0f, 0.5f)] float ringThickness = 0.05f;

    NetworkIdentity _localPlayerIdentity;
    SpriteRenderer _spriteRenderer;
    MaterialPropertyBlock _propBlock;

    static readonly int CircleColorId = Shader.PropertyToID("_CircleColor");
    static readonly int RingColorId = Shader.PropertyToID("_RingColor");
    static readonly int RadiusId = Shader.PropertyToID("_Radius");
    static readonly int RingThicknessId = Shader.PropertyToID("_RingThickness");

    void Awake()
    {
        _localPlayerIdentity = GetComponentInParent<NetworkIdentity>();
        _spriteRenderer = targetToShow.GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_localPlayerIdentity == null || !_localPlayerIdentity.isLocalPlayer || Mouse.current == null)
        {
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = cam.ScreenToWorldPoint(mousePos);

        bool show = IsValidFishingSpot(worldPos);
        targetToShow.SetActive(show);
        if (show)
        {
            float worldScale = targetToShow.transform.lossyScale.x;
            radius = fishingManager.GetRodThrowDistance() / worldScale;
            UpdateShaderProperties();
        }
    }

    void UpdateShaderProperties()
    {
        _spriteRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(CircleColorId, circleColor);
        _propBlock.SetColor(RingColorId, ringColor);
        _propBlock.SetFloat(RadiusId, radius);
        _propBlock.SetFloat(RingThicknessId, ringThickness);
        _spriteRenderer.SetPropertyBlock(_propBlock);
    }

    bool IsValidFishingSpot(Vector2 worldPosition)
    {
        return fishingManager.IsValidFishingSpot(worldPosition);
    }
}

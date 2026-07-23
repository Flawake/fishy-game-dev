using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapAreaMouseHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
{
    [SerializeField]
    Collider2D col;
    [SerializeField]
    RectTransform rectTransform;
    [SerializeField]
    GameObject areaNameBoxObject;
    [SerializeField]
    TMP_Text areaOnlinePlayersText;
    [SerializeField]
    AreaComponent area;

    public void OnPointerEnter(PointerEventData eventData)
    {

        if (areaNameBoxObject != null)
        {
            areaOnlinePlayersText.text = "0";
            if (GameNetworkManager.AreaPlayerCount.TryGetValue(area.area, out int onlinePlayerCount)) {
                areaOnlinePlayersText.text = onlinePlayerCount.ToString();
            }
            areaNameBoxObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {

        if (areaNameBoxObject != null)
        {
            areaNameBoxObject.SetActive(false);
        }
    }

    #region ICanvasRaycastFilter implementation
    public bool IsRaycastLocationValid(Vector2 screenPos, Camera eventCamera)
    {
        if (col == null || rectTransform == null)
        {
            return false;
        }
        var worldPoint = Vector3.zero;
        var isInside = RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform,
            screenPos,
            eventCamera,
            out worldPoint
        );
        if (isInside)
            isInside = col.OverlapPoint(worldPoint);
        return isInside;
    }
    #endregion
}
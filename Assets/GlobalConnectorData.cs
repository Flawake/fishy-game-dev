using UnityEngine;

[System.Serializable]
public struct AreaImageConnector
{
    public Area Area;
    public Sprite AreaImage;
}

public static class GlobalConnector
{
    public static AreaImageConnector[] areaImageConnector;
    public static Sprite bucksImage;
}

public class GlobalConnectorData : MonoBehaviour
{
    [SerializeField]
    AreaImageConnector[] areaImageConnectorData;
    [SerializeField]
    Sprite bucksImageData;

    void Awake()
    {
        GlobalConnector.areaImageConnector = areaImageConnectorData;
        GlobalConnector.bucksImage = bucksImageData;
    }
}

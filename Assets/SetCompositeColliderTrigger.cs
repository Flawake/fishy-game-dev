using Mirror;
using UnityEngine;

public class SetCompositeColliderTrigger : MonoBehaviour
{
    [SerializeField]
    CompositeCollider2D coll;

    public void Start()
    {
        if(NetworkServer.active) {
            ApplyServerSettings();
        }
        else if(NetworkClient.active) {
            ApplyClientSettings();
        }
    }

    void ApplyServerSettings() {
        coll.geometryType = CompositeCollider2D.GeometryType.Polygons;
        coll.isTrigger = true;
        Physics2D.SyncTransforms();
    }

    void ApplyClientSettings() {
        coll.geometryType = CompositeCollider2D.GeometryType.Outlines;
        coll.isTrigger = false;
        Physics2D.SyncTransforms();
    }
}

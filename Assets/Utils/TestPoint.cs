using UnityEngine;

[ExecuteInEditMode] // Ensures script runs in Edit mode
public class TestPointEditor : MonoBehaviour
{
    public CompositeCollider2D targetCollider;
    
    void OnDrawGizmos()
    {
        if (targetCollider == null) return;

        // Check if the object's position is inside the collider
        bool isInside = targetCollider.OverlapPoint(transform.position);

        // Set Gizmo color based on result
        Gizmos.color = isInside ? Color.green : Color.red;

        // Draw a sphere at the position for visualization
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
}
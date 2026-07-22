using UnityEngine;

public static class UILayerManager
{
    /// <summary>
    /// Raises the given panel (or any descendant of it) above all other panels
    /// in its Canvas. Call this right after activating the panel.
    /// </summary>
    public static void BringToFront(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            panel.transform.SetAsLastSibling();
            return;
        }

        Transform stop = canvas.transform;
        Transform current = panel.transform;
        while (current != null && current != stop)
        {
            current.SetAsLastSibling();
            if (current.parent == stop)
            {
                break;
            }
            current = current.parent;
        }
    }
}

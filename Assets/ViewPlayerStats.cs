using UnityEngine;
using TMPro;

public class ViewPlayerStats : MonoBehaviour
{


    public bool ProcesPlayerCheck(Vector2 clickedPos)
    {
        //Debug.Log("Checking for other player");
        if (!IsOtherPlayer(clickedPos))
        {
            return false;
        }
        return true;
    }

    bool IsOtherPlayer(Vector2 clickedPos)
    { 
        var playerLayer = LayerMask.GetMask("Player");
        RaycastHit2D[] hits =  Physics2D.RaycastAll(clickedPos, Vector2.zero, float.MaxValue, playerLayer);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.name == "PreciseCollision" )
            {
                PlayerStatMenu(hit.collider.gameObject);
                Debug.Log("this is a player");
                return true;
            }
        }
        return false;
    }
    
    void PlayerStatMenu(GameObject menu)
    {
        var player = GetTopmostParent(menu);
        var playerStats = player.GetComponent<PlayerData>();
        var canvas = menu.transform.parent.GetChild(1).gameObject.transform;
        canvas.Find("PlayerName").gameObject.GetComponent<TMP_Text>().text = playerStats.GetUsername();
        canvas.Find("PlayerXp").gameObject.GetComponent<TMP_Text>().text = playerStats.GetXp().ToString();
        canvas.gameObject.SetActive(!canvas.gameObject.activeSelf);
    }
    
    
    GameObject GetTopmostParent(GameObject obj)
    {
        Transform current = obj.transform;
        while (current.parent != null)
        {
            current = current.parent;
        }
        return current.gameObject;
    }
}

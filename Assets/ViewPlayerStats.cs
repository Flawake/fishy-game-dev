using UnityEngine;
using TMPro;

public class ViewPlayerStats : MonoBehaviour
{
    PlayerStatsUIManager _playerStatsUIManager;
    PlayerData _playerData;
    GameObject _player;
    
    public bool ProcesPlayerCheck(Vector2 clickedPos)
    {
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
                return true;
            }
        }
        return false;
    }
    
    void PlayerStatMenu(GameObject menu)
    {
        _player = GetTopmostParent(menu);
        _playerData = _player.GetComponent<PlayerData>();
        var canvas = menu.transform.parent.GetChild(1).gameObject.transform;
        canvas.Find("PlayerName").gameObject.GetComponent<TMP_Text>().text = _playerData.GetUsername();
        canvas.Find("PlayerXp").gameObject.GetComponent<TMP_Text>().text = _playerData.GetXp().ToString();
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

    public void More()
    {
        _playerStatsUIManager = _player.transform.Find("Canvas(Clone)").Find("PlayerStats").gameObject.GetComponent<PlayerStatsUIManager>();
        if (_playerData != null && _playerStatsUIManager != null)
        {
            _playerStatsUIManager.ToggleStore();
        }
        else
        {
            (_playerStatsUIManager).TestFunction();
        }
        
    }


}

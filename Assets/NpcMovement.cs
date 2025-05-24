using System.Linq;
using Mirror;
using UnityEngine;

public class NpcMovement : NetworkBehaviour
{
    [SerializeField] private bool enableMovement = false;
    [SerializeField] private float minTimeBetweenMoves = 3f;
    [SerializeField] private float maxTimeBetweenMoves = 10f;
    [SerializeField] private Collider2D customWalkArea;
    
    float prevMoveTime = float.MinValue;
    float timeBetweenMoves = 0f;
    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        ManageNpcMove();
    }

    void ManageNpcMove()
    {
        if (!enableMovement)
        {
            return;
        }

        if (Time.time - prevMoveTime > timeBetweenMoves)
        {
            Debug.Log("Time to move");
            prevMoveTime = Time.time;
            timeBetweenMoves = Random.Range(minTimeBetweenMoves, maxTimeBetweenMoves);

            Vector2 newMovePos;
            foreach (var i in Enumerable.Range(0, 3))
            {
                Debug.Log("Trying to find move");
                newMovePos = new Vector2(transform.position.x + (Random.Range(-0.5f, 0.5f) * (i + 1)), transform.position.y + (Random.Range(-0.5f, 0.5f) * (i + 1)));
                if (CheckNewPosValid(newMovePos))
                {
                    Debug.Log($"Found move {newMovePos}");
                    transform.position = newMovePos;
                    break;
                }
            }
        }
    }
    
    [Server]
    bool CheckNewPosValid(Vector2 position) {
        if(customWalkArea == null)
        {
            customWalkArea = SceneObjectCache.GetWorldCollider(gameObject.scene);
        }

        if (customWalkArea == null)
        {
            Debug.LogWarning("No walkArea found for this npc");
            return true;
        }

        if (!customWalkArea.OverlapPoint(position))
        {
            Debug.Log($"Invalid pos {position}");
            return false;
        }

        return true;
    }

    [Server]
    void _NpcChangeScene()
    {
        Debug.LogWarning("Moving NPC to new area has not been implemented");
        customWalkArea = SceneObjectCache.GetWorldCollider(gameObject.scene);
    }
}

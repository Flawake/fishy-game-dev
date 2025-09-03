using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class NpcMovement : NetworkBehaviour
{
    [SerializeField] private bool enableMovement;
    [SerializeField] private float minTimeBetweenMoves = 3f;
    [SerializeField] private float maxTimeBetweenMoves = 10f;
    [SerializeField] private float moveSpeed = 1.7f;
    [SerializeField] private Collider2D customWalkArea;
    [SerializeField] private Rigidbody2D npcRigidBody;
    [SerializeField] private Animator npcAnimator;

    private List<Vector2> _nextMoves = new List<Vector2>();
    private float _prevMoveTime = float.MinValue;
    private float _timeBetweenMoves;
    
    private static readonly int AnimatorSpeedHash = Animator.StringToHash("Speed");
    private static readonly int AnimatorHorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int AnimatorVerticalHash = Animator.StringToHash("Vertical");

    private void Update()
    {
        ManageNpcMove();
    }
    
    private void PathFindRequestCallback(List<Vector2> path)
    {
        // Skip first position since it might move the npc a little bit back to get aligned with the grid, but they don't have a collider that interacts with the world. So no point of aligning them.
        if (path == null || path.Count <= 1)
        {
            Debug.LogWarning("Path is null or too short to process.");
            return;
        }

        _nextMoves = path.GetRange(1, path.Count - 1);
    }
    
    private void ManageNpcMove()
    {
        if (!enableMovement)
        {
            return;
        }

        if (!isServer)
        {
            MoveNpc();
            return;
        }

        if (Time.time - _prevMoveTime < _timeBetweenMoves)
        {
            return;
        }
        
        _prevMoveTime = Time.time;
        _timeBetweenMoves = Random.Range(minTimeBetweenMoves, maxTimeBetweenMoves);

        Vector2 newPos = FindNewPos();
        if (newPos != new Vector2(transform.position.x, transform.position.y))
        {
            transform.position = newPos;
            ClientSetNewNpcTargetPos(newPos);
        }
    }

    [Server]
    private Vector2 FindNewPos()
    {
        // The new position might lay outside the walk area. Try up to three times.
        foreach (var attempt in Enumerable.Range(0, 3))
        {
            Vector2 newMovePos = new Vector2(transform.position.x + (Random.Range(-0.5f, 0.5f) * (attempt + 3)), transform.position.y + (Random.Range(-0.5f, 0.5f) * (attempt + 3)));
            if (CheckNewPosValid(newMovePos))
            {
                return newMovePos;
            }
        }

        return transform.position;
    }

    [ClientRpc]
    private void ClientSetNewNpcTargetPos(Vector2 newPos)
    {
        // Make sure the transform.position won't change by forcing the npc at its current position till the path calculation is done
        _nextMoves = null;
        npcRigidBody.linearVelocity = Vector2.zero;
        PathFinding pathFinder = SceneObjectCache.GetPathFinding(gameObject.scene);
        pathFinder.QueueNewPath(transform.position, newPos, gameObject, PathFindRequestCallback);
    }

    private void MoveNpc()
    {
        Vector2 moveDir = Vector2.zero;
        if (_nextMoves != null && _nextMoves.Count != 0)
        {
            moveDir = CalculateMovementVector();
        }
        npcRigidBody.linearVelocity = moveDir.normalized * moveSpeed;

        if (moveDir == Vector2.zero)
        {
            npcAnimator.SetFloat(AnimatorSpeedHash, 0);
            return;
        }
        
        npcAnimator.SetFloat(AnimatorHorizontalHash, moveDir.normalized.x);
        npcAnimator.SetFloat(AnimatorVerticalHash, moveDir.normalized.y);
        npcAnimator.SetFloat(AnimatorSpeedHash, 1);
    }
    
    private Vector2 CalculateMovementVector()
    {
        Vector2 dir = _nextMoves[0] - (Vector2)transform.position;
        // Pin the NPC to the target position if the distance between the target and where it is less is then what it could have moved in the last frame. Used to avoid oscillating around the target position
        if (Vector2.Distance(_nextMoves[0], transform.position) < Time.deltaTime * moveSpeed)
        {
            transform.position = _nextMoves[0];
            _nextMoves.RemoveAt(0);
            if (_nextMoves.Count == 0)
            {
                _nextMoves = null;
            }
        }
        return dir;
    }
    
    [Server]
    private bool CheckNewPosValid(Vector2 position) {
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

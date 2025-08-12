using System;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Shell : MonoBehaviour
{
    [SerializeField] private List<Sprite> shellSprites = new List<Sprite>();
    
    [SerializeField] private SpriteRenderer ShellSpriteRenderer;

    public void SpawnShell(Vector3 shellPosition)
    {
        ShellSpriteRenderer.sprite = shellSprites[Random.Range(0, shellSprites.Count)];
        transform.position = shellPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (NetworkServer.active && other.CompareTag("NPCFeet"))
        {
            GetComponentInParent<ShellSpawner>().NpcCollectShell(new ComparableVector3(transform.position));
        }
        else if(other.CompareTag("PlayerSprite") && other.gameObject.GetComponentInParent<NetworkIdentity>().isOwned)
        {
            ShellSpawner shellSpawner = GetComponentInParent<ShellSpawner>();
            shellSpawner.CmdCollectShell(transform.position);
            // Directly remove the shell locally, don't wait on the server for this.
            shellSpawner.ShellRemoved(new ComparableVector3(transform.position));
        }
    }
}

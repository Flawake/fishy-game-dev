using System.Collections.Generic;
using ItemSystem;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;
using Grants;

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
        else if(NetworkClient.active && other.CompareTag("PlayerSprite") && other.gameObject.GetComponentInParent<NetworkIdentity>().isOwned)
        {
            ShellSpawner shellSpawner = GetComponentInParent<ShellSpawner>();
            ItemGrantService grantService = other.gameObject.GetComponentInParent<ItemGrantService>();
            ItemDefinition shellDef = shellSpawner.GetShellDefinition();
            // Register optimistic shell pickup (1 item)
            System.Guid operationId = System.Guid.Empty;
            if (grantService != null && shellDef != null)
            {
                operationId = grantService.ClientRegisterOptimistic(shellDef, 1);
            }
            shellSpawner.CmdCollectShell(transform.position, operationId);
            // Directly remove the shell locally, don't wait on the server for this.
            shellSpawner.ShellRemoved(new ComparableVector3(transform.position));
        }
    }
}

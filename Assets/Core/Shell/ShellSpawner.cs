using System;
using System.Collections;
using System.Collections.Generic;
using ItemSystem;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class ComparableVector3 : IComparable<ComparableVector3>
{
    public ComparableVector3(Vector3 vector)
    {
        V3 = vector;
    }
    public Vector3 V3;
    public int CompareTo(ComparableVector3 other)
    {
        return ToHash(V3).CompareTo(ToHash(other.V3));
    }
    
    public override bool Equals(object other) => other is ComparableVector3 other1 && V3.Equals(other1.V3);

    public override int GetHashCode() => HashCode.Combine(V3);

    private static int ToHash(Vector3 pos)
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + pos.x.GetHashCode();
            hash = hash * 23 + pos.y.GetHashCode();
            hash = hash * 23 + pos.z.GetHashCode();
            return hash;
        }
    }
}

public static class ComparableVector3Serializer
{
    public static void WriteComparableVector3(this NetworkWriter writer, ComparableVector3 value)
    {
        writer.WriteVector3(value.V3);
    }

    public static ComparableVector3 ReadComparableVector3(this NetworkReader reader)
    {
        return new ComparableVector3(reader.ReadVector3());
    }
}

public class ShellSpawner : NetworkBehaviour
{
    [SerializeField] private ItemDefinition shellDefinition;
    [SerializeField] GameObject shellPrefab;
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();
    readonly SyncSortedSet<ComparableVector3> spawnedShellPositions = new SyncSortedSet<ComparableVector3>();
    
    [SerializeField] Transform shellsParent;
    Dictionary<ComparableVector3, GameObject> spawnedShells = new Dictionary<ComparableVector3, GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        spawnedShellPositions.OnAdd += ShellSpawned;
        spawnedShellPositions.OnRemove += ShellRemoved;
        foreach (ComparableVector3 shellPos in spawnedShellPositions)
        {
            ShellSpawned(shellPos);
        }
    }

    private void ShellSpawned(ComparableVector3 position)
    {
        GameObject newShell = Instantiate(shellPrefab, shellsParent);
        Shell shellScript = newShell.GetComponent<Shell>();
        shellScript.SpawnShell(position.V3);
        spawnedShells.Add(position, newShell);
    }

    public void ShellRemoved(ComparableVector3 position)
    {
        if (spawnedShells.TryGetValue(position, out GameObject shell))
        {
            Destroy(shell);
            spawnedShells.Remove(position);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartCoroutine(TrySpawnShell());
    }

    private IEnumerator TrySpawnShell()
    {
        while (true)
        {
            Vector3 randomPos = spawnPoints[Random.Range(0, spawnPoints.Count)].position;
            if (!spawnedShellPositions.Contains(new ComparableVector3(randomPos)))
            {
                spawnedShellPositions.Add(new ComparableVector3(randomPos));
                ShellSpawned(new ComparableVector3(randomPos));
            }
            yield return new WaitForSeconds(Random.Range(8, 18));
        }
    }

    [Server]
    public void NpcCollectShell(ComparableVector3 position)
    {
        spawnedShellPositions.Remove(position);
        ShellRemoved(position);
    }
    
    [Command(requiresAuthority = false)]
    public void CmdCollectShell(Vector3 shellPosition, NetworkConnectionToClient sender = null)
    {
        PlayerController controller = sender.identity.GetComponent<PlayerController>();
        PlayerDataSyncManager syncManager = sender.identity.GetComponent<PlayerDataSyncManager>();
        // This function checks if the player could have moved to this position, and moves the player on the server if the location is valid
        if (controller.ServerHandleMovement(shellPosition))
        {
            ComparableVector3 shellPos = new ComparableVector3(shellPosition);
            if (spawnedShellPositions.Remove(shellPos))
            {
                ShellRemoved(shellPos);
                ItemInstance shell = new ItemInstance(shellDefinition);
                syncManager.AddItem(shell);
            }
        }
    }
}

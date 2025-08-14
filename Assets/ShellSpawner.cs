using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShellSpawner : NetworkBehaviour
{
    [SerializeField] GameObject shellPrefab;
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();
    SyncSortedSet<Vector3> spawnedShellPositions = new SyncSortedSet<Vector3>();
    
    [SerializeField] Transform shellsParent;
    Dictionary<Vector3, GameObject> spawnedShells = new Dictionary<Vector3, GameObject>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        spawnedShellPositions.OnAdd += ShellSpawned;
        spawnedShellPositions.OnRemove += ShellRemoved;
    }

    private void ShellSpawned(Vector3 position)
    {
        GameObject newShell = Instantiate(shellPrefab, shellsParent);
        Shell shellScript = newShell.GetComponent<Shell>();
        shellScript.SpawnShell(position);
    }

    private void ShellRemoved(Vector3 position)
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
            spawnedShellPositions.Add(randomPos);
            yield return new WaitForSeconds(Random.Range(8, 12));
        }
    }
    
    [Command]
    public void CmdCollectShell(Vector3 shellPosition, NetworkConnectionToClient sender = null) {
        throw new System.NotImplementedException(); //Check validity first
        spawnedShellPositions.Remove(shellPosition);
    }
}

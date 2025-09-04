using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnPointRegistry : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public WorldTravel.CustomSpawnInstruction instruction;
        public Transform spawnPoint;
        public Transform arrivalTarget;
    }

    [Tooltip("Per instruction spawn locations and optional arrival targets for this scene.")]
    public List<Entry> entries = new List<Entry>();

    public bool TryGet(WorldTravel.CustomSpawnInstruction instruction, out Transform spawn, out Transform target)
    {
        foreach (var e in entries)
        {
            if (e.instruction == instruction)
            {
                spawn = e.spawnPoint;
                target = e.arrivalTarget;
                return true;
            }
        }
        spawn = null;
        target = null;
        return false;
    }
} 
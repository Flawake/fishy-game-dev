using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Shell : MonoBehaviour
{
    [SerializeField] private List<Sprite> shellSprites = new List<Sprite>();
    
    [SerializeField] private Sprite renderedShellSprite;

    public void SpawnShell(Vector3 shellPosition)
    {
        renderedShellSprite = shellSprites[Random.Range(0, shellSprites.Count)];
        transform.position = shellPosition;
    }
}

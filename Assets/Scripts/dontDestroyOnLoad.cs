using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class dontDestroyOnLoad : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer || isServer) {
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}

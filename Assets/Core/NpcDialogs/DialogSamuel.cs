using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogSamuel : MonoBehaviour
{
    private Dialog _startDialog = new Dialog(
        1, 
        "Hello sir, I expected you. Do you want some dough again?",
        DialogResponse.End,
        -1,
        -1,
        -1,
        null,
        null,
        null
        );
    
    [SerializeField] NpcDialog npcDialog;

    private void Awake()
    {
        Dictionary<int, Dialog> dialogs = new Dictionary<int, Dialog>();
        dialogs.Add(_startDialog.DialogID, _startDialog);
        npcDialog.SetDialogs(dialogs);
    }
    
}

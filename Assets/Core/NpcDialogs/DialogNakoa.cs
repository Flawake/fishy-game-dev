using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogNakoa : MonoBehaviour
{
    private Dialog _startDialog = new Dialog(
        1, 
        "This beach keeps secrets better than people do.",
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

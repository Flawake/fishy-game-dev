using System.Collections.Generic;
using ItemSystem;
using Mirror;
using UnityEngine;

public class DialogSamuel : NetworkBehaviour
{
    [SerializeField] ItemDefinition doughDefinition;

    private Dialog _startDialog;

    private Dialog dialog2;

    private Dialog dialog3;

    private Dialog dialog4;

    private Dialog dialog5;

    [SerializeField] NpcDialog npcDialog;

    private void Awake()
    {
        if (NetworkServer.active)
        {
            return;
        }
        _startDialog = new Dialog(
            1,
            "Hello sir, I expected you. Do you want some dough?",
            DialogResponse.YesAndNo,
            -1,
            2,
            3,
            null,
            CheckAndGiveDough,
            null
        );

        dialog2 = new Dialog(
            2,
            "No problem at all, is this enough?",
            DialogResponse.YesAndNo,
            -1,
            3,
            4,
            null,
            null,
            CheckAndGiveDough
        );

        dialog3 = new Dialog(
            3,
            "Okay, looking forward to see you later.",
            DialogResponse.End,
            -1,
            -1,
            -1,
            null,
            null,
            null
        );

        dialog4 = new Dialog(
            4,
            "So, here you go. You ain't getting more from me for now.",
            DialogResponse.End,
            -1,
            -1,
            -1,
            null,
            null,
            null
        );

        dialog5 = new Dialog(
            5,
            "You've already got plenty of dough, friend. Don't be greedy now!.",
            DialogResponse.End,
            -1,
            -1,
            -1,
            null,
            null,
            null
        );

        Dictionary<int, Dialog> dialogs = new Dictionary<int, Dialog>
        {
            { _startDialog.DialogID, _startDialog },
            { dialog2.DialogID, dialog2 },
            { dialog3.DialogID, dialog3 },
            { dialog4.DialogID, dialog4 },
            { dialog5.DialogID, dialog5 }
        };
        npcDialog.SetDialogs(dialogs);
    }

    void CheckAndGiveDough()
    {
        PlayerInventory inv = NetworkClient.connection.identity.GetComponent<PlayerInventory>();
        if (HasEnoughDough(inv))
        {
            HasEnoughDoughDialogSetter();
            return;
        }
        ItemInstance dough = new ItemInstance(doughDefinition, 40);
        inv.ClientAddItem(dough);
        CmdRequestDough();
    }

    [Command(requiresAuthority = false)]
    void CmdRequestDough(NetworkConnectionToClient sender = null)
    {
        PlayerInventory inv = sender.identity.GetComponent<PlayerInventory>();
        ItemInstance currentDoughReference = inv.GetBaitByDefinitionId(doughDefinition.Id);
        if (currentDoughReference != null && currentDoughReference.GetState<StackState>().currentAmount > 70)
        {
            GameNetworkManager.KickPlayerForCheating(sender, "Tried claiming too much dough");
        }
        ItemInstance dough = new ItemInstance(doughDefinition, 40);
        PlayerDataSyncManager syncManager = sender.identity.GetComponent<PlayerDataSyncManager>();
        syncManager.AddItem(dough);
    }

    void HasEnoughDoughDialogSetter()
    {
        PlayerInventory inv = NetworkClient.connection.identity.GetComponent<PlayerInventory>();
        if (HasEnoughDough(inv))
        {
            npcDialog.ShowNextDialog(dialog5);
        }
    }

    bool HasEnoughDough(PlayerInventory inventory)
    {
        ItemInstance currentDoughReference = inventory.GetBaitByDefinitionId(doughDefinition.Id);
        if (currentDoughReference != null && currentDoughReference.GetState<StackState>().currentAmount > 70)
        {
            return true;
        }
        return false;
    }
}

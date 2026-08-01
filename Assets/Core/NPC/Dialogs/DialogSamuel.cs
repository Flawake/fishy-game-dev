using ItemSystem;
using Mirror;
using UnityEngine;
using System;
using Grants;

public class DialogSamuel : NetworkBehaviour
{
    [SerializeField] ItemDefinition doughDefinition;
    [SerializeField] NpcDialog npcDialog;

    private DialogNode _startDialog;
    private DialogNode _dialog2;
    private DialogNode _dialog3;
    private DialogNode _dialog4;
    private DialogNode _dialog5;

    private void Awake()
    {
        if (NetworkServer.active)
        {
            return;
        }

        BuildDialogTree();
        npcDialog.SetRootDialog(_startDialog);
    }

    private void BuildDialogTree()
    {
        // Create all dialog nodes
        _startDialog = new DialogNode(
            "Hello sir, I expected you. Do you want some dough?",
            DialogOptions.YesNo
        );

        _dialog2 = new DialogNode(
            "No problem at all, is this enough?",
            DialogOptions.YesNo
        );

        _dialog3 = new DialogNode(
            "Okay, looking forward to see you later.",
            DialogOptions.Click
        );

        _dialog4 = new DialogNode(
            "So, here you go. You ain't getting more from me for now.",
            DialogOptions.Click
        );

        _dialog5 = new DialogNode(
            "You've already got plenty of dough, friend. Don't be greedy now!.",
            DialogOptions.Click
        );

        // Build the dialog tree structure
        _startDialog
            .SetNextYes(_dialog2, CheckAndGiveDough)
            .SetNextNo(_dialog3);

        _dialog2
            .SetNextYes(_dialog3)
            .SetNextNo(_dialog4, CheckAndGiveDough);

        // End dialogs don't need next nodes - they just close
    }

    private void CheckAndGiveDough()
    {
        PlayerInventory inv = NetworkClient.connection.identity.GetComponent<PlayerInventory>();
        if (HasEnoughDough(inv))
        {
            ShowEnoughDoughDialog();
            return;
        }
        
        ItemGrantService grantService = NetworkClient.connection.identity.GetComponent<ItemGrantService>();
        GrantId grantId = GrantId.None;
        if (grantService != null)
        {
            grantId = grantService.ClientRegisterOptimistic(doughDefinition, 40);
        }
        CmdRequestDough(grantId);
    }

    [Command(requiresAuthority = false)]
    void CmdRequestDough(GrantId grantId, NetworkConnectionToClient sender = null)
    {
        PlayerInventory inv = sender.identity.GetComponent<PlayerInventory>();
        ItemGrantService grantService = sender.identity.GetComponent<ItemGrantService>();
        ItemInstance currentDoughReference = inv.GetBaitByDefinitionId(doughDefinition.Id);

        bool hasGrant = grantService != null && grantId.IsValid;

        if (currentDoughReference != null && currentDoughReference.GetState<StackState>().currentAmount > 70)
        {
            if (hasGrant)
            {
                grantService.ServerDeny(grantId);
            }
            GameNetworkManager.KickPlayerForCheating(sender, "Tried claiming too much dough");
            return;
        }

        // With a grant the client already holds the dough optimistically and the confirmation corrects
        // it; without one the new state has to be pushed to the client instead.
        PlayerDataSyncManager syncManager = sender.identity.GetComponent<PlayerDataSyncManager>();
        DeltaItem dough = new DeltaItem(doughDefinition, 40);
        if (!syncManager.ServerAddItem(dough, !hasGrant, out InventoryChange change))
        {
            if (hasGrant)
            {
                grantService.ServerDeny(grantId);
            }
            return;
        }

        if (hasGrant)
        {
            grantService.ServerConfirm(grantId, change);
        }
    }

    private void ShowEnoughDoughDialog()
    {
        PlayerInventory inv = NetworkClient.connection.identity.GetComponent<PlayerInventory>();
        if (HasEnoughDough(inv))
        {
            npcDialog.ShowDialog(_dialog5);
        }
    }

    private bool HasEnoughDough(PlayerInventory inventory)
    {
        ItemInstance currentDoughReference = inventory.GetBaitByDefinitionId(doughDefinition.Id);
        if (currentDoughReference != null && currentDoughReference.GetState<StackState>().currentAmount >= 70)
        {
            return true;
        }
        return false;
    }
}

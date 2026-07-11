using Mirror;
using UnityEngine;

/// <summary>
/// Dialog of Herb, the wandering NPC that gives everybody the same daily quest:
/// bring him the fishes he is craving, get paid in fishcoins.
///
/// Herb is spawned into the area his quest points to by <see cref="HerbSpawnManager"/>,
/// and despawned/respawned whenever the quest changes. This dialog therefore never has to
/// deal with a rollover in place: it is simply built from the quest carried on the spawned
/// Herb (see <see cref="HerbQuestSync"/>).
/// </summary>
public class DialogHerb : MonoBehaviour
{
    [SerializeField] NpcDialog npcDialog;

    private DialogNode _startDialog;
    private DialogNode returningDialog;
    private DialogNode questDialog;
    private DialogNode declinedDialog;
    private DialogNode alreadyDoneDialog;
    private DialogNode notEnoughDialog;
    private DialogNode successDialog;
    private DialogNode failedDialog;

    private HerbQuest quest;
    private PlayerData localPlayerData;

    private void OnDestroy()
    {
        if (localPlayerData != null)
        {
            localPlayerData.HerbQuestHandInProcessed -= OnHandInProcessed;
        }
    }

    /// <summary>
    /// Builds the dialog from the quest carried on the spawned Herb. Called by
    /// <see cref="HerbQuestSync"/> on the client once the synced quest data has arrived.
    /// </summary>
    public void BuildForQuest(HerbQuest herbQuest)
    {
        // Dialogs only exist on clients, just like the other NPC dialogs.
        if (!NetworkClient.active)
        {
            return;
        }

        quest = herbQuest;

        _startDialog = new DialogNode(DialogOptions.Click);

        if (quest == null || !quest.IsValid)
        {
            _startDialog.SetDialogText("Name's Herb. Normally I'd have a job for you, but I'm all out of cravings right now. Come find me again soon!");
            npcDialog.SetRootDialog(_startDialog);
            return;
        }

        returningDialog = new DialogNode(DialogOptions.Click);
        questDialog = new DialogNode(DialogOptions.YesNo);
        declinedDialog = new DialogNode(DialogOptions.Click);
        alreadyDoneDialog = new DialogNode(DialogOptions.Click);
        notEnoughDialog = new DialogNode(DialogOptions.Click);
        successDialog = new DialogNode(DialogOptions.Click);
        failedDialog = new DialogNode(DialogOptions.Click);

        string fishList = quest.DescribeFishes();
        int reward = quest.RewardCoins;

        _startDialog
            .SetNextClick(questDialog, ShowQuestOrAlreadyDone)
            .SetDialogText("Hey there! Herb's the name. I wander from shore to shore and every day I'm craving different fish.")
            .SetPlayerResponse("What are you craving today?");

        // Once the player has met Herb today the introduction is skipped and this leads
        // straight into the hand-in attempt (see ShowQuestOrAlreadyDone / RefreshRootDialog).
        returningDialog
            .SetNextClick(null, TryHandInQuest)
            .SetDialogText("Ah, back already! Let's see if you brought what I'm craving.");

        questDialog
            .SetDialogText($"Today I'd love to eat: {fishList}. All of them swim right here in this area! Bring them to me and I'll pay you {reward} fishcoins. Do you have them with you?")
            .SetNextYes(null, TryHandInQuest)
            .SetNextNo(declinedDialog);

        declinedDialog
            .SetDialogText("No rush! I'll be around for a while, then I'm off to the next spot.");

        alreadyDoneDialog
            .SetDialogText("You already brought me my fish today, they were delicious! Come look for me again, I'll be somewhere else with a new craving.");

        notEnoughDialog
            .SetDialogText($"Hmm, that's not everything on my list. I need: {fishList}. The good news: you can catch all of them right here!");

        successDialog
            .SetDialogText($"Mmm, exactly what I was craving! Here's your {reward} fishcoins, as promised. See you next time, somewhere else!");

        failedDialog
            .SetDialogText("Huh, something went wrong on my end... Let's try that again in a moment.");

        RefreshRootDialog();
    }

    private PlayerData GetLocalPlayerData()
    {
        if (localPlayerData == null && NetworkClient.connection?.identity != null)
        {
            localPlayerData = NetworkClient.connection.identity.GetComponentInChildren<PlayerData>();
            if (localPlayerData != null)
            {
                localPlayerData.HerbQuestHandInProcessed += OnHandInProcessed;
            }
        }
        return localPlayerData;
    }

    /// <summary>
    /// Points the dialog at the right entry node: the "already brought" line for a player
    /// who already completed today's quest, the returning-player hand-in node for one who
    /// only accepted it, or the introduction otherwise.
    /// </summary>
    private void RefreshRootDialog()
    {
        if (quest == null || !quest.IsValid || returningDialog == null)
        {
            npcDialog.SetRootDialog(_startDialog);
            return;
        }

        PlayerData playerData = GetLocalPlayerData();

        // Already handed in today's quest: open straight on the "already brought" line
        // instead of greeting the player as if they might still hand fishes in.
        if (playerData != null && playerData.HasCompletedHerbQuest(quest))
        {
            npcDialog.SetRootDialog(alreadyDoneDialog);
            return;
        }

        bool skipIntro = playerData != null && playerData.HasAcceptedHerbQuest(quest);
        npcDialog.SetRootDialog(skipIntro ? returningDialog : _startDialog);
    }

    private void ShowQuestOrAlreadyDone()
    {
        PlayerData playerData = GetLocalPlayerData();
        if (playerData == null || quest == null)
        {
            return;
        }

        if (playerData.HasCompletedHerbQuest(quest))
        {
            npcDialog.ShowDialog(alreadyDoneDialog);
            return;
        }

        // The player has now seen today's quest; remember it so Herb skips the
        // introduction next time and the dialog opens straight on the hand-in.
        if (!playerData.HasAcceptedHerbQuest(quest))
        {
            playerData.CmdAcceptHerbQuest();
            npcDialog.SetRootDialog(returningDialog);
        }
    }

    private void TryHandInQuest()
    {
        PlayerData playerData = GetLocalPlayerData();
        if (playerData == null || quest == null)
        {
            npcDialog.ShowDialog(failedDialog);
            return;
        }

        if (playerData.HasCompletedHerbQuest(quest))
        {
            npcDialog.ShowDialog(alreadyDoneDialog);
            return;
        }

        PlayerInventory inventory = NetworkClient.connection.identity.GetComponentInChildren<PlayerInventory>();
        if (inventory == null || !HerbQuestManager.HasAllQuestFishes(inventory, quest))
        {
            npcDialog.ShowDialog(notEnoughDialog);
            return;
        }

        // The server re-validates everything against its own current quest,
        // removes the fishes and rewards the coins.
        playerData.CmdHandInHerbQuest();
        npcDialog.ShowDialog(successDialog);
    }

    private void OnHandInProcessed(bool success, int rewardCoins)
    {
        // The success dialog is already showing optimistically, only correct it when the server refused
        if (!success && NpcDialog.DialogActive)
        {
            npcDialog.ShowDialog(failedDialog);
            return;
        }

        if (success)
        {
            // Quest is done for today: reopening Herb should go straight to the
            // "already brought" line instead of the returning-player hand-in node.
            npcDialog.SetRootDialog(alreadyDoneDialog);
        }
    }
}

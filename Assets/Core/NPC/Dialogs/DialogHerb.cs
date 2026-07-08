using Mirror;
using UnityEngine;

/// <summary>
/// Dialog of Herb, the wandering NPC that gives everybody the same daily quest:
/// bring him a mix of 2-4 fishes from the area he is standing in, get paid
/// 1 fishcoin per fish. He moves and finds a new quest every day at 04:00 Amsterdam time.
/// </summary>
public class DialogHerb : MonoBehaviour
{
    [SerializeField] NpcDialog npcDialog;

    private DialogNode _startDialog;
    private DialogNode questDialog;
    private DialogNode declinedDialog;
    private DialogNode alreadyDoneDialog;
    private DialogNode notEnoughDialog;
    private DialogNode successDialog;
    private DialogNode failedDialog;
    private DialogNode noQuestDialog;

    private int builtForQuestDayNumber = int.MinValue;
    private PlayerData localPlayerData;

    private void Awake()
    {
        // Dialogs only exist on clients, just like the other NPC dialogs
        if (NetworkServer.active)
        {
            return;
        }

        BuildDialog();
    }

    private void OnDestroy()
    {
        if (localPlayerData != null)
        {
            localPlayerData.DailyQuestHandInProcessed -= OnHandInProcessed;
        }
    }

    private void BuildDialog()
    {
        HerbQuest quest = HerbQuestManager.GetCurrentQuest();
        builtForQuestDayNumber = quest.questDayNumber;

        _startDialog = new DialogNode(DialogOptions.Click);
        npcDialog.SetRootDialog(_startDialog);

        if (!quest.IsValid)
        {
            _startDialog.SetDialogText("Name's Herb. Normally I'd have a job for you, but today the fish just aren't in season. Come find me tomorrow!");
            return;
        }

        questDialog = new DialogNode(DialogOptions.YesNo);
        declinedDialog = new DialogNode(DialogOptions.Click);
        alreadyDoneDialog = new DialogNode(DialogOptions.Click);
        notEnoughDialog = new DialogNode(DialogOptions.Click);
        successDialog = new DialogNode(DialogOptions.Click);
        failedDialog = new DialogNode(DialogOptions.Click);
        noQuestDialog = new DialogNode(DialogOptions.Click);

        string fishList = quest.DescribeFishes();
        int reward = quest.RewardCoins;

        _startDialog
            .SetNextClick(questDialog, ShowQuestOrAlreadyDone)
            .SetDialogText("Hey there! Herb's the name. I wander from shore to shore and every day I'm craving different fish.")
            .SetPlayerResponse("What are you craving today?");

        questDialog
            .SetDialogText($"Today I'd love to eat: {fishList}. All of them swim right here in this area! Bring them to me and I'll pay you {reward} fishcoins. Do you have them with you?")
            .SetNextYes(null, TryHandInQuest)
            .SetNextNo(declinedDialog);

        declinedDialog
            .SetDialogText("No rush! I'm here until four in the morning, then I'm off to the next spot.");

        alreadyDoneDialog
            .SetDialogText("You already brought me my fish today, they were delicious! Come look for me tomorrow, I'll be somewhere else with a new craving.");

        notEnoughDialog
            .SetDialogText($"Hmm, that's not everything on my list. I need: {fishList}. The good news: you can catch all of them right here!");

        successDialog
            .SetDialogText($"Mmm, exactly what I was craving! Here's your {reward} fishcoins, as promised. See you tomorrow, somewhere else!");

        failedDialog
            .SetDialogText("Huh, something went wrong on my end... Let's try that again in a moment.");

        noQuestDialog
            .SetDialogText("Ah, you just missed it, my craving changed with the clock! Talk to me again for the new list.");
    }

    private PlayerData GetLocalPlayerData()
    {
        if (localPlayerData == null && NetworkClient.connection?.identity != null)
        {
            localPlayerData = NetworkClient.connection.identity.GetComponentInChildren<PlayerData>();
            if (localPlayerData != null)
            {
                localPlayerData.DailyQuestHandInProcessed += OnHandInProcessed;
            }
        }
        return localPlayerData;
    }

    /// <summary>
    /// Rebuilds the dialog when Herb rolled over to a new quest while this scene was open
    /// </summary>
    private bool RefreshQuestIfRolledOver()
    {
        if (HerbQuestManager.CurrentQuestDayNumber() == builtForQuestDayNumber)
        {
            return false;
        }
        DialogNode rolledOverNode = noQuestDialog;
        BuildDialog();
        if (rolledOverNode != null)
        {
            npcDialog.ShowDialog(rolledOverNode);
        }
        return true;
    }

    private void ShowQuestOrAlreadyDone()
    {
        if (RefreshQuestIfRolledOver())
        {
            return;
        }

        PlayerData playerData = GetLocalPlayerData();
        HerbQuest quest = HerbQuestManager.GetCurrentQuest();
        if (playerData != null && playerData.HasCompletedDailyQuest(quest))
        {
            npcDialog.ShowDialog(alreadyDoneDialog);
        }
    }

    private void TryHandInQuest()
    {
        if (RefreshQuestIfRolledOver())
        {
            return;
        }

        PlayerData playerData = GetLocalPlayerData();
        if (playerData == null)
        {
            npcDialog.ShowDialog(failedDialog);
            return;
        }

        HerbQuest quest = HerbQuestManager.GetCurrentQuest();
        if (playerData.HasCompletedDailyQuest(quest))
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

        // The server re-validates everything, removes the fishes and rewards the coins
        playerData.CmdHandInDailyQuest();
        npcDialog.ShowDialog(successDialog);
    }

    private void OnHandInProcessed(bool success, int rewardCoins)
    {
        // The success dialog is already showing optimistically, only correct it when the server refused
        if (!success && NpcDialog.DialogActive)
        {
            npcDialog.ShowDialog(failedDialog);
        }
    }
}

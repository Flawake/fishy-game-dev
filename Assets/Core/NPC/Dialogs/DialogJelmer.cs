/*
Here’s a **fish game NPC dialogue** concept—friendly, slightly quirky, focused on **teaching about Japan** and **eating the fish instead of selling them**. You can drop this straight into a game.

---

**NPC Name:** Taro the Fisher-Scholar

**[Player approaches]**

**Taro:**
Ah! You’ve come at a good tide. See this fish?
Most players would sell it.
But in Japan… we *respect* the fish first.

**[Dialogue Option 1: “Respect how?”]**

**Taro:**
By knowing where it comes from.
This mackerel swam the cold currents near Hokkaido.
The water shapes its fat, the fat shapes the flavor.
Money cannot teach you that—but eating it can.

**[Dialogue Option 2: “I could make a lot of coins from this.”]**

**Taro:**
Coins disappear.
Experience stays in your bones.
In Japan, food is not just fuel—it is memory.
If you eat this fish, you learn its story.

**[Player chooses to eat the fish]**

**Taro:**
Good choice. Sit. Eat slowly.
Notice the texture? That is why we slice sashimi *against* the grain.
Every cut has a reason. Every meal is a lesson.

**[Player gains buff: “Taste of Japan”]**
*+Knowledge, +Focus, +Cultural Insight*

**Taro:**
Tomorrow, I will teach you about rice.
But first—finish chewing. Rushing dishonors the ocean.

**[Player chooses to sell the fish]**

**Taro:**
Hmm. The market thanks you.
But you have learned nothing today.
Come back when you are hungry for more than coins.

*/

using Mirror;
using UnityEngine;

public class DialogJelmer : MonoBehaviour
{
    [SerializeField] NpcDialog npcDialog;

    private DialogNode _startDialog;
    private DialogNode dialogOne;
    private DialogNode dialogTwo;

    private DialogNode dialogThree;
    private DialogNode dialogFour;
    private DialogNode dialogFive;

    private DialogNode dialogSix;

    private void Awake()
    {
        if (NetworkServer.active)
        {
            return;
        }

        _startDialog = new DialogNode(
            DialogOptions.Click
        );
        npcDialog.SetRootDialog(_startDialog);

        dialogOne = new DialogNode(
            DialogOptions.Click
        );

        dialogTwo = new DialogNode(
            DialogOptions.Click
        );

        dialogThree = new DialogNode(
            DialogOptions.Click
        );

        dialogFour = new DialogNode(
            DialogOptions.Click
        );

        dialogFive = new DialogNode(
            DialogOptions.YesNo
        );

        dialogSix = new DialogNode(
            DialogOptions.Click
        );

        _startDialog
            .SetNextClick(dialogOne, null)
            .SetDialogText("Ah! You've come at a good tide. See this fish? Most players would sell it. But in Japan… we respect the fish first.")
            .SetPlayerResponse("Respect how?");

        dialogOne
            .SetNextClick(dialogTwo, null)
            .SetDialogText("By knowing where it comes from. This mackerel swam the cold currents near Hokkaido. The water shapes its fat, the fat shapes the flavor. Money cannot teach you that—but eating it can.")
            .SetPlayerResponse("I could make a lot of bucks from this.");

        dialogTwo
            .SetNextClick(dialogThree, null)
            .SetDialogText("Coins disappear. Experience stays in your bones. In Japan, food is not just fuel—it is memory. If you eat this fish, you learn its story.")
            .SetPlayerResponse("I'd love to try that fish out");

        dialogThree
            .SetNextClick(dialogFour, null)
            .SetDialogText("Good choice. Sit. Eat slowly. Notice the texture? That is why we slice sashimi *against* the grain. Every cut has a reason. Every meal is a lesson.")
            .SetPlayerResponse("Oh, yes. I feel the power of the fish growing inside me.");

        dialogFour
            .SetNextClick(dialogFive, SetSellDialogText)
            .SetDialogText( "Tomorrow, I will teach you about rice. But first—finish chewing. Rushing dishonors the ocean. Are you willing to sell all of your fish?")
            .SetDialogText("I can't wait to hear about the rice story. But how much will you pay me for my fish?");

        dialogFive.SetNextYes(dialogSix, SellAllFish);

        dialogSix.SetDialogText("Thank you, it was nice doing buisness with you");
    }

    private void SetSellDialogText()
    {
        SellFish sellFishScript = NetworkClient.connection.identity.GetComponentInChildren<SellFish>();
        dialogFive.SetDialogText($"I can give you {sellFishScript.GetTotalFishValue()} for {sellFishScript.GetFishAmount()} fishes. But my buddy here might be able to give you a better price");
    }

    private void SellAllFish()
    {
        NetworkClient.connection.identity.GetComponentInChildren<SellFish>().SellAllFishAtMarket();
    }
}

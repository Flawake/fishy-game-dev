using Mirror;
using UnityEngine;

public class DialogErnesto : MonoBehaviour
{
    [SerializeField] NpcDialog npcDialog;

    private DialogNode _startDialog;

    private void Awake()
    {
        if (NetworkServer.active)
        {
            return;
        }

        /*_startDialog = new DialogNode(
            "Good morning, it is morning, right?. Whatever. Do you want to sell some fish?",
            DialogOptions.YesNo
        );*/
        _startDialog = new DialogNode(
            "I can't buy fish at the moment, please come back later",
            DialogOptions.Click
        );

        npcDialog.SetRootDialog(_startDialog);

        _startDialog
            .SetNextYes(null, OpenSellFishUI)
            .SetNextNo(null);
    }

    private void OpenSellFishUI()
    {
        NetworkClient.connection.identity.GetComponentInChildren<SellFishUIManager>().OpenSellFishUI();
    }
}

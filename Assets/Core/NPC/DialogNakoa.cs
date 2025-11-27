using Mirror;
using UnityEngine;

public class DialogNakoa : MonoBehaviour
{
    [SerializeField] NpcDialog npcDialog;

    private DialogNode _startDialog;

    private void Awake()
    {
        if (NetworkServer.active)
        {
            return;
        }

        _startDialog = new DialogNode(
            "This beach keeps secrets better than people do.",
            DialogOptions.Click
        );

        npcDialog.SetRootDialog(_startDialog);
    }
}

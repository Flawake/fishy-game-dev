using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum DialogResponse
{
    Click,
    Yes,
    No,
    YesAndNo,
    End,
}

public class Dialog
{
    public int DialogID { get; }
    public string Text { get; }
    public DialogResponse ResponseType { get; }
    public int? NextClickDialog { get; }
    public int? NextYesDialog { get; }
    public int? NextNoDialog { get; }

    public Action OnClick { get; }
    public Action OnYes { get; }
    public Action OnNo { get; }

    public Dialog(
        int dialogID,
        string text,
        DialogResponse responseType,
        int? nextClickDialog = null,
        int? nextYesDialog = null,
        int? nextNoDialog = null,
        Action onClick = null,
        Action onYes = null,
        Action onNo = null)
    {
        DialogID = dialogID;
        Text = text;
        ResponseType = responseType;
        NextClickDialog = nextClickDialog;
        NextYesDialog = nextYesDialog;
        NextNoDialog = nextNoDialog;
        OnClick = onClick;
        OnYes = onYes;
        OnNo = onNo;
    }
}

public class NpcDialog : MonoBehaviour
{
    [SerializeField] private GameObject canvasObject;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private GameObject clickIcon;
    [SerializeField] private GameObject yesButton;
    [SerializeField] private GameObject noButton;
    
    public static bool DialogActive;
    private Dialog _currentDialog;
    
    private Dictionary<int, Dialog> _dialogs;
    
    public void SetDialogs(Dictionary<int, Dialog> newDialogs)
    {
        _dialogs = newDialogs;
    }
    
    public void StartDialog(Camera eventCamera)
    {
        canvasObject.SetActive(true);
        canvas.worldCamera = eventCamera;
        if (_dialogs == null)
        {
            Debug.LogWarning("Dialogs of the npc are not set!");
            return;
        }
        DialogActive = true;
        ShowNextDialog(_dialogs[1]);
        PlayerController.OnMouseClickedAction += OnMouseClicked;
    }

    private void EndDialog()
    {
        canvasObject.SetActive(false);
        DialogActive = false;
    }

    private void ShowNextDialog(Dialog nextDialog)
    {
        SetAppropiateAnswers(nextDialog.ResponseType);
        dialogText.text = nextDialog.Text;
        _currentDialog = nextDialog;
    }

    private void OnMouseClicked()
    {
        if (_currentDialog.ResponseType == DialogResponse.End)
        {
            _currentDialog.OnClick?.Invoke();
            EndDialog();
            return;
        }
        if (_currentDialog.ResponseType == DialogResponse.Click)
        {
            _currentDialog.OnClick?.Invoke();
            if (_currentDialog.NextClickDialog.HasValue && _dialogs.TryGetValue(_currentDialog.NextClickDialog.Value, out Dialog dialog))
            {
                ShowNextDialog(dialog);
            }
            else
            {
                EndDialog();
            }
        }
    }
    
    //Called from button ingame
    public void YesClicked(Dialog nextDialog)
    {
        _currentDialog.OnYes?.Invoke();
        if (_currentDialog.NextYesDialog.HasValue && _dialogs.TryGetValue(_currentDialog.NextYesDialog.Value, out Dialog dialog))
        {
            ShowNextDialog(dialog);
        }
        else
        {
            EndDialog();
        }
    }
    
    //Called from button ingame
    public void NoClicked(Dialog nextDialog)
    {
        _currentDialog.OnNo?.Invoke();
        if (_currentDialog.NextNoDialog.HasValue && _dialogs.TryGetValue(_currentDialog.NextNoDialog.Value, out Dialog dialog))
        {
            ShowNextDialog(dialog);
        }
        else
        {
            EndDialog();
        }
    }

    private void SetAppropiateAnswers(DialogResponse response)
    {
        switch (response)
        {
            case DialogResponse.Click:
                yesButton.SetActive(false);
                noButton.SetActive(false);
                clickIcon.SetActive(true);
                break;
            case DialogResponse.Yes:
                yesButton.SetActive(true);
                noButton.SetActive(false);
                clickIcon.SetActive(false);
                break;
            case DialogResponse.No:
                yesButton.SetActive(false);
                noButton.SetActive(true);
                clickIcon.SetActive(false);
                break;
            case DialogResponse.YesAndNo:
                yesButton.SetActive(true);
                noButton.SetActive(true);
                clickIcon.SetActive(false);
                break;
        }
    }
}

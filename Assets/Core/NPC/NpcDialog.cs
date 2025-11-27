using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Represents a single dialog node with text, options, and callbacks.
/// </summary>
public class DialogNode
{
    public string Text { get; }
    public DialogOptions Options { get; }
    public DialogNode NextClick { get; set; }
    public DialogNode NextYes { get; set; }
    public DialogNode NextNo { get; set; }
    
    public Action OnClick { get; set; }
    public Action OnYes { get; set; }
    public Action OnNo { get; set; }

    public DialogNode(string text, DialogOptions options = DialogOptions.Click)
    {
        Text = text;
        Options = options;
    }

    /// <summary>
    /// Fluent API: Set the next dialog when clicking/continuing
    /// </summary>
    public DialogNode SetNextClick(DialogNode next, Action onClick = null)
    {
        NextClick = next;
        OnClick = onClick;
        return this;
    }

    /// <summary>
    /// Fluent API: Set the next dialog when clicking Yes
    /// </summary>
    public DialogNode SetNextYes(DialogNode next, Action onYes = null)
    {
        NextYes = next;
        OnYes = onYes;
        return this;
    }

    /// <summary>
    /// Fluent API: Set the next dialog when clicking No
    /// </summary>
    public DialogNode SetNextNo(DialogNode next, Action onNo = null)
    {
        NextNo = next;
        OnNo = onNo;
        return this;
    }

    /// <summary>
    /// Fluent API: Set callbacks without changing next dialog
    /// </summary>
    public DialogNode OnClickAction(Action action)
    {
        OnClick = action;
        return this;
    }

    public DialogNode OnYesAction(Action action)
    {
        OnYes = action;
        return this;
    }

    public DialogNode OnNoAction(Action action)
    {
        OnNo = action;
        return this;
    }
}

/// <summary>
/// Defines what interaction options are available for a dialog node
/// </summary>
public enum DialogOptions
{
    /// <summary>Simple click to continue or end</summary>
    Click,
    /// <summary>Yes button only</summary>
    Yes,
    /// <summary>No button only</summary>
    No,
    /// <summary>Both Yes and No buttons</summary>
    YesNo
}

/// <summary>
/// Manages the dialog UI and flow for NPCs
/// </summary>
public class NpcDialog : MonoBehaviour
{
    [SerializeField] private GameObject canvasObject;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text dialogText;
    [SerializeField] private GameObject clickIcon;
    [SerializeField] private GameObject yesButton;
    [SerializeField] private GameObject noButton;
    
    public static bool DialogActive { get; private set; }
    
    private DialogNode _currentNode;
    private DialogNode _rootNode;

    /// <summary>
    /// Sets the root dialog node to start from
    /// </summary>
    public void SetRootDialog(DialogNode rootNode)
    {
        _rootNode = rootNode;
    }

    /// <summary>
    /// Starts the dialog system with the root node
    /// </summary>
    public void StartDialog(Camera eventCamera)
    {
        if (_rootNode == null)
        {
            Debug.LogWarning($"[NpcDialog] No root dialog set on {gameObject.name}!");
            return;
        }

        canvasObject.SetActive(true);
        canvas.worldCamera = eventCamera;
        DialogActive = true;
        
        ShowDialog(_rootNode);
        PlayerController.OnMouseClickedAction += HandleMouseClick;
    }

    /// <summary>
    /// Shows a specific dialog node (can be called externally for dynamic dialog changes)
    /// </summary>
    public void ShowDialog(DialogNode node)
    {
        if (node == null)
        {
            Debug.LogWarning("[NpcDialog] Attempted to show null dialog node!");
            EndDialog();
            return;
        }

        _currentNode = node;
        dialogText.text = node.Text;
        UpdateUIForOptions(node.Options);
    }

    private void EndDialog()
    {
        canvasObject.SetActive(false);
        DialogActive = false;
        PlayerController.OnMouseClickedAction -= HandleMouseClick;
        _currentNode = null;
    }

    private void HandleMouseClick()
    {
        if (_currentNode == null) return;

        // Handle click-based navigation
        if (_currentNode.Options == DialogOptions.Click || _currentNode.Options == DialogOptions.YesNo)
        {
            // For YesNo, clicking doesn't advance - must use buttons
            if (_currentNode.Options == DialogOptions.YesNo)
            {
                return;
            }

            // Store the current node before callback
            DialogNode nodeBeforeCallback = _currentNode;

            // Execute callback - it may change the dialog
            _currentNode.OnClick?.Invoke();

            // If callback changed the dialog, don't do automatic navigation
            if (_currentNode != nodeBeforeCallback)
            {
                return;
            }

            // Otherwise, proceed with automatic navigation
            if (_currentNode.NextClick != null)
            {
                ShowDialog(_currentNode.NextClick);
            }
            else
            {
                EndDialog();
            }
        }
    }

    /// <summary>
    /// Called by the Yes button in the UI
    /// </summary>
    public void OnYesButtonClicked()
    {
        if (_currentNode == null) return;

        if (_currentNode.Options != DialogOptions.Yes && _currentNode.Options != DialogOptions.YesNo)
        {
            Debug.LogWarning("[NpcDialog] Yes button clicked but not available for current dialog!");
            return;
        }

        // Store the current node before callback
        DialogNode nodeBeforeCallback = _currentNode;

        // Execute callback - it may change the dialog
        _currentNode.OnYes?.Invoke();

        // If callback changed the dialog, don't do automatic navigation
        if (_currentNode != nodeBeforeCallback)
        {
            return;
        }

        // Otherwise, proceed with automatic navigation
        if (_currentNode.NextYes != null)
        {
            ShowDialog(_currentNode.NextYes);
        }
        else
        {
            EndDialog();
        }
    }

    /// <summary>
    /// Called by the No button in the UI
    /// </summary>
    public void OnNoButtonClicked()
    {
        if (_currentNode == null) return;

        if (_currentNode.Options != DialogOptions.No && _currentNode.Options != DialogOptions.YesNo)
        {
            Debug.LogWarning("[NpcDialog] No button clicked but not available for current dialog!");
            return;
        }

        // Store the current node before callback
        DialogNode nodeBeforeCallback = _currentNode;

        // Execute callback - it may change the dialog
        _currentNode.OnNo?.Invoke();

        // If callback changed the dialog, don't do automatic navigation
        if (_currentNode != nodeBeforeCallback)
        {
            return;
        }

        // Otherwise, proceed with automatic navigation
        if (_currentNode.NextNo != null)
        {
            ShowDialog(_currentNode.NextNo);
        }
        else
        {
            EndDialog();
        }
    }

    private void UpdateUIForOptions(DialogOptions options)
    {
        switch (options)
        {
            case DialogOptions.Click:
                yesButton.SetActive(false);
                noButton.SetActive(false);
                clickIcon.SetActive(true);
                break;

            case DialogOptions.Yes:
                yesButton.SetActive(true);
                noButton.SetActive(false);
                clickIcon.SetActive(false);
                break;

            case DialogOptions.No:
                yesButton.SetActive(false);
                noButton.SetActive(true);
                clickIcon.SetActive(false);
                break;

            case DialogOptions.YesNo:
                yesButton.SetActive(true);
                noButton.SetActive(true);
                clickIcon.SetActive(false);
                break;
        }
    }
}

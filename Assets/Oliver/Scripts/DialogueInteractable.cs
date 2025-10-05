using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class DialogueInteractable : Interactable
{
    [Header("Dialogue")]
    public DialogueRunner runner;
    public string startNode;

    [Header("Lock Player While Talking")]
    // drag the scripts to be disabled during dialogue
    public Behaviour[] componentsToDisable;
    public bool unlockCursorDuringDialogue = true;

    private PlayerInteraction _lastInteractor;
    private readonly List<Behaviour> _disabledThisConversation = new();

    void Reset()
    {
        runner = FindFirstObjectByType<DialogueRunner>();
    }

    void OnEnable()
    {
        if (!runner) runner = FindFirstObjectByType<DialogueRunner>();
        if (runner) runner.onDialogueComplete.AddListener(OnDialogueComplete);
    }

    void OnDisable()
    {
        if (runner) runner.onDialogueComplete.RemoveListener(OnDialogueComplete);
        // fail-safe: if this gets disabled mid-convo, try to unlock
        ForceUnlockControls();
    }

    public override void Interact(PlayerInteraction interactor)
    {
        base.Interact(interactor); // still fire UnityEvent if anything is selected

        if (!runner)
        {
            Debug.LogWarning("[DialogueInteractable] No DialogueRunner found in scene.");
            return;
        }
        if (runner.IsDialogueRunning)
            return; // prevent overlap

        _lastInteractor = interactor;

        // LOCK controls
        LockControls(interactor);

        // start the node
        var node = string.IsNullOrEmpty(startNode) ? runner.startNode : startNode;
        runner.StartDialogue(node);
    }

    void OnDialogueComplete()
    {
        // unlock controls on completion
        ForceUnlockControls();
        _lastInteractor = null;
    }

    // ----- helpers -----

    void LockControls(PlayerInteraction interactor)
    {
        _disabledThisConversation.Clear();

        if (componentsToDisable != null && componentsToDisable.Length > 0)
        {
            foreach (var b in componentsToDisable)
            {
                if (b && b.enabled)
                {
                    b.enabled = false;
                    _disabledThisConversation.Add(b);
                }
            }
        }
        else if (interactor) // fallback
        {
            var move =
                (Behaviour)interactor.GetComponent("PlayerMovement");

            if (move && move.enabled)
            {
                move.enabled = false;
                _disabledThisConversation.Add(move);
            }
        }

        if (unlockCursorDuringDialogue)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void ForceUnlockControls()
    {
        // re-enable everything we actually disabled for this conversation
        for (int i = 0; i < _disabledThisConversation.Count; i++)
        {
            var b = _disabledThisConversation[i];
            if (b) b.enabled = true;
        }
        _disabledThisConversation.Clear();

        // restore cursor for gameplay
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }
}
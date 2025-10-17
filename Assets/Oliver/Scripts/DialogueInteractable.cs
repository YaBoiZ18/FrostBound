using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Yarn.Unity;

public class DialogueInteractable : Interactable
{
    [Header("Dialogue")]
    public DialogueRunner runner;
    public string startNode;

    [Header("Lock Player While Talking")]
    public Behaviour[] componentsToDisable;
    public bool unlockCursorDuringDialogue = true;

    [Header("Testing Overrides")]
    public bool overrideIllnessForTesting = false;
    public string illnessOverrideKey = "";
    public bool onlyAffectsYarn = true;

    public InMemoryVariableStorage storage;
    public string illnessName = "$illness"; // Yarn variable key for illness
    public NPCState npcState;

    private PlayerInteraction _lastInteractor;
    private readonly List<Behaviour> _disabledThisConversation = new();

    private void Awake()
    {
        if (!storage) storage = FindFirstObjectByType<InMemoryVariableStorage>();
        if (!npcState) npcState = GetComponent<NPCState>();
    }

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
        // decide illness (NPC’s or test override)
        string chosenIllness = npcState ? npcState.illnessKey : "";
        if (overrideIllnessForTesting && !string.IsNullOrWhiteSpace(illnessOverrideKey))
            chosenIllness = illnessOverrideKey.Trim();

        // fallback: assign if missing via registry
        if (string.IsNullOrWhiteSpace(chosenIllness) && npcState && !string.IsNullOrWhiteSpace(npcState.npcId) && IllnessRegistry.IR != null)
        {
            chosenIllness = IllnessRegistry.IR.AssignIfMissing(npcState.npcId) ?? "";
            npcState.illnessKey = chosenIllness;
        }

        // push Yarn vars once
        if (storage)
        {
            if (runner && runner.VariableStorage != storage)
            {
                runner.VariableStorage = storage;
            }
            if (runner && storage.Program == null && runner.YarnProject != null)
            {
                storage.Program = runner.YarnProject.Program;
            }

            if (npcState) storage.SetValue("$npc_id", npcState.npcId);

            // normalize keys
            var customKey = string.IsNullOrEmpty(illnessName)
                ? "$illness"
                : (illnessName.StartsWith("$") ? illnessName : "$" + illnessName);

            // always set canonical $illness used by Yarn scripts
            storage.SetValue("$illness", chosenIllness ?? string.Empty);
            Debug.Log($"[DialogueInteractable] Set $illness = '{chosenIllness}'", this);

            // if a custom key differs, mirror to it too (keeps backward compat)
            if (!customKey.Equals("$illness"))
            {
                storage.SetValue(customKey, chosenIllness ?? string.Empty);
                Debug.Log($"[DialogueInteractable] Set {customKey} = '{chosenIllness}'", this);
            }
        }

#if UNITY_EDITOR
        if (!onlyAffectsYarn && npcState && IllnessRegistry.IR != null && !string.IsNullOrEmpty(chosenIllness))
            IllnessRegistry.IR.DebugSetIllness(npcState.npcId, chosenIllness);
#endif

        base.Interact(interactor);

        if (!runner) { Debug.LogWarning("[DialogueInteractable] No DialogueRunner found."); return; }
        if (runner.IsDialogueRunning) return;

        _lastInteractor = interactor;
        LockControls(interactor);

        var node = string.IsNullOrEmpty(startNode) ? runner.startNode : startNode;
        runner.StartDialogue(node);
    }

    void OnDialogueComplete()
    {
        // If a command-driven internal jump is happening, don't unlock controls here.
        if (DialogueBridge.IsInternalGotoInProgress) return;

        // unlock controls on real completion
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
        for (int i = 0; i < _disabledThisConversation.Count; i++)
            if (_disabledThisConversation[i]) _disabledThisConversation[i].enabled = true;
        _disabledThisConversation.Clear();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
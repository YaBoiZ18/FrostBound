using UnityEngine;
using Yarn.Unity;

public class DialogueBootstrapper : MonoBehaviour
{
    [Header("Yarn")]
    public DialogueRunner runner;
    public string startNode = "Start"; // starting node

    [Header("Lock Player While Talking")]
    public MonoBehaviour[] movementScriptsToDisable; // pick scripts to disable on first load
    public bool unlockCursorDuringDialogue = true;

    [Header("Initial Variables")]
    public bool TD_GoodMorning = true;

    void Awake()
    {
        if (!runner) runner = FindFirstObjectByType<DialogueRunner>();
    }

    void OnEnable()
    {
        if (runner) runner.onDialogueComplete.AddListener(OnDialogueComplete);
    }

    void OnDisable()
    {
        if (runner) runner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }

    void Start()
    {
        if (!runner) { Debug.LogWarning("No DialogueRunner found."); return; }

        // Lock player BEFORE starting dialogue
        foreach (var mb in movementScriptsToDisable)
            if (mb) mb.enabled = false;

        if (unlockCursorDuringDialogue)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        var node = string.IsNullOrEmpty(startNode) ? runner.startNode : startNode;
        runner.StartDialogue(node);
    }

    void OnDialogueComplete()
    {
        // re-enable player control after conversation
        foreach (var mb in movementScriptsToDisable)
            if (mb) mb.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        TD_GoodMorning = false;
    }
}
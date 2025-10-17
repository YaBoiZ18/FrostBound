using System.Collections;
using System.Reflection;
using UnityEngine;
using Yarn.Unity;

public class DialogueBridge : MonoBehaviour
{
    [Header("Yarn")]
    public DialogueRunner runner;
    public InMemoryVariableStorage storage;
    public string startNode = "Start_Radio";

    [Header("Lock Player While Talking")]
    public MonoBehaviour[] movementScriptsToDisable;
    public bool unlockCursorDuringDialogue = true;

    [Header("Initial Variables")]
    public bool TD_GoodMorning = false;

    static bool s_registered = false;

    // Indicates a command-driven internal jump is in progress; used to suppress completion side-effects
    public static bool IsInternalGotoInProgress { get; private set; }

    // Track if this bridge actually changed input state so we only undo what we did
    bool _bridgeDisabledMovement;
    bool _bridgeChangedCursor;

    void Awake()
    {
        if (!runner) runner = FindFirstObjectByType<DialogueRunner>();
        if (!runner) { Debug.LogError("DialogueBridge: no DialogueRunner."); return; }

        if (!storage) storage = FindFirstObjectByType<InMemoryVariableStorage>();

        // Ensure the DialogueRunner uses the same VariableStorage instance that other code writes to
        if (storage != null)
        {
            runner.VariableStorage = storage;
            Debug.Log($"DialogueBridge: assigned runner.VariableStorage -> {storage.name}");
        }
        else
        {
            Debug.LogWarning("DialogueBridge: no InMemoryVariableStorage found; Yarn variable lookups may fail.");
        }

        if (!s_registered)
        {
            s_registered = true;

            // function: ill_node(ill, part) -> "ill_part"
            runner.AddFunction<string, string, string>("ill_node", (ill, part) => $"{ill}_{part}");

            // command: <<goto_node Some_Node>>
            runner.AddCommandHandler<string>("goto_node", CoGoto);

            DontDestroyOnLoad(gameObject);
        }
    }
    
    void OnEnable() { if (runner) runner.onDialogueComplete.AddListener(OnDialogueComplete); }
    void OnDisable() { if (runner) runner.onDialogueComplete.RemoveListener(OnDialogueComplete); }

    void Start()
    {
        if (!runner) return;
        StartCoroutine(CoStartDialogueSafely());
    }

    IEnumerator CoStartDialogueSafely()
    {
        // Only mark we changed movement if we actually had something to disable
        _bridgeDisabledMovement = false;
        if (movementScriptsToDisable != null && movementScriptsToDisable.Length > 0)
        {
            foreach (var mb in movementScriptsToDisable) if (mb && mb.enabled) { mb.enabled = false; _bridgeDisabledMovement = true; }
        }

        if (unlockCursorDuringDialogue)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _bridgeChangedCursor = true;
        }
        else
        {
            _bridgeChangedCursor = false;
        }

        // allow one frame for Yarn to finish binding
        yield return null;

        if (!HasYarnProjectAssigned(runner))
        {
            Debug.LogError("DialogueRunner has no YarnProject assigned.");
            yield break;
        }

        var node = string.IsNullOrEmpty(startNode) ? runner.startNode : startNode;
        runner.StartDialogue(node);
    }

    void OnDialogueComplete()
    {
        // If this "completion" is caused by an internal goto, do not alter input state
        if (IsInternalGotoInProgress) return;

        if (_bridgeDisabledMovement)
        {
            foreach (var mb in movementScriptsToDisable) if (mb) mb.enabled = true;
            _bridgeDisabledMovement = false;
        }

        if (_bridgeChangedCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _bridgeChangedCursor = false;
        }
    }

    IEnumerator CoGoto(string node)
    {
        Debug.Log($"DialogueBridge.CoGoto node='{node}'");

        if (!runner || string.IsNullOrEmpty(node)) yield break;

        // Resolve Yarn variable token like "$target" to an actual node name
        string resolvedNode = node;
        var vs = runner.VariableStorage;
        if (vs != null && resolvedNode.Length > 0 && resolvedNode[0] == '$')
        {
            if (vs.TryGetValue<string>(resolvedNode, out var valueFromVar))
            {
                resolvedNode = valueFromVar ?? "";
                Debug.Log($"DialogueBridge: resolved {node} -> '{resolvedNode}'");
            }
            else
            {
                Debug.LogWarning($"DialogueBridge: variable {node} not found or not a string in VariableStorage");
            }
        }

        if (string.IsNullOrEmpty(resolvedNode))
        {
            Debug.LogWarning("DialogueBridge.CoGoto: resolved node name is empty; aborting.");
            yield break;
        }

        // Suppress completion side-effects during the internal jump
        IsInternalGotoInProgress = true;

        // Stop current dialogue, wait for it to fully end, then start the next node
        if (runner.IsDialogueRunning) runner.Stop();
        while (runner.IsDialogueRunning) yield return null;

        // One extra frame lets presenters and UI fully tear down before we restart
        yield return null;

        runner.StartDialogue(resolvedNode);

        // One frame to let StartDialogue kick off and presenters initialise
        yield return null;

        // Keep the same "talking" state (cursor visible etc.) – do not override here,
        // because the owning interactable maintains control locks for player input.

        IsInternalGotoInProgress = false;
    }

    static bool HasYarnProjectAssigned(DialogueRunner r)
    {
        if (r == null) return false;
        var t = r.GetType();

        var prop = t.GetProperty("Project", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null) return prop.GetValue(r) != null;

        var field = t.GetField("yarnProject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null) return field.GetValue(r) != null;

        return true;
    }
}
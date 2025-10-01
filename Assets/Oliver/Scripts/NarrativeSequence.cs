using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TMPDialogueSequence : MonoBehaviour
{
    [Header("Wiring")]
    public NarrativeTextReveal revealer;
    public CanvasGroup continueIndicator;

    [Header("Content")]
    [TextArea(3, 10)] public string[] pages;
    public bool playOnStart = true;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;
    public int advanceMouseButton = 0; // 0 = LMB
    public bool useUnscaledTime = true;

    [Header("Continue Indicator")]
    public bool blinkIndicator = true;
    public float blinkSpeed = 4f;

    public System.Action<int> OnPageShown;
    public System.Action OnSequenceFinished;

    Coroutine _runner;

    void Reset() { if (!revealer) revealer = GetComponent<NarrativeTextReveal>(); }
    void Start() { if (playOnStart) StartSequence(); }

    public void StartSequence()
    {
        if (revealer == null) revealer = GetComponent<NarrativeTextReveal>();
        if (revealer == null || pages == null || pages.Length == 0) return;

        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(CoRun());
    }

    IEnumerator CoRun()
    {
        if (continueIndicator) { continueIndicator.alpha = 0f; continueIndicator.gameObject.SetActive(true); }

        for (int i = 0; i < pages.Length; i++)
        {
            // **Input release gate** so prior press doesn't bleed into this page
            yield return WaitForRelease();

            revealer.ResetFastForward();      // fresh page speed
            revealer.Play(pages[i]);
            OnPageShown?.Invoke(i);

            // While revealing: first NEW press  fast-forward (no advance)
            while (revealer.IsRevealing)
            {
                if (PressedThisFrame())
                    revealer.FastForward();
                yield return null;
            }

            // Completed: require a NEW press to advance
            if (continueIndicator)
            {
                float t = 0f;
                continueIndicator.alpha = 1f;

                // First, make sure the fast-forward press is released
                yield return WaitForRelease();

                // Then wait for a fresh press to advance
                while (!PressedThisFrame())
                {
                    if (blinkIndicator)
                    {
                        t += DT;
                        continueIndicator.alpha = 0.5f + 0.5f * Mathf.PingPong(t * blinkSpeed, 1f);
                    }
                    yield return null;
                }
                continueIndicator.alpha = 0f;
            }
            else
            {
                // Without an indicator, still gate for release then wait for a new press
                yield return WaitForRelease();
                yield return new WaitUntil(PressedThisFrame);
            }
        }

        if (continueIndicator) continueIndicator.gameObject.SetActive(false);
        OnSequenceFinished?.Invoke();
        _runner = null;
    }

    IEnumerator WaitForRelease()
    {
        // Wait until both inputs are fully UP at least once
        while (Input.GetKey(advanceKey) || (advanceMouseButton >= 0 && Input.GetMouseButton(advanceMouseButton)))
            yield return null;
    }

    bool PressedThisFrame()
    {
        if (Input.GetKeyDown(advanceKey)) return true;
        if (advanceMouseButton >= 0 && Input.GetMouseButtonDown(advanceMouseButton)) return true;
        return false;
    }

    float DT => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
}
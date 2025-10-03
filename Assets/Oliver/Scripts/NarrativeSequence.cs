using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Finish Button")]
    public Button continueButton;                 // assign in Inspector
    public bool hideContinueButtonUntilDone = true;
    public CanvasGroup continueButtonCanvasGroup; // use this to fade in/out
    public float continueButtonFadeSeconds = 0.35f;
    public bool autoSelectContinueButton = true;  // for gamepad/keyboard navigation

    public System.Action<int> OnPageShown;
    public System.Action OnSequenceFinished;

    Coroutine _runner;

    void Reset() { if (!revealer) revealer = GetComponent<NarrativeTextReveal>(); }
    void Start() { if (playOnStart) StartSequence(); }

    public void StartSequence()
    {
        if (revealer == null) revealer = GetComponent<NarrativeTextReveal>();
        if (revealer == null || pages == null || pages.Length == 0) return;

        // warn if accidentally assigned the same CanvasGroup to both.
        if (continueButtonCanvasGroup && continueIndicator && ReferenceEquals(continueButtonCanvasGroup, continueIndicator))
        {
            Debug.LogWarning("[TMPDialogueSequence] CButtonCanvasGroup is the SAME component as continueIndicator. " +
                             "Put the finish button on a different GameObject/CanvasGroup so it doesn't blink every page.");
        }

        // hide the continue button up front (if desired).
        HideContinueButtonNow();

        // hide the continue button until all text has been revealed
        if (continueButton && hideContinueButtonUntilDone)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.interactable = false;
            if (continueButtonCanvasGroup) continueButtonCanvasGroup.alpha = 0f;
        }

        if (_runner != null) StopCoroutine(_runner);
        _runner = StartCoroutine(CoRun());

    }

    IEnumerator CoRun()
    {
        if (continueIndicator)
        {
            continueIndicator.alpha = 0f;
            continueIndicator.gameObject.SetActive(true);
        }

        int pageCount = pages.Length - 1;

        for (int i = 0; i < pages.Length; i++)
        {
            // make sure the continue indicator is hidden at start of each page
            if (i < pageCount)
            {
                HideContinueButtonNow();
            }

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

            if (i < pageCount)
            {
                if (continueIndicator)
                {
                    float t = 0f;
                    continueIndicator.alpha = 1f;

                    // first make sure the fast-forward press is released
                    yield return WaitForRelease();

                    // then wait for a fresh press to advance
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
                    // with no indicator, still gate for release then wait for a new press
                    yield return WaitForRelease();
                    yield return new WaitUntil(PressedThisFrame);
                }
            }
        }

        if (continueIndicator) continueIndicator.gameObject.SetActive(false);

        // make sure any advance key/mouse press is released before we show the button
        yield return WaitForRelease();

        // SHOW/ENABLE the finish button now
        ShowContinueButtonNow();

        OnSequenceFinished?.Invoke();
        _runner = null;

        //    if (continueButton)
        //    {
        //        continueButton.gameObject.SetActive(true);

        //        // fade-in
        //        if (continueButtonCanvasGroup && hideContinueButtonUntilDone && continueButtonFadeSeconds > 0f)
        //        {
        //            float t = 0f, dur = continueButtonFadeSeconds;
        //            while (t < dur)
        //            {
        //                t += DT;
        //                continueButtonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / Mathf.Max(0.0001f, dur));
        //                yield return null;
        //            }
        //            continueButtonCanvasGroup.alpha = 1f;
        //        }

        //        continueButton.interactable = true;
        //    }

        //    OnSequenceFinished?.Invoke();
        //    _runner = null;
        //}
    }

    void HideContinueButtonNow()
    {
        if (!continueButton) return;

        if (hideContinueButtonUntilDone)
        {
            // Fully disable and hide
            continueButton.interactable = false;
            if (continueButtonCanvasGroup) continueButtonCanvasGroup.alpha = 0f;
            continueButton.gameObject.SetActive(false);
        }
        else
        {
            // Keep active but non-interactable (if you prefer)
            continueButton.interactable = false;
            if (continueButtonCanvasGroup) continueButtonCanvasGroup.alpha = 0.25f;
            continueButton.gameObject.SetActive(true);
        }
    }

    void ShowContinueButtonNow()
    {
        if (!continueButton) return;

        continueButton.gameObject.SetActive(true);

        if (continueButtonCanvasGroup && hideContinueButtonUntilDone && continueButtonFadeSeconds > 0f)
        {
            // fade-in
            StartCoroutine(CoFadeInContinueButton());
        }
        else
        {
            // just show it
            if (continueButtonCanvasGroup) continueButtonCanvasGroup.alpha = 1f;
            continueButton.interactable = true;
        }

        if (autoSelectContinueButton)
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem) eventSystem.SetSelectedGameObject(continueButton.gameObject);
        }
    }

    IEnumerator CoFadeInContinueButton()
    {
        float t = 0f, dur = Mathf.Max(0.0001f, continueButtonFadeSeconds);
        if (continueButtonCanvasGroup) continueButtonCanvasGroup.alpha = 0f;

        while (t < dur)
        {
            t += DT;
            if (continueButtonCanvasGroup)
                continueButtonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / dur);
            yield return null;
        }

        if (continueButtonCanvasGroup) continueButtonCanvasGroup.alpha = 1f;
        continueButton.interactable = true;
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
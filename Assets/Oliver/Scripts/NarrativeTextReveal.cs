using System;
using System.Collections;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class NarrativeTextReveal : MonoBehaviour
{
    public enum RevealMode { Typewriter, FadeIn }

    [Header("Target")]
    public TMP_Text label;

    [Header("Mode")]
    public RevealMode mode = RevealMode.Typewriter;

    [Header("Shared")]
    [TextArea(3, 10)] public string sourceText;
    public bool useUnscaledTime = true;
    public float initialDelay = 0f;

    [Header("Typewriter")]
    public float charsPerSecond = 35f;
    public bool ignoreWhitespaceDelay = true;
    public float startDelay = 0f;

    [Header("Fade In")]
    public float fadeCharsPerSecond = 30f;   // base head speed
    public int fadeTrailLength = 3;        // chars fading behind the head
    public float fadeEaseInSeconds = 0.75f;

    [Header("Fast-Forward (when player presses)")]
    public float typewriterFastFactor = 50f;
    public float fadeFastFactor = 10f;
    public int fadeTrailLengthFast = 1;   // shorter trail during FF
    public bool fastForwardSkipsStartDelay = true;

    // --- State / Events ---
    public bool IsRevealing { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsFastForwarding { get; private set; }
    public event Action OnRevealComplete;

    // internals
    TMP_TextInfo _info;
    Coroutine _co;

    void Reset() => label = GetComponent<TMP_Text>();
    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
        if (!label) enabled = false;
    }

    public void Play(string text = null)
    {
        if (!label) return;

        // choose text
        if (text != null) sourceText = text;
        if (string.IsNullOrEmpty(sourceText)) sourceText = label.text;

        // stop prior run & init fresh state
        Stop();
        IsComplete = false;
        IsRevealing = true;
        IsFastForwarding = false;

        // render full text once (stable layout)
        label.maxVisibleCharacters = int.MaxValue;
        label.text = sourceText;

        // IMPORTANT: force full reparse + geometry (prevents characterCount=0 hiccups)
        label.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
        _info = label.textInfo;

        _co = (mode == RevealMode.Typewriter)
            ? StartCoroutine(CoTypewriter())
            : StartCoroutine(CoFadeIn());
    }

    public void Stop()
    {
        if (_co != null) StopCoroutine(_co);
        _co = null;
        IsRevealing = false;
        IsFastForwarding = false;
    }

    public void FastForward()
    {
        if (!IsRevealing) return;
        IsFastForwarding = true;
    }

    public void SkipToEnd()
    {
        if (!IsRevealing) return;
        if (mode == RevealMode.Typewriter) SnapTypewriter(CurrentInfo().characterCount);
        else SnapFade(1f);
        Finish();
    }

    public void ResetFastForward() => IsFastForwarding = false;

    void Finish()
    {
        IsRevealing = false;
        IsFastForwarding = false;
        IsComplete = true;
        OnRevealComplete?.Invoke();
    }

    // ---------- TYPEWRITER ----------
    IEnumerator CoTypewriter()
    {
        // start fully hidden by alpha
        SetAllCharAlpha(0f);

        // optional initial delay (skippable if FF pressed)
        float remainingDelay = Mathf.Max(0f, initialDelay + startDelay);
        while (remainingDelay > 0f)
        {
            if (IsFastForwarding && fastForwardSkipsStartDelay) break;
            remainingDelay -= DT; yield return null;
        }

        // make sure TMP info is fresh after any delay
        label.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: false);
        _info = label.textInfo;

        int total = _info.characterCount;
        if (total <= 0) { Finish(); yield break; }

        int logical = 0;    // counts ALL TMP characters (including spaces/newlines)
        float carry = 0f;

        // while we haven't stepped past the last character
        while (logical < total)
        {
            float cps = charsPerSecond * (IsFastForwarding ? Mathf.Max(1f, typewriterFastFactor) : 1f);
            carry += cps * DT;

            // consume whole characters at this frame's rate
            while (carry >= 1f && logical < total)
            {
                var ci = _info.characterInfo[logical];

                // if ignoring whitespace delay and this char is NOT visible,
                // advance the logical index WITHOUT spending "carry" time.
                if (ignoreWhitespaceDelay && !ci.isVisible)
                {
                    logical++;
                    continue; // don't decrement carry
                }

                // visible (or we're counting whitespace): spend time
                logical++;
                carry -= 1f;
            }

            // reveal everything up to 'logical - 1' via vertex alpha
            ApplyTypewriterCutoff(logical - 1);

            yield return null;
        }

        // ensure fully revealed at the end
        ApplyTypewriterCutoff(total - 1);
        Finish();
    }

    void ApplyTypewriterCutoff(int cutoffIndex)
    {
        var info = label.textInfo;
        int total = info.characterCount;
        if (total == 0) return;

        cutoffIndex = Mathf.Clamp(cutoffIndex, -1, total - 1);

        var meshes = info.meshInfo;

        // make chars <= cutoff opaque, > cutoff transparent (visible only)
        for (int i = 0; i < total; i++)
        {
            var ci = info.characterInfo[i];
            if (!ci.isVisible) continue;

            var cols = meshes[ci.materialReferenceIndex].colors32;
            int vi = ci.vertexIndex;
            if (vi < 0 || vi + 3 >= cols.Length) continue;

            byte a = (i <= cutoffIndex) ? (byte)255 : (byte)0;
            cols[vi].a = cols[vi + 1].a = cols[vi + 2].a = cols[vi + 3].a = a;
        }

        for (int m = 0; m < meshes.Length; m++)
        {
            var mesh = meshes[m].mesh;
            mesh.colors32 = meshes[m].colors32;
            label.UpdateGeometry(mesh, m);
        }
    }

    void SnapTypewriter(int visibleCount)
    {
        var info = CurrentInfo();
        label.maxVisibleCharacters = Mathf.Clamp(visibleCount, 0, info.characterCount);
        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    // ---------- FADE IN ----------
    IEnumerator CoFadeIn()
    {
        // start fully transparent
        SetAllCharAlpha(0f);

        float remainingDelay = Mathf.Max(0f, initialDelay);

        // ensure fresh info
        label.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: false);
        _info = label.textInfo;

        int total = _info.characterCount;
        if (total == 0) { Finish(); yield break; }

        float key = -fadeTrailLength; // head starts before first char
        float end = total - 1;

        float speed = 0f;
        float accelT = 0f;

        while (key < end)
        {
            float baseSpeed = fadeCharsPerSecond;
            float targetSpeed = baseSpeed * (IsFastForwarding ? Mathf.Max(1f, fadeFastFactor) : 1f);

            if (!IsFastForwarding && accelT <= fadeEaseInSeconds)
            {
                accelT += DT;
                speed = Mathf.Lerp(0f, targetSpeed, accelT / Mathf.Max(0.0001f, fadeEaseInSeconds));
            }
            else speed = targetSpeed;

            key = Mathf.MoveTowards(key, end, speed * DT);

            int trail = IsFastForwarding ? Mathf.Max(1, fadeTrailLengthFast) : Mathf.Max(1, fadeTrailLength);
            ApplyFadeTrail(key, trail);

            yield return null;
        }

        SnapFade(1f);
        Finish();
    }

    void ApplyFadeTrail(float keyChar, int trailLen)
    {
        var info = CurrentInfo();
        int total = info.characterCount;
        if (total == 0) return;

        int lastFull = Mathf.Clamp(Mathf.CeilToInt(keyChar), -1, total - 1);
        int firstZero = Mathf.Clamp((int)(keyChar + Mathf.Max(1, trailLen)) + 1, 0, total);

        var meshes = info.meshInfo;

        // fully opaque
        for (int i = 0; i <= lastFull && i < total; i++)
        {
            var ci = info.characterInfo[i];
            if (!ci.isVisible) continue;

            var cols = meshes[ci.materialReferenceIndex].colors32;
            int vi = ci.vertexIndex;
            if (vi < 0 || vi + 3 >= cols.Length) continue;

            cols[vi].a = cols[vi + 1].a = cols[vi + 2].a = cols[vi + 3].a = 255;
        }

        // trail band (1..0)
        for (int i = lastFull + 1; i < firstZero && i < total; i++)
        {
            var ci = info.characterInfo[i];
            if (!ci.isVisible) continue;

            var cols = meshes[ci.materialReferenceIndex].colors32;
            int vi = ci.vertexIndex;
            if (vi < 0 || vi + 3 >= cols.Length) continue;

            float t = 1f - Mathf.Clamp01((i - keyChar) / Mathf.Max(1, trailLen));
            byte a = (byte)Mathf.RoundToInt(255f * t);
            cols[vi].a = cols[vi + 1].a = cols[vi + 2].a = cols[vi + 3].a = a;
        }

        // fully transparent
        for (int i = firstZero; i < total; i++)
        {
            var ci = info.characterInfo[i];
            if (!ci.isVisible) continue;

            var cols = meshes[ci.materialReferenceIndex].colors32;
            int vi = ci.vertexIndex;
            if (vi < 0 || vi + 3 >= cols.Length) continue;

            cols[vi].a = cols[vi + 1].a = cols[vi + 2].a = cols[vi + 3].a = 0;
        }

        // push back
        for (int m = 0; m < meshes.Length; m++)
        {
            var mesh = meshes[m].mesh;
            mesh.colors32 = meshes[m].colors32;
            label.UpdateGeometry(mesh, m);
        }
    }

    void SnapFade(float normalized)
    {
        byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(normalized) * 255f);

        var info = CurrentInfo();
        var meshes = info.meshInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            var ci = info.characterInfo[i];
            if (!ci.isVisible) continue;

            var cols = meshes[ci.materialReferenceIndex].colors32;
            int vi = ci.vertexIndex;
            if (vi < 0 || vi + 3 >= cols.Length) continue;

            cols[vi].a = cols[vi + 1].a = cols[vi + 2].a = cols[vi + 3].a = a;
        }

        for (int m = 0; m < meshes.Length; m++)
        {
            var mesh = meshes[m].mesh;
            mesh.colors32 = meshes[m].colors32;
            label.UpdateGeometry(mesh, m);
        }
    }

    void SetAllCharAlpha(float a01)
    {
        // Always rebuild info before bulk write
        label.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: false);
        _info = label.textInfo;

        byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(a01) * 255f);
        var meshes = _info.meshInfo;

        for (int i = 0; i < _info.characterCount; i++)
        {
            var ci = _info.characterInfo[i];
            if (!ci.isVisible) continue;

            var cols = meshes[ci.materialReferenceIndex].colors32;
            int vi = ci.vertexIndex;
            if (vi < 0 || vi + 3 >= cols.Length) continue;

            cols[vi].a = cols[vi + 1].a = cols[vi + 2].a = cols[vi + 3].a = a;
        }

        for (int m = 0; m < meshes.Length; m++)
        {
            var mesh = meshes[m].mesh;
            mesh.colors32 = meshes[m].colors32;
            label.UpdateGeometry(mesh, m);
        }
    }

    TMP_TextInfo CurrentInfo()
    {
        // Always read fresh—TMP can reallocate arrays mid-play
        _info = label.textInfo;
        return _info;
    }

    float DT => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
}
using TMPro;
using UnityEngine;


[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class TitleFX : MonoBehaviour
{
    public TMP_Text text;

    [Header("Material")]
    public bool cloneMaterial = true;
    public string lightAngleProperty = "_LightAngle";
    public string faceDilateProperty = "_FaceDilate";

    public enum SweepMode { Loop, PingPong }

    [Header("Light Sweep")]
    public bool enableLightSweep = true;
    public bool startSweepOnEnable = true;
    public bool startSweepAfterReveal = true;      // wait for reveal to finish
    public float sweepStartDelayAfterReveal = 0f;  // extra delay if needed
    public SweepMode mode = SweepMode.Loop;
    public float minAngle = 0f;
    public float maxAngle = 6f;
    public float sweepSeconds = 4.5f;
    public AnimationCurve sweepEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool randomizeSweepPhase = true;

    [Header("Dilate Reveal")]
    public bool enableDilateReveal = true;
    public bool playRevealOnEnable = true;
    public float startDilate = -1f;
    public float endDilate = 0f;
    public float revealSeconds = 1.6f;
    public float revealDelay = 0f;
    public AnimationCurve revealEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool updatePaddingDuringReveal = false;

    [Header("Underlay")]
    public bool includeUnderlayInReveal = true;
    public bool forceEnableUnderlayKeyword = true;
    public float startUnderlayDilate = -1f;
    public float endUnderlayDilate = 0f;
    public float startUnderlaySoftness = 0f;
    public float endUnderlaySoftness = 0.00f;
    public bool animateUnderlayAlpha = false;
    [Range(0, 1)] public float startUnderlayAlpha = 0.75f;
    [Range(0, 1)] public float endUnderlayAlpha = 0.75f;

    [Header("Time")]
    public bool useUnscaledTime = true;

    // internals
    Material _mat;
    int _idLightAngle, _idFaceDilate, _idULColor, _idULDilate, _idULSoft, _idULOffX, _idULOffY;
    bool _hasLightAngle, _hasFaceDilate, _hasULColor, _hasULDilate, _hasULSoft, _hasULOffX, _hasULOffY;

    float _phase;
    bool _revealDone;
    bool _sweepActive;
    Coroutine _revealCo, _sweepDelayCo;

    void Reset() => text = GetComponent<TMP_Text>();

    void Awake()
    {
        if (!text) text = GetComponent<TMP_Text>();
        if (!text) { enabled = false; return; }

        _mat = EnsureInstance(text, cloneMaterial);

        var c = text.color; if (c.a < 1f) { c.a = 1f; text.color = c; }

        _idLightAngle = Shader.PropertyToID(lightAngleProperty);
        _idFaceDilate = Shader.PropertyToID(faceDilateProperty);
        _idULColor = Shader.PropertyToID("_UnderlayColor");
        _idULDilate = Shader.PropertyToID("_UnderlayDilate");
        _idULSoft = Shader.PropertyToID("_UnderlaySoftness");
        _idULOffX = Shader.PropertyToID("_UnderlayOffsetX");
        _idULOffY = Shader.PropertyToID("_UnderlayOffsetY");

        _hasLightAngle = _mat.HasProperty(_idLightAngle);
        _hasFaceDilate = _mat.HasProperty(_idFaceDilate);
        _hasULColor = _mat.HasProperty(_idULColor);
        _hasULDilate = _mat.HasProperty(_idULDilate);
        _hasULSoft = _mat.HasProperty(_idULSoft);
        _hasULOffX = _mat.HasProperty(_idULOffX);
        _hasULOffY = _mat.HasProperty(_idULOffY);

        if (forceEnableUnderlayKeyword) _mat.EnableKeyword("UNDERLAY_ON");

        // lock offsets so the underlay doesn't slide
        if (_hasULOffX) _mat.SetFloat(_idULOffX, -1f);
        if (_hasULOffY) _mat.SetFloat(_idULOffY, -.35f);
        // touch this and i'll find you

        if (_hasULColor && !animateUnderlayAlpha)
        {
            var col = _mat.GetColor(_idULColor);
            col.a = Mathf.Clamp01(endUnderlayAlpha);
            _mat.SetColor(_idULColor, col);
        }

        if (enableDilateReveal && _hasFaceDilate)
            SetFaceDilate(startDilate, applyPadding: true);

        if (includeUnderlayInReveal)
            InitUnderlayStart();

        if (_hasLightAngle)
            ApplyAngle(minAngle);

        if (randomizeSweepPhase) _phase = Random.value;
    }

    void OnEnable()
    {
        if (_mat && text && text.fontMaterial != _mat) text.fontMaterial = _mat;

        if (playRevealOnEnable && enableDilateReveal && !_revealDone)
        {
            if (_revealCo != null) StopCoroutine(_revealCo);
            _revealCo = StartCoroutine(CoReveal());
        }
        else
        {
            MaybeStartSweepNow();
        }
    }

    void OnDisable()
    {
        if (_revealCo != null) { StopCoroutine(_revealCo); _revealCo = null; }
        if (_sweepDelayCo != null) { StopCoroutine(_sweepDelayCo); _sweepDelayCo = null; }
    }

    void OnDestroy()
    {
        if (cloneMaterial && _mat) Destroy(_mat);
        _mat = null;
    }

    void Update()
    {
        if (!_sweepActive || !enableLightSweep || !_hasLightAngle || sweepSeconds <= 0f) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float delta = dt / Mathf.Max(0.0001f, sweepSeconds);

        if (mode == SweepMode.Loop)
        {
            _phase = Mathf.Repeat(_phase + delta, 1f);
            float k = sweepEase.Evaluate(_phase);
            ApplyAngle(Mathf.Lerp(minAngle, maxAngle, k));
        }
        else
        {
            _phase = Mathf.Repeat(_phase + delta * 2f, 2f);
            float tri = _phase <= 1f ? _phase : 2f - _phase;
            float k = sweepEase.Evaluate(tri);
            ApplyAngle(Mathf.Lerp(minAngle, maxAngle, k));
        }
    }

    System.Collections.IEnumerator CoReveal()
    {
        if (revealDelay > 0f)
        {
            float t0 = 0f;
            while (t0 < revealDelay) { t0 += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime); yield return null; }
        }

        float t = 0f, dur = Mathf.Max(0.0001f, revealSeconds);
        while (t < dur)
        {
            float k = Mathf.Clamp01(t / dur);
            if (revealEase != null) k = revealEase.Evaluate(k);

            // Face dilate
            if (_hasFaceDilate) SetFaceDilate(Mathf.Lerp(startDilate, endDilate, k), applyPadding: false);

            // Underlay follows the same k to avoid mismatch/snap
            if (includeUnderlayInReveal) StepUnderlay(k, applyPadding:false);

            if (updatePaddingDuringReveal)
            {
                text.UpdateMeshPadding();
                text.havePropertiesChanged = true;
            }

            yield return null;
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        if (_hasFaceDilate) SetFaceDilate(endDilate, applyPadding: true);
        if (includeUnderlayInReveal) StepUnderlay(1f, applyPadding: true);

        _revealDone = true;
        _revealCo = null;
        MaybeStartSweepNow();
    }

    void MaybeStartSweepNow()
    {
        if (!enableLightSweep || !startSweepOnEnable || _sweepActive) return;
        if (startSweepAfterReveal && enableDilateReveal && !_revealDone) return;

        if (sweepStartDelayAfterReveal > 0f)
        {
            if (_sweepDelayCo != null) StopCoroutine(_sweepDelayCo);
            _sweepDelayCo = StartCoroutine(CoStartSweepAfterDelay(sweepStartDelayAfterReveal));
        }
        else StartSweep();
    }

    System.Collections.IEnumerator CoStartSweepAfterDelay(float delay)
    {
        float t = 0f;
        while (t < delay) { t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime); yield return null; }
        StartSweep();
        _sweepDelayCo = null;
    }

    void StartSweep()
    {
        if (randomizeSweepPhase) _phase = Random.value;
        ApplyAngle(minAngle);
        _sweepActive = true;
    }

    // ---- helpers ----

    void InitUnderlayStart()
    {
        if (forceEnableUnderlayKeyword) _mat.EnableKeyword("UNDERLAY_ON");

        if (_hasULDilate) _mat.SetFloat(_idULDilate, startUnderlayDilate);
        if (_hasULSoft) _mat.SetFloat(_idULSoft, startUnderlaySoftness);
        if (_hasULColor && animateUnderlayAlpha)
        {
            var col = _mat.GetColor(_idULColor);
            col.a = startUnderlayAlpha;
            _mat.SetColor(_idULColor, col);
        }
        text.UpdateMeshPadding();
        text.havePropertiesChanged = true;
    }

    void StepUnderlay(float k, bool applyPadding = false)
    {
        if (_hasULDilate) _mat.SetFloat(_idULDilate, Mathf.Lerp(startUnderlayDilate, endUnderlayDilate, k));
        if (_hasULSoft) _mat.SetFloat(_idULSoft, Mathf.Lerp(startUnderlaySoftness, endUnderlaySoftness, k));

        if (_hasULColor && animateUnderlayAlpha)
        {
            var col = _mat.GetColor(_idULColor);
            col.a = Mathf.Lerp(startUnderlayAlpha, endUnderlayAlpha, k);
            _mat.SetColor(_idULColor, col);
        }

        if (applyPadding) text.UpdateMeshPadding();
        text.havePropertiesChanged = true;
    }

    static Material EnsureInstance(TMP_Text txt, bool wantClone)
    {
        var current = txt.fontMaterial;
        var shared = txt.fontSharedMaterial;
        if (current && current != shared) return current;
        if (!wantClone) return current ? current : shared;
        var inst = Object.Instantiate(shared);
        txt.fontMaterial = inst;
        return inst;
    }

    void ApplyAngle(float angle)
    {
        if (_mat && _hasLightAngle)
        {
            _mat.SetFloat(_idLightAngle, angle);
            text.havePropertiesChanged = true;
        }
    }

    void SetFaceDilate(float value, bool applyPadding)
    {
        if (_mat && _hasFaceDilate)
        {
            _mat.SetFloat(_idFaceDilate, value);
            if (applyPadding) text.UpdateMeshPadding();
            text.havePropertiesChanged = true;
        }
    }

    public void PlayReveal(bool restart = false)
    {
        if (!enableDilateReveal || !_hasFaceDilate) return;
        if (restart) { _revealDone = false; SetFaceDilate(startDilate, true); }
        if (_revealCo != null) StopCoroutine(_revealCo);
        _revealCo = StartCoroutine(CoReveal());
    }
}

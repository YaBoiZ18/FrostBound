using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class BetterTMPButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    // ----- Public API -----
    public enum UIState { Normal, Highlighted, Pressed, Selected, Disabled }

    [Header("Optional: link a Selectable (e.g., Button) to inherit Disabled")]
    public Selectable selectable;

    [Header("Targets")]
    [Tooltip("Graphics (Image, TMP_Text, etc.) that should tint.")]
    public Graphic[] tintTargets;

    [Tooltip("TMP texts that should get an outline. Materials are cloned per instance.")]
    public TMP_Text[] outlineTargets;

    [Header("Transition")]
    public float fadeDuration = 0.12f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool useUnscaledTime = true;

    [System.Serializable]
    public struct StateStyle
    {
        public Color tint;
        public Color outlineColor;
        [Range(0, 1)] public float outlineWidth;
    }

    [System.Serializable]
    public struct StyleSet
    {
        public StateStyle normal;
        public StateStyle highlighted;
        public StateStyle pressed;
        public StateStyle selected;
        public StateStyle disabled;

        public StateStyle For(UIState s)
        {
            switch (s)
            {
                case UIState.Highlighted: return highlighted;
                case UIState.Pressed: return pressed;
                case UIState.Selected: return selected;
                case UIState.Disabled: return disabled;
                default: return normal;
            }
        }
        public void Set(UIState s, StateStyle v)
        {
            switch (s)
            {
                case UIState.Highlighted: highlighted = v; break;
                case UIState.Pressed: pressed = v; break;
                case UIState.Selected: selected = v; break;
                case UIState.Disabled: disabled = v; break;
                default: normal = v; break;
            }
        }
    }

    [Header("Styles")]
    public StyleSet style = new StyleSet
    {
        normal = new StateStyle { tint = Color.white, outlineColor = Color.white, outlineWidth = 0f },
        highlighted = new StateStyle { tint = new Color(0.95f, 0.95f, 0.95f, 1), outlineColor = Color.white, outlineWidth = 0.22f },
        pressed = new StateStyle { tint = new Color(0.88f, 0.93f, 1f, 1), outlineColor = Color.white, outlineWidth = 0.28f },
        selected = new StateStyle { tint = new Color(0.95f, 0.95f, 0.95f, 1), outlineColor = Color.white, outlineWidth = 0.22f },
        disabled = new StateStyle { tint = new Color(1, 1, 1, 0.5f), outlineColor = new Color(1, 1, 1, 0.5f), outlineWidth = 0f }
    };

    [Header("Safety")]
    public bool ensureTextAlphaOpaqueOnAwake = true;
    public bool reapplyStateInStart = true;

    // Runtime theming
    public void SetTint(UIState state, Color c) { var v = style.For(state); v.tint = c; style.Set(state, v); Refresh(); }
    public void SetOutline(UIState state, Color c, float w) { var v = style.For(state); v.outlineColor = c; v.outlineWidth = w; style.Set(state, v); Refresh(); }
    public void ApplyStyleSet(StyleSet set) { style = set; Refresh(true); }
    public void Refresh(bool instant = false) { ApplyState(GetCompositeState(), instant); }

    // ----- Internals -----
    static int ID_OutlineWidth;
    static int ID_OutlineColor;
    static bool sIDsReady;

    struct TmpEntry { public TMP_Text text; public Material mat; }
    readonly List<TmpEntry> _tmps = new List<TmpEntry>();

    UIState _state = UIState.Normal;
    Coroutine _anim;
    bool _hover, _pressed, _selected;
    bool _lastInteractable = true;

    void Reset()
    {
        selectable = GetComponent<Selectable>();
        var g = GetComponent<Graphic>();
        if (g) tintTargets = new[] { g };
        var tmp = GetComponent<TMP_Text>();
        if (tmp) outlineTargets = new[] { tmp };
    }

    void Awake()
    {
        EnsureIDs(); // <-- safe: uses PropertyToID, no Shader.Find
        if (!selectable) selectable = GetComponent<Selectable>();
        PrepareTintTargets();
        PrepareOutlineTargets();
        ApplyState(GetCompositeState(), true);
        if (ensureTextAlphaOpaqueOnAwake) ForceTextAlphaOpaque();
        ApplyState(GetCompositeState(), true);
    }

    void Start()
    {
        if (reapplyStateInStart) ApplyState(GetCompositeState(), true);
    }

    void OnEnable() => ApplyState(GetCompositeState(), true);
    void OnDisable() => StopTween();

    void OnDestroy()
    {
        // clean up cloned materials
        for (int i = 0; i < _tmps.Count; i++)
            if (_tmps[i].mat) Destroy(_tmps[i].mat);
        _tmps.Clear();
    }

    static void EnsureIDs()
    {
        if (sIDsReady) return;
        // Avoid ShaderUtilities here; it triggers Shader.Find during static init.
        ID_OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        ID_OutlineColor = Shader.PropertyToID("_OutlineColor");
        sIDsReady = true;
    }

    void PrepareTintTargets()
    {
        if (tintTargets == null || tintTargets.Length == 0)
        {
            var g = GetComponent<Graphic>();
            if (g) tintTargets = new[] { g };
        }
    }

    void PrepareOutlineTargets()
    {
        _tmps.Clear();
        if (outlineTargets == null) return;

        foreach (var t in outlineTargets)
        {
            if (!t) continue;
            var cloned = Instantiate(t.fontSharedMaterial); // isolate instance
            t.fontMaterial = cloned;
            var c = t.color; c.a = 1f; t.color = c;        // ensure full alpha
            t.UpdateMeshPadding();                         // avoid clipping
            _tmps.Add(new TmpEntry { text = t, mat = cloned });
        }
    }

    void Update()
    {
        if (selectable)
        {
            bool interactable = selectable.IsActive() && selectable.IsInteractable();
            if (interactable != _lastInteractable)
            {
                _lastInteractable = interactable;
                ApplyState(GetCompositeState());
            }
        }
    }

    UIState GetCompositeState()
    {
        if (selectable && (!selectable.IsActive() || !selectable.IsInteractable()))
            return UIState.Disabled;

        if (_pressed) return UIState.Pressed;
        if (_hover) return _selected ? UIState.Selected : UIState.Highlighted;
        if (_selected) return UIState.Selected;
        return UIState.Normal;
    }

    void ApplyState(UIState s, bool instant = false)
    {
        if (_state == s && !instant) return;
        _state = s;
        var target = style.For(s);
        StartTween(target, instant ? 0f : fadeDuration);
    }

    void StartTween(StateStyle target, float duration)
    {
        StopTween();
        _anim = StartCoroutine(AnimateTo(target, Mathf.Max(0f, duration)));
    }

    void StopTween()
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = null;
    }

    IEnumerator AnimateTo(StateStyle target, float dur)
    {
        var startTints = new Color[tintTargets?.Length ?? 0];
        for (int i = 0; i < startTints.Length; i++)
            startTints[i] = tintTargets[i] ? tintTargets[i].color : Color.white;

        var startOW = new float[_tmps.Count];
        var startOC = new Color[_tmps.Count];
        for (int i = 0; i < _tmps.Count; i++)
        {
            var e = _tmps[i];
            if (!e.mat) continue;
            startOW[i] = e.mat.GetFloat(ID_OutlineWidth);
            startOC[i] = e.mat.GetColor(ID_OutlineColor);
        }

        float t = 0f;
        while (t < dur)
        {
            float k = dur <= 0f ? 1f : fadeCurve.Evaluate(t / dur);

            for (int i = 0; i < startTints.Length; i++)
                if (tintTargets[i]) tintTargets[i].color = Color.Lerp(startTints[i], target.tint, k);

            for (int i = 0; i < _tmps.Count; i++)
            {
                var e = _tmps[i];
                if (!e.mat || !e.text) continue;
                e.mat.SetFloat(ID_OutlineWidth, Mathf.Lerp(startOW[i], target.outlineWidth, k));
                e.mat.SetColor(ID_OutlineColor, Color.Lerp(startOC[i], target.outlineColor, k));
                e.text.UpdateMeshPadding();
                e.text.havePropertiesChanged = true;
            }

            yield return null;
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        // snap final
        for (int i = 0; i < startTints.Length; i++)
            if (tintTargets[i]) tintTargets[i].color = target.tint;

        for (int i = 0; i < _tmps.Count; i++)
        {
            var e = _tmps[i];
            if (!e.mat || !e.text) continue;
            e.mat.SetFloat(ID_OutlineWidth, target.outlineWidth);
            e.mat.SetColor(ID_OutlineColor, target.outlineColor);
            e.text.UpdateMeshPadding();
            e.text.havePropertiesChanged = true;
        }
    }

    void ForceTextAlphaOpaque()
    {
        if (outlineTargets != null)
        {
            foreach (var t in outlineTargets) if (t) { var c = t.color; c.a = 1f; t.color = c; }
        }
        if (tintTargets != null)
            foreach (var g in tintTargets) if (g) { var c = g.color; c.a = 1f; g.color = c; }
    }

    // ----- Event hooks -----
    public void OnPointerEnter(PointerEventData e) { _hover = true; ApplyState(GetCompositeState()); }
    public void OnPointerExit(PointerEventData e) { _hover = false; ApplyState(GetCompositeState()); }
    public void OnPointerDown(PointerEventData e) { _pressed = true; ApplyState(GetCompositeState()); }
    public void OnPointerUp(PointerEventData e) { _pressed = false; ApplyState(GetCompositeState()); }
    public void OnSelect(BaseEventData e) { _selected = true; ApplyState(GetCompositeState()); }
    public void OnDeselect(BaseEventData e) { _selected = false; ApplyState(GetCompositeState()); }
}
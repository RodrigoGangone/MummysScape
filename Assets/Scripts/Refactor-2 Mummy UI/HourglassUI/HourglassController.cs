using UnityEngine;

/// <summary>
/// HourglassController
/// Controla el fill de los dos MeshRenderers (top/bottom) del reloj de arena usando el mismo valor _Fill:
/// - Material TOP (HourglassSandTop): Fill=1 es lleno, Fill=0 es vacío.
/// - Material BOTTOM (HourglassSandBottom): Fill=0 es lleno, Fill=1 es vacío.
/// El controller interpreta "_fillTop" como "cuánto está lleno el TOP" y se lo pasa a ambos renderers.
/// Se suscribe a GameEventManager.playerEvents.OnBandagesCountChanged:
///   • bandages > 0  → ResetAndFill()  (TOP se llena; BOTTOM se vacía)
///   • bandages == 0 → StartCountdown() (TOP se vacía; BOTTOM se llena)
/// Métodos públicos:
///   • StartCountdown(): anima TOP 1→0 en _countdownDuration
///   • ResetAndFill():  anima TOP 0→1 en _resetDuration
/// Usa MaterialPropertyBlock para no instanciar materiales.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class HourglassController : MonoBehaviour
{
    [Header("Renderers (asignar materiales correctos)")]
    [SerializeField] private MeshRenderer _topLiquidRenderer;    // Material: HourglassSandTop (IsBottomSand = false)
    [SerializeField] private MeshRenderer _bottomLiquidRenderer; // Material: HourglassSandBottom (IsBottomSand = true)

    [Header("Duraciones (segundos)")]
    [Min(0f)] [SerializeField] private float _countdownDuration = 10f; // TOP 1→0
    [Min(0f)] [SerializeField] private float _resetDuration     = 0.75f; // TOP 0→1

    [Header("Curvas")]
    [SerializeField] private AnimationCurve _countdownCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve _resetCurve     = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private static readonly int FillID = Shader.PropertyToID("_Fill");

    private MaterialPropertyBlock _topMPB;
    private MaterialPropertyBlock _botMPB;

    // Valor que mandamos al shader: "cuánto está lleno el TOP" [0..1]
    private float _fillTop = 1f;

    // Animación
    private bool _animating;
    private float _from, _to, _elapsed, _duration;
    private AnimationCurve _curve;

    // Eventos
    private bool _bootstrapped;
    private int  _lastBandages = -1;

    private void Awake()
    {
        _topMPB = new MaterialPropertyBlock();
        _botMPB = new MaterialPropertyBlock();

        var evt = GameEventManager.Instance.playerEvents.OnBandagesCountChanged;
        if (evt != null) evt.Register<int>(OnBandagesChanged);

        // Importante: NO seteamos un estado por defecto aquí; esperamos el primer Raise.
    }

    private void OnDisable()
    {
        var evt = GameEventManager.Instance.playerEvents.OnBandagesCountChanged;
        if (evt != null) evt.Unregister<int>(OnBandagesChanged);
    }

    private void Update()
    {
        if (!_animating) return;

        _elapsed += Time.deltaTime;
        float n = _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
        float k = (_curve != null) ? _curve.Evaluate(n) : n;

        ApplyFill(Mathf.LerpUnclamped(_from, _to, k));

        if (n >= 1f - Mathf.Epsilon)
        {
            _animating = false;
            ApplyFill(_to); // snap final
        }
    }

    private void OnBandagesChanged(int count)
    {
        if (!_bootstrapped)
        {
            _bootstrapped = true;
            _lastBandages = count;
            SnapByBandageCount(count);
            return;
        }

        if (count == 0 && _lastBandages > 0)         // perdió todas → empezar countdown
            StartCountdown();
        else if (count > 0 && _lastBandages == 0)     // volvió a tener → resetear
            ResetAndFill();

        _lastBandages = count;
    }

    /// <summary>Vacía el slot superior (TOP 1→0) y llena el inferior.</summary>
    public void StartCountdown() => BeginAnim(_fillTop, 0f, _countdownDuration, _countdownCurve);

    /// <summary>Llena el slot superior (TOP 0→1) y vacía el inferior.</summary>
    public void ResetAndFill()   => BeginAnim(_fillTop, 1f, _resetDuration, _resetCurve);

    /// <summary>Estado instantáneo según # de vendas (sin animar).</summary>
    public void SnapByBandageCount(int bandages)
    {
        // bandages > 0 ⇒ TOP debe estar lleno ⇒ _fillTop = 1
        // bandages == 0 ⇒ TOP vacío ⇒ _fillTop = 0
        ApplyFill(bandages > 0 ? 1f : 0f);
    }

    // Internos --------------------------------------------------------------

    private void BeginAnim(float from, float to, float duration, AnimationCurve curve)
    {
        _from = Mathf.Clamp01(from);
        _to = Mathf.Clamp01(to);
        _duration = Mathf.Max(0f, duration);
        _curve = curve ?? AnimationCurve.Linear(0, 0, 1, 1);
        _elapsed = 0f;
        _animating = _duration > 0f;

        if (!_animating) ApplyFill(_to);
    }

    private void ApplyFill(float topFill01)
    {
        _fillTop = Mathf.Clamp01(topFill01);

        if (_topLiquidRenderer)
        {
            _topLiquidRenderer.GetPropertyBlock(_topMPB);
            _topMPB.SetFloat(FillID, _fillTop);      // TOP: 1 lleno / 0 vacío
            _topLiquidRenderer.SetPropertyBlock(_topMPB);
        }
        if (_bottomLiquidRenderer)
        {
            _bottomLiquidRenderer.GetPropertyBlock(_botMPB);
            _botMPB.SetFloat(FillID, _fillTop);      // BOTTOM: 1 vacío / 0 lleno (mismo valor, interpretación opuesta)
            _bottomLiquidRenderer.SetPropertyBlock(_botMPB);
        }
    }
}
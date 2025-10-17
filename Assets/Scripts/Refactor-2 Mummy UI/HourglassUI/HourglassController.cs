using UnityEngine;

/// <summary>
/// HourglassController
/// Control independiente de los 2 volúmenes (arriba/abajo) del reloj de arena
/// que usan el shader "HourglassSand".
/// - Setea _UpDown por renderer con MaterialPropertyBlock.
/// - Actualiza _Fill por renderer (Top = 1 - t, Bottom = t) sin instanciar materiales.
/// - Métodos: StartCountdown (vacía arriba / llena abajo), ResetAndFill (revierte),
///   SnapByBandageCount (aplica estado instantáneo según # de vendas).
/// </summary>
[DisallowMultipleComponent]
public sealed class HourglassController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private MeshRenderer _topLiquidRenderer;    // sandUp_low
    [SerializeField] private MeshRenderer _bottomLiquidRenderer; // sandDown_low

    [Header("Configuración")]
    [Min(0.01f)]
    [SerializeField] private float _transitionDuration = 5f;
    [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Shader property IDs
    private static readonly int FillID   = Shader.PropertyToID("_Fill");
    private static readonly int UpDownID = Shader.PropertyToID("_UpDown");

    private MaterialPropertyBlock _topMPB;
    private MaterialPropertyBlock _botMPB;

    // t en [0..1]  (Top = 1 - t, Bottom = t)
    // <<< CAMBIO: arrancamos con t=0 => TOP lleno, BOTTOM vacío >>>
    private float _tCurrent = 0f; 
    private float _tTarget  = 0f;
    private float _elapsed;
    private bool  _isPlaying;

    private void Awake()
    {
        _topMPB = new MaterialPropertyBlock();
        _botMPB = new MaterialPropertyBlock();

        // Fijamos orientación por renderer UNA sola vez
        if (_topLiquidRenderer)
        {
            _topLiquidRenderer.GetPropertyBlock(_topMPB);
            _topMPB.SetFloat(UpDownID, 1f); // arriba: “baja” el fill
            _topLiquidRenderer.SetPropertyBlock(_topMPB);
        }
        if (_bottomLiquidRenderer)
        {
            _bottomLiquidRenderer.GetPropertyBlock(_botMPB);
            _botMPB.SetFloat(UpDownID, 0f); // abajo: “sube” el fill
            _bottomLiquidRenderer.SetPropertyBlock(_botMPB);
        }

        // <<< CAMBIO: aplicamos el estado inicial coherente (Top lleno / Bottom vacío) >>>

        ApplyFills(_tCurrent, force:true);
    }

    private void Update()
    {
        if (_isPlaying)
        {
            _elapsed += Time.deltaTime;
            float n01 = Mathf.Clamp01(_elapsed / _transitionDuration);
            float shaped = _curve.Evaluate(n01);

            // Interpolamos hacia el target con la forma de la curva
            float t = Mathf.LerpUnclamped(_tCurrent, _tTarget, shaped);
            ApplyFills(t);

            if (n01 >= 1f - Mathf.Epsilon)
            {
                _tCurrent = _tTarget;
                _isPlaying = false;
                ApplyFills(_tCurrent, force:true);
            }
        }
        else
        {
            // Asegura que el MPB siga aplicado aunque no haya animación
            ApplyFills(_tCurrent);
        }
    }

    /// <summary>Vacía el slot superior y llena el inferior (como cuando te quedás sin vendas).</summary>
    public void StartCountdown()
    {
        _tTarget = 1f; // Top=1-1=0 (vacío), Bottom=1 (lleno)
        _elapsed = 0f;
        _isPlaying = true;
    }

    /// <summary>Llena el slot superior y vacía el inferior (cuando recuperás vendas).</summary>
    public void ResetAndFill()
    {
        _tTarget = 0f; // Top=1-0=1 (lleno), Bottom=0 (vacío)
        _elapsed = 0f;
        _isPlaying = true;
    }

    /// <summary>Aplica estado inmediato según # de vendas (0 = contando; >0 = reseteado).</summary>
    public void SnapByBandageCount(int currentBandages)
    {
        _isPlaying = false;
        _tCurrent = (currentBandages == 0) ? 1f : 0f;
        ApplyFills(_tCurrent, force:true);
    }

    private void ApplyFills(float t, bool force = false)
    {
        _tCurrent = t;
        float topFill = 1f - t;
        float botFill = t;

        if (_topLiquidRenderer)
        {
            _topLiquidRenderer.GetPropertyBlock(_topMPB);
            _topMPB.SetFloat(FillID, topFill);
            _topLiquidRenderer.SetPropertyBlock(_topMPB);
        }

        if (_bottomLiquidRenderer)
        {
            _bottomLiquidRenderer.GetPropertyBlock(_botMPB);
            _botMPB.SetFloat(FillID, botFill);
            _bottomLiquidRenderer.SetPropertyBlock(_botMPB);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (_topMPB == null) _topMPB = new MaterialPropertyBlock();
            if (_botMPB == null) _botMPB = new MaterialPropertyBlock();
            // Mantener vista coherente en editor
            ApplyFills(_tCurrent, force:true);
        }
    }
#endif
}
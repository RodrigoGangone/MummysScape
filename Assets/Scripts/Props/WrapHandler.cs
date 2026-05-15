using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary> 
/// Animador de Material (Wrap): Controla visualmente el efecto de "envolvimiento" mediante 
/// MaterialPropertyBlocks, optimizando el rendimiento al animar propiedades de shader sin instanciar materiales. 
/// </summary>
public class WrapHandler : MonoBehaviour
{
    [Header("Configuración Shader")]
    [Tooltip("Nombre de la propiedad en Shader Graph (ej: _Offset, _Fill)")]
    [SerializeField]
    private string _propertyName = "_Offset";

    [FormerlySerializedAs("_unWrapValue")] [FormerlySerializedAs("_initValue")] [Header("Valores")] [SerializeField]
    private float _unwrapValue;

    [SerializeField] private float _wrapValue;

    [Header("Tiempos")] [SerializeField] private float _wrapDuration = 0.5f;
    [SerializeField] private float _unwrapDuration = 0.5f;

    public float WrapDuration => _wrapDuration;

    [Header("Referencias")] [SerializeField]
    private Renderer[] _renderers;

    [SerializeField] private FxBank bank;

    private MaterialPropertyBlock _propBlock;
    private int _propID;
    private Coroutine _currentCoroutine;

    private float _currentValue = 0f;

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _propID = Shader.PropertyToID(_propertyName);

        _currentValue = _unwrapValue;
        ApplyValue(_currentValue);
    }

    [ContextMenu("Wrap (0 -> 1)")]
    public void Wrap()
    {
        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);

        _currentCoroutine = StartCoroutine(AnimateToValue(_wrapValue, _wrapDuration));

        bank.Play3D(SfxIDs.Wrap.Wrap_Key, transform.position);
    }

    [ContextMenu("UnWrap (1 -> 0)")]
    public void UnWrap()
    {
        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);

        _currentCoroutine = StartCoroutine(AnimateToValue(_unwrapValue, _unwrapDuration));

        bank.Play3D(SfxIDs.Wrap.UnWrap, transform.position);
    }

    private IEnumerator AnimateToValue(float targetValue, float duration)
    {
        float startValue = _currentValue;
        float timer = 0f;

        if (duration <= 0.01f)
        {
            _currentValue = targetValue;
            ApplyValue(_currentValue);
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            _currentValue = Mathf.Lerp(startValue, targetValue, t);

            ApplyValue(_currentValue);
            yield return null;
        }

        _currentValue = targetValue;
        ApplyValue(_currentValue);
    }

    private void ApplyValue(float value)
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;

            r.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_propID, value);
            r.SetPropertyBlock(_propBlock);
        }
    }
}
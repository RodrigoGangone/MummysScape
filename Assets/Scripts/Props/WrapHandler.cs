using System.Collections;
using UnityEngine;

public class WrapHandler : MonoBehaviour
{
    [Header("Configuración Shader")]
    [Tooltip("Nombre de la propiedad en Shader Graph (ej: _Offset, _Fill)")]
    [SerializeField] private string _propertyName = "_Offset";
    
    [Header("Tiempos")]
    [SerializeField] private float _wrapDuration = 0.5f;   // Tiempo 0 a 1
    [SerializeField] private float _unwrapDuration = 0.5f; // Tiempo 1 a 0

    public float WrapDuration => _wrapDuration;
    
    [Header("Referencias")]
    [SerializeField] private Renderer[] _renderers;

    private MaterialPropertyBlock _propBlock;
    private int _propID;
    private Coroutine _currentCoroutine;
    
    private float _currentValue = 0f; 

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        _propID = Shader.PropertyToID(_propertyName);

        _currentValue = 0f;
        ApplyValue(_currentValue);
    }

    [ContextMenu("Wrap (0 -> 1)")]
    public void Wrap()
    {
        // Si ya hay una animación corriendo, la paramos y empezamos desde donde quedó
        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        
        // Iniciamos corrutina hacia 1
        _currentCoroutine = StartCoroutine(AnimateToValue(1f, _wrapDuration));
    }

    [ContextMenu("UnWrap (1 -> 0)")]
    public void UnWrap()
    {
        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        
        // Iniciamos corrutina hacia 0
        _currentCoroutine = StartCoroutine(AnimateToValue(0f, _unwrapDuration));
    }

    // Corrutina Genérica para ir de "Lo que sea que tenga ahora" -> "Objetivo"
    private IEnumerator AnimateToValue(float targetValue, float duration)
    {
        float startValue = _currentValue;
        float timer = 0f;

        // Evitamos división por cero si la duración es muy pequeña
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
            
            // Lerp desde donde estaba (startValue) hasta donde quiero ir (targetValue)
            _currentValue = Mathf.Lerp(startValue, targetValue, t);
            
            ApplyValue(_currentValue);
            yield return null;
        }

        // Asegurar valor final exacto
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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anima representaciones 2D de gemas entre una posición del mundo y un punto del Canvas.
/// No conoce el motivo del vuelo ni modifica contadores o estado persistente.
/// </summary>
public class GemFlightAnimator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private GameObject _gemUiPrefab;

    [Header("Movimiento")]
    [SerializeField, Min(0.01f)] private float _travelDuration = 0.8f;
    [SerializeField] private float _curveHeight = 100f;

    private readonly List<FlightHandle> _activeFlights = new();

    public int ActiveFlightCount => _activeFlights.Count;

    public sealed class FlightHandle
    {
        internal GemFlightAnimator Owner;
        internal RectTransform Visual;
        internal Coroutine Routine;
        internal Vector2 TargetPosition;
        internal Action OnComplete;
        internal bool IsCompleted;

        public void CompleteImmediately()
        {
            Owner?.CompleteFlight(this);
        }
    }

    public FlightHandle PlayFromWorld(Vector3 worldPosition, RectTransform target, Action onComplete = null)
    {
        if (target == null)
        {
            Debug.LogWarning("[GemFlightAnimator] No se asignó un destino para la gema.", this);
            onComplete?.Invoke();
            return null;
        }

        return PlayFromWorld(worldPosition, RectTransformToCanvasPosition(target), onComplete);
    }

    public FlightHandle PlayFromWorld(Vector3 worldPosition, Vector2 targetCanvasPosition, Action onComplete = null)
    {
        if (!TryResolveReferences())
        {
            onComplete?.Invoke();
            return null;
        }

        GameObject instance = Instantiate(_gemUiPrefab, _canvasRect);
        if (!instance.TryGetComponent(out RectTransform gemRect))
        {
            Debug.LogError("[GemFlightAnimator] El prefab configurado no posee RectTransform.", instance);
            Destroy(instance);
            onComplete?.Invoke();
            return null;
        }

        gemRect.anchoredPosition = WorldToCanvasPosition(worldPosition);

        var handle = new FlightHandle
        {
            Owner = this,
            Visual = gemRect,
            TargetPosition = targetCanvasPosition,
            OnComplete = onComplete
        };

        _activeFlights.Add(handle);
        handle.Routine = StartCoroutine(AnimateFlight(handle));
        return handle;
    }

    public void CompleteAllImmediately()
    {
        FlightHandle[] pending = _activeFlights.ToArray();
        foreach (FlightHandle handle in pending)
            CompleteFlight(handle);
    }

    public Vector2 RectTransformToCanvasPosition(RectTransform element)
    {
        if (_canvasRect == null || element == null)
            return Vector2.zero;

        Camera eventCamera = GetCanvasEventCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, element.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPoint,
            eventCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private IEnumerator AnimateFlight(FlightHandle handle)
    {
        float elapsed = 0f;
        Vector2 startPosition = handle.Visual.anchoredPosition;
        Vector2 middlePoint = Vector2.Lerp(startPosition, handle.TargetPosition, 0.5f) + Vector2.up * _curveHeight;

        while (elapsed < _travelDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / _travelDuration);
            float easedTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);

            Vector2 firstHalf = Vector2.Lerp(startPosition, middlePoint, easedTime);
            Vector2 secondHalf = Vector2.Lerp(middlePoint, handle.TargetPosition, easedTime);
            handle.Visual.anchoredPosition = Vector2.Lerp(firstHalf, secondHalf, easedTime);
            yield return null;
        }

        CompleteFlight(handle);
    }

    private void CompleteFlight(FlightHandle handle)
    {
        if (handle == null || handle.IsCompleted)
            return;

        handle.IsCompleted = true;

        if (handle.Routine != null)
            StopCoroutine(handle.Routine);

        if (handle.Visual != null)
        {
            handle.Visual.anchoredPosition = handle.TargetPosition;
            Destroy(handle.Visual.gameObject);
        }

        _activeFlights.Remove(handle);
        Action callback = handle.OnComplete;
        handle.Owner = null;
        handle.Routine = null;
        handle.OnComplete = null;
        callback?.Invoke();
    }

    private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
    {
        Vector2 screenPoint = _worldCamera.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPoint,
            GetCanvasEventCamera(),
            out Vector2 localPoint);

        return localPoint;
    }

    private bool TryResolveReferences()
    {
        if (_worldCamera == null)
            _worldCamera = Camera.main;

        if (_canvasRect == null)
            _canvasRect = transform as RectTransform;

        if (_worldCamera != null && _canvasRect != null && _gemUiPrefab != null)
            return true;

        Debug.LogError("[GemFlightAnimator] Faltan cámara, Canvas RectTransform o prefab de gema.", this);
        return false;
    }

    private Camera GetCanvasEventCamera()
    {
        if (_canvasRect == null)
            return null;

        Canvas canvas = _canvasRect.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }
}

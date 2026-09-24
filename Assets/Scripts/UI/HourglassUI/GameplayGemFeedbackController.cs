using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquesta el vuelo de las gemas recogidas hacia el HUD durante gameplay.
/// La representación 3D se actualiza mediante OnGemReachedUI al finalizar el vuelo.
/// </summary>
public class GameplayGemFeedbackController : MonoBehaviour
{
    [SerializeField] private RectTransform _hudRoot;
    [SerializeField] private GemFlightAnimator _flightAnimator;
    [SerializeField] private RectTransform[] _gemTargets;

    private readonly Dictionary<GemFlightAnimator.FlightHandle, int> _pendingFlights = new();
    private bool _endingLevel;

    private void OnGemPicked(GemPickupData pickup)
    {
        if (_endingLevel)
        {
            RaiseArrival(pickup.GemNumber);
            return;
        }

        RectTransform target = GetTarget(pickup.GemNumber);
        if (_flightAnimator == null || target == null)
        {
            Debug.LogWarning($"[GameplayGemFeedback] No se pudo animar la gema {pickup.GemNumber}; se completa inmediatamente.", this);
            RaiseArrival(pickup.GemNumber);
            return;
        }

        GemFlightAnimator.FlightHandle handle = null;
        handle = _flightAnimator.PlayFromWorld(
            pickup.WorldPosition,
            target,
            () => HandleArrival(handle, pickup.GemNumber));

        if (handle != null && !handle.IsCompleted)
            _pendingFlights[handle] = pickup.GemNumber;
    }

    private void HandleArrival(GemFlightAnimator.FlightHandle handle, int gemNumber)
    {
        if (handle != null)
            _pendingFlights.Remove(handle);

        RaiseArrival(gemNumber);
    }

    private RectTransform GetTarget(int gemNumber)
    {
        int index = gemNumber - 1;
        return _gemTargets != null && index >= 0 && index < _gemTargets.Length
            ? _gemTargets[index]
            : null;
    }

    private static void RaiseArrival(int gemNumber)
    {
        GameEventManager.Instance.levelEvents.OnGemReachedUI.Raise(gemNumber);
    }

    private void HandleWin(int _) => CompleteAndHide();
    private void HandleDeath() => CompleteAndHide();

    private void CompleteAndHide()
    {
        if (_endingLevel)
            return;

        _endingLevel = true;
        _flightAnimator?.CompleteAllImmediately();
        _pendingFlights.Clear();

        if (_hudRoot != null)
            _hudRoot.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _endingLevel = false;
        GameEventManager.Instance.levelEvents.OnPickedGem.Register<GemPickupData>(OnGemPicked);
        GameEventManager.Instance.levelEvents.OnWin.Register<int>(HandleWin);
        GameEventManager.Instance.levelEvents.OnDeath.Register(HandleDeath);
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance == null)
            return;

        GameEventManager.Instance.levelEvents.OnPickedGem.Unregister<GemPickupData>(OnGemPicked);
        GameEventManager.Instance.levelEvents.OnWin.Unregister<int>(HandleWin);
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(HandleDeath);
    }
}

using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Coordina en Selector el conteo de gemas globales pendientes de mostrar.
/// El movimiento visual está delegado a GemFlightAnimator.
/// </summary>
public class GemCounterAnimator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GemFlightAnimator _flightAnimator;
    [SerializeField] private RectTransform _targetGemUI;
    [SerializeField] private TextMeshProUGUI _totalGemsText;
    [SerializeField] private Transform _playerTransform;

    [Header("Configuración")]
    [SerializeField] private float _delayBetweenGems = 0.15f;
    [SerializeField] private float _initialWaitTime = 2f;
    [SerializeField] private Vector3 _iconPunchScale = new(1.3f, 1.3f, 1.3f);

    private Coroutine _punchRoutine;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        if (FocusManager.Instance != null && FocusManager.Instance.IsBusy)
        {
            while (FocusManager.Instance.IsBusy)
                yield return null;

            yield return new WaitForSeconds(_initialWaitTime);
        }

        int actualTotal = Save.GetGlobalGemCount();
        int lastSeenGems = Save.GetSeenGemsCount();

        if (actualTotal > lastSeenGems)
        {
            _totalGemsText.text = lastSeenGems.ToString();
            yield return SequenceRoutine(lastSeenGems, actualTotal - lastSeenGems, actualTotal);
        }
        else
        {
            _totalGemsText.text = actualTotal.ToString();
        }
    }

    private IEnumerator SequenceRoutine(int startCount, int amount, int finalTotal)
    {
        yield return new WaitForSeconds(0.3f);

        if (_flightAnimator == null)
        {
            Debug.LogError("[GemCounterAnimator] Falta asignar GemFlightAnimator.", this);
            _totalGemsText.text = finalTotal.ToString();
            Save.UpdateSeenGemsCount(finalTotal);
            yield break;
        }

        Vector3 playerPosition = _playerTransform != null ? _playerTransform.position : Vector3.zero;
        int currentCount = startCount;
        int completedFlights = 0;

        for (int i = 0; i < amount; i++)
        {
            _flightAnimator.PlayFromWorld(playerPosition + Vector3.up * 1.5f, _targetGemUI, () =>
            {
                currentCount++;
                completedFlights++;
                _totalGemsText.text = currentCount.ToString();

                if (_punchRoutine != null)
                    StopCoroutine(_punchRoutine);

                _punchRoutine = StartCoroutine(PunchIcon());
            });

            yield return new WaitForSeconds(_delayBetweenGems);
        }

        while (completedFlights < amount)
            yield return null;

        Save.UpdateSeenGemsCount(finalTotal);
    }

    private IEnumerator PunchIcon()
    {
        _targetGemUI.localScale = _iconPunchScale;
        yield return new WaitForSeconds(0.1f);
        _targetGemUI.localScale = Vector3.one;
        _punchRoutine = null;
    }
}

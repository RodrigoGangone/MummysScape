using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial asociado")]
    [SerializeField] private TutorialFocusPoint focusPoint;

    private bool _playerInside;

    private void Reset()
    {
        // Aseguramos que el Collider sea trigger y tenga un tamaño visible
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        if (FocusManager.Instance == null) return;
        if (focusPoint == null) return;

        _playerInside = true;

        // Primera activación obligatoria.
        // Si ya fue vista, FocusManager la ignora internamente.
        FocusManager.Instance.RequestTutorialFirstTime(focusPoint);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;
        _playerInside = false;
    }

    private void Update()
    {
        if (!_playerInside) return;
        if (FocusManager.Instance == null) return;
        if (focusPoint == null) return;

        if (Input.GetKeyDown(FocusManager.Instance.TutorialKey))
        {
            // Re-visualización opcional del tutorial
            FocusManager.Instance.RequestTutorialOptional(focusPoint);
        }
    }
}

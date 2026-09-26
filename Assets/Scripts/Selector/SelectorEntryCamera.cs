using Cinemachine;
using UnityEngine;

public class SelectorEntryCamera : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField]
    private CinemachineVirtualCamera entryCamera;

    [Header("Priority")]
    [SerializeField]
    private int inactivePriority = 0;

    [SerializeField]
    private int activePriority = 200;

    public bool IsActive =>
        entryCamera != null &&
        entryCamera.Priority == activePriority;

    private void Awake()
    {
        Deactivate();
    }

    public void Activate()
    {
        if (entryCamera == null)
            return;

        // Evita que Cinemachine reutilice el estado anterior
        // cuando el EntryRig acaba de cambiar de nivel.
        entryCamera.PreviousStateIsValid = false;

        entryCamera.Priority =
            activePriority;
    }

    public void Deactivate()
    {
        if (entryCamera == null)
            return;

        entryCamera.Priority =
            inactivePriority;
    }

    private void OnDisable()
    {
        Deactivate();
    }
}
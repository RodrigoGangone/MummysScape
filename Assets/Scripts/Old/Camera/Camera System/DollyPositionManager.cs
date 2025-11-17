using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class DollyPositionManager : MonoBehaviour
{
    [SerializeField] private float transitionSpeed = 2f;

    private CinemachineVirtualCamera vcam;
    private CinemachineTrackedDolly dolly;

    private Coroutine moveRoutine;

    // Para bloquear cambios por zonas cuando hay plano especial
    public bool ZonesLocked { get; private set; }

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        dolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();

        dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
    }

    public void SetZoneDollyPosition(int waypointIndex)
    {
        if (ZonesLocked) return;                  
        if (dolly.m_Path == null) return;

        float min = dolly.m_Path.MinPos;
        float max = dolly.m_Path.MaxPos;

        float target = Mathf.Clamp(waypointIndex, min, max);

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToPosition(target));
    }

    public void SetZonesLocked(bool locked)
    {
        ZonesLocked = locked;

        if (locked && moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

private IEnumerator MoveToPosition(float target)
    {
        var path = dolly.m_Path;
        if (path == null) yield break;

        float min = path.MinPos;
        float max = path.MaxPos;
        bool looped = path.Looped;

        target = Mathf.Clamp(target, min, max);

        // Usamos un umbral un poco más generoso que 0.001f
        while (Mathf.Abs(dolly.m_PathPosition - target) > 0.01f)
        {
            float current = dolly.m_PathPosition;

            if (looped)
            {
                // Asegurarnos de que el rango no sea cero
                float pathRange = max - min;
                if (pathRange <= Mathf.Epsilon) yield break; // Evita división por cero

                float currentNorm = (current - min) / pathRange;
                float targetNorm = (target - min) / pathRange;

                float currentAngle = currentNorm * 360f;
                float targetAngle = targetNorm * 360f;

                // --- CAMBIO CLAVE (Looped) ---
                // Convertir la velocidad de "unidades/seg" a "grados/seg"
                float degreesPerPathUnit = 360f / pathRange;
                float angularSpeed = transitionSpeed * degreesPerPathUnit;

                float nextAngle = Mathf.MoveTowardsAngle(
                    currentAngle,
                    targetAngle,
                    angularSpeed * Time.deltaTime
                );
                // --- FIN DEL CAMBIO ---

                float nextNorm = nextAngle / 360f;
                dolly.m_PathPosition = min + nextNorm * pathRange;
            }
            else
            {
                // --- CAMBIO CLAVE (No Looped) ---
                // Usar MoveTowards para una velocidad constante
                float next = Mathf.MoveTowards(
                    current,
                    target,
                    transitionSpeed * Time.deltaTime
                );
                // --- FIN DEL CAMBIO ---

                dolly.m_PathPosition = next;
            }

            yield return null;
        }

        // Asegurar la posición final exacta
        dolly.m_PathPosition = target;
        moveRoutine = null;
    }
}

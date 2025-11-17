using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class DollyPositionManager : MonoBehaviour
{
    [System.Serializable]
    public struct DollyTrack
    {
        public string id;                 // Ej: "Ground", "Water"
        public CinemachinePathBase path;  // El Dolly Track de Cinemachine
    }

    [Header("Configuración de movimiento")]
    [SerializeField] private float transitionSpeed = 2f;

    [Header("Tracks disponibles")]
    [SerializeField] private DollyTrack[] tracks;

    private CinemachineVirtualCamera vcam;
    private CinemachineTrackedDolly dolly;

    private Coroutine moveRoutine;
    private int currentTrackIndex = -1;

    // Para bloquear cambios por zonas cuando hay plano especial
    public bool ZonesLocked { get; private set; }

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        dolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();

        // NO tocamos el m_Path aquí para no romper tu setup actual.
        // Solo nos encargamos si nos piden un track por ID.
        dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
    }

    // ------------------------------------------------------------------
    //  API EXISTENTE (por compatibilidad)
    //  Usa el path actual que ya tenga asignado la vcam
    // ------------------------------------------------------------------
    public void SetZoneDollyPosition(int waypointIndex)
    {
        if (ZonesLocked) return;
        if (dolly.m_Path == null) return;

        float min = dolly.m_Path.MinPos;
        float max = dolly.m_Path.MaxPos;
        float target = Mathf.Clamp(waypointIndex, min, max);

        StartMoveRoutine(target);
    }

    // ------------------------------------------------------------------
    //  NUEVA API: Multi-track (trackId + waypoint)
    // ------------------------------------------------------------------
    public void SetZoneDollyPosition(string trackId, int waypointIndex)
    {
        if (ZonesLocked) return;

        int idx = FindTrackIndex(trackId);
        if (idx < 0)
        {
            Debug.LogWarning($"[DollyPositionManager] No se encontró track con id '{trackId}'.");
            return;
        }

        SetCurrentTrack(idx);

        if (dolly.m_Path == null) return;

        float min = dolly.m_Path.MinPos;
        float max = dolly.m_Path.MaxPos;
        float target = Mathf.Clamp(waypointIndex, min, max);

        StartMoveRoutine(target);
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

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------
    private int FindTrackIndex(string trackId)
    {
        if (tracks == null) return -1;

        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i].id == trackId)
                return i;
        }

        return -1;
    }

    private void SetCurrentTrack(int index)
    {
        if (index == currentTrackIndex) return;
        if (index < 0 || index >= tracks.Length) return;

        currentTrackIndex = index;
        var track = tracks[index];

        if (track.path == null)
        {
            Debug.LogWarning($"[DollyPositionManager] Track '{track.id}' no tiene path asignado.");
            return;
        }

        dolly.m_Path = track.path;
        dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
    }

    private void StartMoveRoutine(float target)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToPosition(target));
    }

    private IEnumerator MoveToPosition(float target)
    {
        var path = dolly.m_Path;
        if (path == null) yield break;

        float min = path.MinPos;
        float max = path.MaxPos;
        bool looped = path.Looped;

        target = Mathf.Clamp(target, min, max);

        while (Mathf.Abs(dolly.m_PathPosition - target) > 0.01f)
        {
            float current = dolly.m_PathPosition;

            if (looped)
            {
                float pathRange = max - min;
                if (pathRange <= Mathf.Epsilon) yield break;

                float currentNorm = (current - min) / pathRange;
                float targetNorm = (target - min) / pathRange;

                float currentAngle = currentNorm * 360f;
                float targetAngle = targetNorm * 360f;

                float degreesPerPathUnit = 360f / pathRange;
                float angularSpeed = transitionSpeed * degreesPerPathUnit;

                float nextAngle = Mathf.MoveTowardsAngle(
                    currentAngle,
                    targetAngle,
                    angularSpeed * Time.deltaTime
                );

                float nextNorm = nextAngle / 360f;
                dolly.m_PathPosition = min + nextNorm * pathRange;
            }
            else
            {
                float next = Mathf.MoveTowards(
                    current,
                    target,
                    transitionSpeed * Time.deltaTime
                );

                dolly.m_PathPosition = next;
            }

            yield return null;
        }

        dolly.m_PathPosition = target;
        moveRoutine = null;
    }
}

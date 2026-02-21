using UnityEngine;
using Cinemachine;
using System.Collections;

/// <summary>
/// SISTEMA DESCARTADO
/// Gestor de Dolly: Controla el movimiento de cámaras sobre rieles (Dolly Tracks), administrando transiciones 
/// suaves de posición y velocidad para recorridos cinematográficos o seguimiento específico. 
/// </summary>

public class DollyPositionManager : MonoBehaviour
{
[System.Serializable]
    public struct DollyTrackCamera
    {
        [Header("Configuración de VCam")]
        public string id;           
        public CinemachineVirtualCamera vcam;

        [Header("Configuración de movimiento")]
        [Tooltip("Velocidad de transición entre waypoints para esta cámara")]
        public float transitionSpeed;

        [HideInInspector] public CinemachineTrackedDolly dolly;
    }

    [Header("Prioridades")]
    [SerializeField] private int basePriority = 5;
    [SerializeField] private int activePriority = 10;

    [Header("Cámaras Gestionadas")]
    [SerializeField] private DollyTrackCamera[] cameras;

    private int currentTrackIndex = -1;
    private Coroutine moveRoutine;

    public bool ZonesLocked { get; private set; }

    private void Awake()
    {
        InitializeCameras();
    }

    private void InitializeCameras()
    {
        if (cameras == null || cameras.Length == 0)
        {
            Debug.LogError("[DollyCameraManager] No hay cámaras asignadas.");
            return;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].vcam == null)
            {
                Debug.LogWarning($"[DollyCameraManager] La cámara con id '{cameras[i].id}' no tiene VCam asignada.");
                continue;
            }

            cameras[i].vcam.Priority = basePriority;

            cameras[i].dolly = cameras[i].vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
            if (cameras[i].dolly != null)
            {
                cameras[i].dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
                
                cameras[i].dolly.m_AutoDolly.m_Enabled = true;
            }
            else
            {
                Debug.LogWarning($"[DollyCameraManager] La VCam '{cameras[i].vcam.name}' (id: {cameras[i].id}) no tiene un componente CinemachineTrackedDolly.");
            }
        }

        currentTrackIndex = 0;
        if (cameras.Length > 0)
        {
            var firstCam = cameras[currentTrackIndex];
            firstCam.vcam.Priority = activePriority;

            if (firstCam.dolly != null)
                firstCam.dolly.m_AutoDolly.m_Enabled = false;
        }
    }

    public void SetZoneDollyPosition(string trackId, int waypointIndex)
    {
        if (ZonesLocked) return;

        int idx = FindTrackIndex(trackId);
        if (idx < 0)
        {
            Debug.LogWarning($"[DollyCameraManager] No se encontró track con id '{trackId}'.");
            return;
        }

        SetActiveCamera(idx);

        MoveActiveCameraToWaypoint(waypointIndex);
    }

    public void SetZoneDollyPosition(int waypointIndex)
    {
        if (ZonesLocked) return;
        if (currentTrackIndex < 0) return;

        MoveActiveCameraToWaypoint(waypointIndex);
    }

    private void SetActiveCamera(int index)
    {
        if (index == currentTrackIndex) return; 
        if (index < 0 || index >= cameras.Length) return;

        if (currentTrackIndex >= 0)
        {
            var oldCam = cameras[currentTrackIndex];
            oldCam.vcam.Priority = basePriority;

            if (oldCam.dolly != null)
            {
                oldCam.dolly.m_AutoDolly.m_Enabled = true;
            }
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        currentTrackIndex = index;
        var newCam = cameras[currentTrackIndex];
        newCam.vcam.Priority = activePriority;

        if (newCam.dolly != null)
        {
            newCam.dolly.m_AutoDolly.m_Enabled = false;
        }
    }
    private void MoveActiveCameraToWaypoint(int waypointIndex)
    {
        if (currentTrackIndex < 0) return;

        var activeCam = cameras[currentTrackIndex];
        if (activeCam.dolly == null || activeCam.dolly.m_Path == null)
        {
            Debug.LogWarning($"[DollyCameraManager] La cámara activa '{activeCam.id}' no tiene path o dolly.");
            return;
        }

        float min = activeCam.dolly.m_Path.MinPos;
        float max = activeCam.dolly.m_Path.MaxPos;
        float target = Mathf.Clamp(waypointIndex, min, max);

        StartMoveRoutine(activeCam.dolly, target, activeCam.transitionSpeed);
    }

    private int FindTrackIndex(string trackId)
    {
        if (cameras == null) return -1;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i].id == trackId)
                return i;
        }
        return -1;
    }

    private void StartMoveRoutine(CinemachineTrackedDolly dolly, float target, float speed)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToPosition(dolly, target, speed));
    }

    private IEnumerator MoveToPosition(CinemachineTrackedDolly dolly, float target, float speed)
    {
        var path = dolly.m_Path;
        if (path == null) yield break;

        float min = path.MinPos;
        float max = path.MaxPos;
        bool looped = path.Looped;

        target = Mathf.Clamp(target, min, max);

        while (Mathf.Abs(dolly.m_PathPosition - target) > 0.01f)
        {
            // Asegurarnos de que no nos "robaron" la cámara
            if (dolly != cameras[currentTrackIndex].dolly)
            {
                moveRoutine = null;
                yield break;
            }

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
                float angularSpeed = speed * degreesPerPathUnit;

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
                    speed * Time.deltaTime
                );

                dolly.m_PathPosition = next;
            }

            yield return null;
        }

        dolly.m_PathPosition = target;
        moveRoutine = null;
    }
}
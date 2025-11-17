using UnityEngine;
using Cinemachine;
using System.Collections;

public class DollyPositionManager : MonoBehaviour
{
[System.Serializable]
    public struct DollyTrackCamera
    {
        [Header("Configuración de VCam")]
        public string id;                 // Ej: "Ground", "Water"
        public CinemachineVirtualCamera vcam; // La vcam específica para este track

        [Header("Configuración de movimiento")]
        [Tooltip("Velocidad de transición entre waypoints para esta cámara")]
        public float transitionSpeed;

        // Almacenados en Awake 
        [HideInInspector] public CinemachineTrackedDolly dolly;
    }

    [Header("Prioridades")]
    [SerializeField] private int basePriority = 5;
    [SerializeField] private int activePriority = 10;

    [Header("Cámaras Gestionadas")]
    [SerializeField] private DollyTrackCamera[] cameras;

    private int currentTrackIndex = -1;
    private Coroutine moveRoutine;

    // Para bloquear cambios por zonas cuando hay plano especial
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

            // Asignar prioridad base
            cameras[i].vcam.Priority = basePriority;

            // Obtener y configurar el componente Dolly
            cameras[i].dolly = cameras[i].vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
            if (cameras[i].dolly != null)
            {
                cameras[i].dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
                
                // --- NUEVO ---
                // Habilitamos AutoDolly por defecto en todas las cámaras inactivas.
                // Asumimos que están configuradas para seguir al player.
                cameras[i].dolly.m_AutoDolly.m_Enabled = true;
            }
            else
            {
                Debug.LogWarning($"[DollyCameraManager] La VCam '{cameras[i].vcam.name}' (id: {cameras[i].id}) no tiene un componente CinemachineTrackedDolly.");
            }
        }

        // Activar la primera cámara de la lista por defecto
        currentTrackIndex = 0;
        if (cameras.Length > 0) // Chequeo de seguridad
        {
            var firstCam = cameras[currentTrackIndex];
            firstCam.vcam.Priority = activePriority;

            // --- NUEVO ---
            // Deshabilitamos AutoDolly SÓLO en la cámara que empieza activa.
            if (firstCam.dolly != null)
            {
                firstCam.dolly.m_AutoDolly.m_Enabled = false;
            }
        }
    }
    // ------------------------------------------------------------------
    //  API (Multi-track)
    //  Usada por CameraZoneTrigger
    // ------------------------------------------------------------------
    public void SetZoneDollyPosition(string trackId, int waypointIndex)
    {
        if (ZonesLocked) return;

        int idx = FindTrackIndex(trackId);
        if (idx < 0)
        {
            Debug.LogWarning($"[DollyCameraManager] No se encontró track con id '{trackId}'.");
            return;
        }

        // Cambia la cámara activa (prioridad)
        SetActiveCamera(idx);

        // Mueve la cámara activa a su waypoint
        MoveActiveCameraToWaypoint(waypointIndex);
    }

    // ------------------------------------------------------------------
    //  API (Compatibilidad)
    //  Mueve la cámara que YA esté activa
    // ------------------------------------------------------------------
    public void SetZoneDollyPosition(int waypointIndex)
    {
        if (ZonesLocked) return;
        if (currentTrackIndex < 0) return; // No hay cámara activa

        MoveActiveCameraToWaypoint(waypointIndex);
    }

    // ------------------------------------------------------------------
    //  API para FocusManager
    // ------------------------------------------------------------------

    public void SetZonesLocked(bool locked)
    {
        ZonesLocked = locked;

        if (locked && moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    /**
     * Activa o desactiva la cámara de gameplay actual.
     * 'FocusManager' llamará a esto con 'false' para ceder
     * prioridad a la 'focusCam'.
     */
    public void ActivateGameplayCamera(bool active)
    {
        if (currentTrackIndex < 0 || currentTrackIndex >= cameras.Length) return;

        // Si 'active' es true, le da prioridad 'activePriority' (10)
        // Si 'active' es false, le da prioridad 0 (para ceder a la focus cam)
        cameras[currentTrackIndex].vcam.Priority = active ? activePriority : 0;
    }


    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------

    private void SetActiveCamera(int index)
    {
        if (index == currentTrackIndex) return; // Ya es la activa
        if (index < 0 || index >= cameras.Length) return;

        // 1. Desactiva la cámara anterior (si hay)
        if (currentTrackIndex >= 0)
        {
            var oldCam = cameras[currentTrackIndex];
            oldCam.vcam.Priority = basePriority;

            // --- NUEVO ---
            // HABILITAR AutoDolly en la cámara que se vuelve inactiva
            if (oldCam.dolly != null)
            {
                oldCam.dolly.m_AutoDolly.m_Enabled = true;
            }
        }

        // 2. Detiene cualquier movimiento en curso
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        // 3. Activa la nueva cámara
        currentTrackIndex = index;
        var newCam = cameras[currentTrackIndex];
        newCam.vcam.Priority = activePriority;

        // --- NUEVO ---
        // DESHABILITAR AutoDolly en la cámara que se vuelve activa
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
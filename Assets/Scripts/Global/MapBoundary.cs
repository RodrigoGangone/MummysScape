using System;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapBoundary : MonoBehaviour
{
    [Header("--- Configuración de Zonas ---")] [SerializeField]
    private float safeRadius = 50f;

    [SerializeField] private float warningRadius = 70f;
    [SerializeField] private float killRadius = 85f;

    [Header("--- Lógica Vertical ---")] [SerializeField]
    private float minYLimit = -20f;

    [CanBeNull] private Transform Player;
    [CanBeNull] private ParticleSystem warningParticle;

    private bool _isPlayerDead;
    private Vector3 _debugThreatPos;

    [Header("Opciones de Gizmo")] [SerializeField]
    private bool painted;

    private void Start()
    {
        Player = FindObjectOfType<PlayerController>()?.Ctx?.Tf;
        warningParticle = GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        if (Player == null || _isPlayerDead) return;

        var centerPos = transform.position;
        var centerPosXZ = new Vector3(centerPos.x, 0, centerPos.z);
        var playerPosXZ = new Vector3(Player.position.x, 0, Player.position.z);

        var distXZ = Vector3.Distance(centerPosXZ, playerPosXZ);
        var dirToPlayer = (playerPosXZ - centerPosXZ).normalized;
        var playerHeight = Player.position.y;

        ManageZoneState(distXZ, playerHeight, dirToPlayer);
    }

    private void ManageZoneState(float dist, float height, Vector3 dir)
    {
        if (height < minYLimit)
        {
            TriggerDeath();
            return;
        }

        if (dist > warningRadius)
        {
            TriggerDeath();
            return;
        }

        if (dist > safeRadius)
        {
            if (!warningParticle.isPlaying) warningParticle.Play();

            float threatDist = (warningRadius + killRadius) / 2f;
            Vector3 targetPos = transform.position + (dir * threatDist);
            targetPos.y = height;

            warningParticle.transform.position = targetPos;
            warningParticle.transform.LookAt(Player.position);

            _debugThreatPos = targetPos;
        }
        else
        {
            if (warningParticle.isPlaying)
                warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void TriggerDeath()
    {
        if (_isPlayerDead) return;

        _isPlayerDead = true;

        if (warningParticle) warningParticle.Stop();

        GameEventManager.Instance.levelEvents.OnDeath.Raise();
    }

    #region Gizmos Visuales

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // --- 1. DIBUJO DE ZONAS (Siempre visible) ---
        // Usamos Handles para discos sólidos.
        // Importante: Dibujar del más grande al más chico para que se superpongan bien (Painter's Algorithm)

        Handles.color = new Color(1, 0, 0, 1f); // Rojo sólido para el borde
        Handles.DrawWireDisc(transform.position, Vector3.up, killRadius);

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, warningRadius);

        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, Vector3.up, safeRadius);

        if (painted)
        {
            // ZONA MORTAL (Roja - Fondo)
            Handles.color = new Color(1, 0, 0, 0.1f); // Rojo muy transparente
            Handles.DrawSolidDisc(transform.position, Vector3.up, killRadius);

            // ZONA DE ADVERTENCIA (Amarilla - Medio)
            Handles.color = new Color(1, 0.92f, 0.016f, 0.15f); // Amarillo transparente
            Handles.DrawSolidDisc(transform.position, Vector3.up, warningRadius);

            // ZONA SEGURA (Verde - Centro/Arriba)
            Handles.color = new Color(0, 1, 0, 0.2f); // Verde transparente
            Handles.DrawSolidDisc(transform.position, Vector3.up, safeRadius);

            // PISO MORTAL (Plano abajo)
            Gizmos.color = new Color(0.5f, 0, 0, 0.3f);
            Vector3 floorCenter = new Vector3(transform.position.x, minYLimit, transform.position.z);
            Gizmos.DrawCube(floorCenter, new Vector3(killRadius * 2, 0.1f, killRadius * 2));
        }

        // --- 2. LÓGICA DE JUGADOR (Solo si existe) ---
        Transform gizmoTarget = Player;
        if (gizmoTarget == null && !Application.isPlaying)
            gizmoTarget = FindObjectOfType<PlayerController>()?.Ctx?.Tf;

        if (gizmoTarget != null)
        {
            Vector3 centerXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 playerXZ = new Vector3(gizmoTarget.position.x, 0, gizmoTarget.position.z);
            float dist = Vector3.Distance(centerXZ, playerXZ);
            Vector3 dir = (playerXZ - centerXZ).normalized;

            // Líneas de estado
            if (dist <= safeRadius)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(gizmoTarget.position, transform.position);
            }
            else if (dist <= warningRadius)
            {
                Gizmos.color = Color.yellow;
                Vector3 anchor = transform.position + (dir * safeRadius);
                anchor.y = gizmoTarget.position.y;
                Gizmos.DrawLine(gizmoTarget.position, anchor);

                // Visualizar punto de amenaza
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(gizmoTarget.position, _debugThreatPos);
                Gizmos.DrawWireSphere(_debugThreatPos, 1f);
            }
            else
            {
                Gizmos.color = Color.red;
                Vector3 anchor = transform.position + (dir * warningRadius);
                anchor.y = gizmoTarget.position.y;
                Gizmos.DrawLine(gizmoTarget.position, anchor);
            }

            // Línea de altura
            Gizmos.color = new Color(0.5f, 0, 0.5f, 0.5f);
            Gizmos.DrawLine(gizmoTarget.position,
                new Vector3(gizmoTarget.position.x, minYLimit, gizmoTarget.position.z));
        }
#endif
    }

    #endregion
}
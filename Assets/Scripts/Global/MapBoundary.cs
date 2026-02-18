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

    [Header("--- Visuales y Brazo ---")] [SerializeField]
    private ParticleSystem warningParticle;

    [SerializeField] private Animator monsterArmAnimator;

    [Header("--- Efecto 'Comido' ---")] [SerializeField]
    private Transform killTarget; // El punto central/boca

    [SerializeField] private float dragSpeed = 12f; // Velocidad de succión
    [SerializeField] private float shrinkSpeed = 2f; // Para que se haga pequeño mientras entra

    [CanBeNull] private Transform Player;

    private bool _isPlayerDead;
    private Vector3 _debugThreatPos;

    [Header("Opciones de Gizmo")] [SerializeField]
    private bool painted;

    private void Start()
    {
        Player = FindObjectOfType<PlayerController>()?.Ctx?.Tf;

        if (warningParticle == null) warningParticle = GetComponentInChildren<ParticleSystem>();
        if (monsterArmAnimator == null) monsterArmAnimator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Player == null) return;

        // Si ya murió, ejecutamos el arrastre visual constantemente
        if (_isPlayerDead)
        {
            ApplyEatingEffect();
            return;
        }

        var centerPos = transform.position;
        var centerPosXZ = new Vector3(centerPos.x, 0, centerPos.z);
        var playerPosXZ = new Vector3(Player.position.x, 0, Player.position.z);

        var distXZ = Vector3.Distance(centerPosXZ, playerPosXZ);
        var dirToPlayer = (playerPosXZ - centerPosXZ).normalized;
        var playerHeight = Player.position.y;

        ManageZoneState(distXZ, playerHeight, dirToPlayer);
    }

    private void ApplyEatingEffect()
    {
        if (killTarget == null) return;

        // 1. Movimiento hacia la 'boca' (killTarget)
        Player.position = Vector3.MoveTowards(Player.position, killTarget.position, dragSpeed * Time.deltaTime);

        // 2. Rotación para dar efecto de descontrol
        Player.Rotate(Vector3.forward, 180f * Time.deltaTime);

        // 3. Efecto extra: Ir reduciendo el tamaño (opcional, da sensación de ser tragado)
        if (Player.localScale.x > 0.1f)
        {
            Player.localScale -= Vector3.one * (shrinkSpeed * Time.deltaTime);
        }

        // 4. Si llega al centro, lo ocultamos finalmente
        if (Vector3.Distance(Player.position, killTarget.position) < 0.2f)
        {
            Player.gameObject.SetActive(false);
        }
    }

    private void ManageZoneState(float dist, float height, Vector3 dir)
    {
        if (height < minYLimit || dist > warningRadius)
        {
            TriggerDeath();
            return;
        }

        if (dist > safeRadius)
        {
            if (warningParticle && !warningParticle.isPlaying) warningParticle.Play();

            float threatDist = (warningRadius + killRadius) / 2f;
            Vector3 targetPos = transform.position + (dir * threatDist);
            targetPos.y = height;

            // Posicionamiento de visuales de amenaza
            if (warningParticle)
            {
                warningParticle.transform.position = targetPos;
                warningParticle.transform.LookAt(Player.position);
            }

            if (monsterArmAnimator)
            {
                monsterArmAnimator.transform.position = targetPos;
                monsterArmAnimator.transform.LookAt(Player.position);
            }

            _debugThreatPos = targetPos;
        }
        else
        {
            if (warningParticle && warningParticle.isPlaying)
                warningParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void TriggerDeath()
    {
        if (_isPlayerDead) return;
        _isPlayerDead = true;

        if (warningParticle) warningParticle.Stop();

        // Disparamos la animación del brazo
        if (monsterArmAnimator)
        {
            monsterArmAnimator.SetTrigger("Kill");
        }

        // Lanzamos el evento de muerte global
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
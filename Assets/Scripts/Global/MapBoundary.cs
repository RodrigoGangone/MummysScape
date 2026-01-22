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

    [Header("Referencias")] [SerializeField]
    private Transform player;

    [SerializeField] private ParticleSystem warningParticle;

    private bool _isPlayerDead;
    private Vector3 _debugThreatPos;

    private void Update()
    {
        if (player == null || _isPlayerDead) return;

        var centerPos = transform.position;
        var centerPosXZ = new Vector3(centerPos.x, 0, centerPos.z);
        var playerPosXZ = new Vector3(player.position.x, 0, player.position.z);

        var distXZ = Vector3.Distance(centerPosXZ, playerPosXZ);
        var dirToPlayer = (playerPosXZ - centerPosXZ).normalized;
        var playerHeight = player.position.y;

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
            warningParticle.transform.LookAt(player.position);

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

    #region Gizmos

    private void OnDrawGizmos()
    {
        // Dibujo base de los anillos (Siempre visible)
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        DrawCircle(safeRadius); // Verde
        Gizmos.color = Color.yellow;
        DrawCircle(warningRadius); // Amarillo
        Gizmos.color = Color.red;
        DrawCircle(killRadius); // Rojo

        // Dibujo del piso mortal
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Vector3 floorCenter = new Vector3(transform.position.x, minYLimit, transform.position.z);
        Gizmos.DrawWireCube(floorCenter, new Vector3(killRadius * 2, 0.1f, killRadius * 2));

        if (player == null) return;

        // Cálculos locales para Gizmos (necesarios en Editor mode)
        Vector3 centerXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerXZ = new Vector3(player.position.x, 0, player.position.z);
        float dist = Vector3.Distance(centerXZ, playerXZ);
        Vector3 dir = (playerXZ - centerXZ).normalized;

        // Lógica de "Cadenas" (Lines)
        if (dist <= safeRadius)
        {
            // Zona Segura -> Línea Verde al Centro
            Gizmos.color = Color.green;
            Gizmos.DrawLine(player.position, transform.position);
        }
        else if (dist <= warningRadius)
        {
            // Límite 1 -> Línea Amarilla al borde Verde
            Gizmos.color = Color.yellow;
            Vector3 anchor = transform.position + (dir * safeRadius);
            anchor.y = player.position.y;
            Gizmos.DrawLine(player.position, anchor);
            Gizmos.DrawWireSphere(anchor, 0.3f);

            // Visualizar amenaza futura
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(player.position, _debugThreatPos);
            Gizmos.DrawWireSphere(_debugThreatPos, 1f);
        }
        else
        {
            // Límite 2 -> Línea Roja al borde Amarillo
            Gizmos.color = Color.red;
            Vector3 anchor = transform.position + (dir * warningRadius);
            anchor.y = player.position.y;
            Gizmos.DrawLine(player.position, anchor);
            Gizmos.DrawWireSphere(anchor, 0.3f);
        }

        // Línea de caída
        Gizmos.color = new Color(0.5f, 0, 0.5f, 0.5f);
        Gizmos.DrawLine(player.position, new Vector3(player.position.x, minYLimit, player.position.z));
    }

    void DrawCircle(float radius)
    {
        // Helper simple para dibujar círculos planos
#if UNITY_EDITOR
        Handles.DrawWireDisc(transform.position, Vector3.up, radius);
#endif
    }

    #endregion
}
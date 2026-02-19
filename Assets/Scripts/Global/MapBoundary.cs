using System;
using System.Collections; // Necesario para la Corrutina
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapBoundary : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    
    [Header("--- Configuración de Zonas ---")] 
    [SerializeField] private float safeRadius = 50f;
    [SerializeField] private float warningRadius = 70f;
    [SerializeField] private float killRadius = 85f;

    [Tooltip("Distancia desde el Kill Radius hacia el CENTRO donde aparecerá la amenaza.")]
    [SerializeField] private float threatOffsetFromKill = 5f;

    [Header("--- Lógica Vertical ---")] 
    [SerializeField] private float minYLimit = -20f;

    [Header("--- Visuales y Brazo ---")] 
    [SerializeField] private ParticleSystem warningParticle;
    [SerializeField] private Animator monsterArmAnimator;

    [Header("--- Efecto 'Comido' (Solo Arrastre) ---")] 
    [SerializeField] private Transform killTarget; 
    [SerializeField] private float dragSpeed = 12f; 

    [CanBeNull] private Transform Player;

    private bool _isPlayerDead;
    private Vector3 _debugThreatPos;

    [Header("Opciones de Gizmo")] 
    [SerializeField] private bool painted = true;

    private void Start()
    {
        var controller = FindObjectOfType<PlayerController>();
        if (controller != null) Player = controller.Ctx.Tf;

        if (warningParticle == null) warningParticle = GetComponentInChildren<ParticleSystem>();
        if (monsterArmAnimator == null) monsterArmAnimator = GetComponentInChildren<Animator>();
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
        if (height < minYLimit || dist > killRadius)
        {
            TriggerDeath();
            return;
        }

        if (dist > safeRadius)
        {
            if (warningParticle && !warningParticle.isPlaying) warningParticle.Play();

            float threatDist = killRadius - threatOffsetFromKill;
            Vector3 targetPos = transform.position + (dir * threatDist);
            targetPos.y = height;

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

    public void TriggerDeath()
    {
        if (_isPlayerDead) return;
        _isPlayerDead = true;

        if (director != null) director.Play();
    }

    public void AnimKill()
    {
        if (warningParticle) warningParticle.Stop();

        if (monsterArmAnimator)
        {
            monsterArmAnimator.SetTrigger("Kill");
        }
    }
    
    public void OnDeathPlayer()
    {
        // Iniciamos la corrutina para el arrastre
        StartCoroutine(EatPlayerRoutine());
        
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.levelEvents.OnDeath.Raise();
    }

    private IEnumerator EatPlayerRoutine()
    {
        if (killTarget == null || Player == null) yield break;

        // Mientras el jugador no haya llegado al centro (con un pequeño margen de error)
        while (Vector3.Distance(Player.position, killTarget.position) > 0.1f)
        {
            // Movimiento fluido hacia la boca
            Player.position = Vector3.MoveTowards(Player.position, killTarget.position, dragSpeed * Time.deltaTime);
            
            // Opcional: Rotación de descontrol (puedes comentarlo si el Timeline ya lo rota)
            Player.Rotate(Vector3.forward, 180f * Time.deltaTime);

            yield return null; // Espera al siguiente frame
        }
    }

    #region Gizmos Visuales
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Handles.color = Color.red;
        Handles.DrawWireDisc(transform.position, Vector3.up, killRadius);

        Handles.color = Color.yellow;
        Handles.DrawWireDisc(transform.position, Vector3.up, warningRadius);

        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, Vector3.up, safeRadius);

        Handles.color = new Color(1f, 0.5f, 0f, 0.8f); 
        Handles.DrawWireDisc(transform.position, Vector3.up, killRadius - threatOffsetFromKill);

        if (painted)
        {
            Handles.color = new Color(1, 0, 0, 0.05f);
            Handles.DrawSolidDisc(transform.position, Vector3.up, killRadius);
            Handles.color = new Color(0, 1, 0, 0.05f);
            Handles.DrawSolidDisc(transform.position, Vector3.up, safeRadius);

            Gizmos.color = new Color(0.5f, 0, 0, 0.2f);
            Vector3 floorCenter = new Vector3(transform.position.x, minYLimit, transform.position.z);
            Gizmos.DrawCube(floorCenter, new Vector3(killRadius * 2, 0.1f, killRadius * 2));
        }

        if (Application.isPlaying && Player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_debugThreatPos, 1f);
            Gizmos.DrawLine(Player.position, _debugThreatPos);
        }
#endif
    }
    #endregion
}
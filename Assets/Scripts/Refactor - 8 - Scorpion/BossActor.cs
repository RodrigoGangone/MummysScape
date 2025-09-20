using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using UnityEngine;

/// <summary>
/// Actor genérico del Jefe. Integra Config por Stages, GOAP, y tu FSM existente.
/// - Implementa IBossContext para desacoplar Skills / GOAP de la clase concreta.
/// - Avanza de Stage cuando colisiona con objetos "Box".
/// - Cuando no quedan Stages, dispara "Die".
/// - Construye WorldModel (distancia, LOS, stage, config) y consulta al GOAP para decidir la intención.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossActor : MonoBehaviour, IBossContext
{
    [Header("Config & Refs")]
    [SerializeField] private BossConfigSO config;
    [SerializeField] private Player player;
    [SerializeField] private Animator animator;

    [Header("FSM (tu implementación)")]
    public StateMachinePlayer stateMachine;

    [Header("Percepción")]
    [SerializeField] private LayerMask losObstacleMask = ~0; // Layers que bloquean la visión

    [Header("Debug")]
    [SerializeField] private bool drawGizmos;
    [SerializeField] private float losRayHeight = 1.5f;

    // IBossContext
    public Transform Transform => transform;
    public Animator Animator => animator;
    public GameObject GameObject => gameObject;
    public Player Player => player;
    public int CurrentStageIndex => _stageIndex;
    public BossConfigSO Config => config;

    // Runtime
    private GoapBrain _goap;
    private int _stageIndex;          // 0..N-1
    private float _time;              // cache Time.time
    private string _lastIntent = "";  // para evitar spam de triggers

    // Eventos opcionales
    public event Action<int> OnStageChanged;
    public event Action OnDeath;
    public event Action OnDamaged;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (stateMachine == null) stateMachine = gameObject.AddComponent<StateMachinePlayer>();

        // Estados mínimos comunes (puedes renombrar a gusto en tu FSM):
        stateMachine.AddState(BossCommonState.Entry, new BS_Entry(this));
        stateMachine.AddState(BossCommonState.Idle, new BS_Idle(this));
        stateMachine.AddState(BossCommonState.Chase, new BS_Chase(this));
        stateMachine.AddState(BossCommonState.UseSkillA, new BS_UseSkillA(this));
        stateMachine.AddState(BossCommonState.UseSkillB, new BS_UseSkillB(this));
        stateMachine.AddState(BossCommonState.Die, new BS_Die(this));

        stateMachine.ChangeState(BossCommonState.Entry);

        _goap = new GoapBrain();
        _stageIndex = 0;
    }

    private void Update()
    {
        _time = Time.time;
        if (player == null || config == null || config.StageCount == 0) return;

        // Decisión GOAP cada frame (con LOS precalculado)
        var wm = BuildWorldModel();
        var intent = _goap.DecideNextIntent(wm, this);

        // Evitar “thrashing”: solo disparar si cambió
        if (intent != _lastIntent)
        {
            _lastIntent = intent;
            TriggerFSM(intent);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Cada “Box” hace daño y avanza de Stage
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            OnDamaged?.Invoke();
            AdvanceStage();
            other.gameObject.SetActive(false);
        }
    }

    private void AdvanceStage()
    {
        _stageIndex++;

        if (_stageIndex >= config.StageCount)
        {
            // Sin más stages: muerte
            _stageIndex = config.StageCount; // clamp
            TriggerFSM("Die");
            OnDeath?.Invoke();
            enabled = false; // apagar Actor
        }
        else
        {
            OnStageChanged?.Invoke(_stageIndex);
            // Puedes disparar un trigger de transición visual aquí si querés
        }
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        var dir = to - from;
        var dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return true;
        dir /= dist;

        return !Physics.Raycast(from + Vector3.up * losRayHeight, dir, dist, losObstacleMask, QueryTriggerInteraction.Ignore);
    }

    public WorldModel BuildWorldModel()
    {
        bool los = HasLineOfSight(transform.position, player.transform.position);
        return new WorldModel(this, los);
    }

    #region Uso de Skills (llamados desde estados)

    public bool TryUseSkillA()
    {
        var wm = BuildWorldModel();
        return config.SkillA != null && config.SkillA.TryExecute(wm, this, _time);
    }

    public bool TryUseSkillB()
    {
        var wm = BuildWorldModel();
        return config.SkillB != null && config.SkillB.TryExecute(wm, this, _time);
    }

    #endregion

    #region IBossContext: TriggerFSM mapping

    /// <summary>
    /// Puente simbólico → tu FSM. Mapea el intent string a tus estados reales.
    /// </summary>
    public void TriggerFSM(string intentOrEvent)
    {
            switch (intentOrEvent)
            {
                case "Entry":  stateMachine.ChangeState(BossCommonState.Entry); break;
                case "Idle":   stateMachine.ChangeState(BossCommonState.Idle); break;
                case "Chase":  stateMachine.ChangeState(BossCommonState.Chase); break;
                case "SkillA": stateMachine.ChangeState(BossCommonState.UseSkillA); break;
                case "SkillB": stateMachine.ChangeState(BossCommonState.UseSkillB); break;
                case "Die":    stateMachine.ChangeState(BossCommonState.Die); break;
                default:       Debug.LogWarning($"[BossActor] Intent desconocido: {intentOrEvent}"); break;
            }     
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || config == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, config.sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, config.attackRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, config.loseSightRange);
    }
}

/// <summary> Estados comunes “placeholder” para usar con tu StateMachine real. </summary>
public enum BossCommonState
{
    Entry,
    Idle,
    Chase,
    UseSkillA,
    UseSkillB,
    Die
}

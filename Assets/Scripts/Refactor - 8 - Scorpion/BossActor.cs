using UnityEngine;
using static BossCommonState;
using System;

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

    [Header("FSM")]
    public StateMachinePlayer stateMachine;

    [Header("Percepción")] [Tooltip("Layers que bloquean la visión")]
    [SerializeField] private LayerMask losObstacleMask;

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

    private BossSkillSO _runtimePrimarySkill;
    private BossSkillSO _runtimeSecondarySkill;
    
    // Eventos opcionales
    public event Action<int> OnStageChanged;
    public event Action OnDeath;
    public event Action OnDamaged;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (stateMachine == null) stateMachine = gameObject.AddComponent<StateMachinePlayer>();

        _runtimePrimarySkill = config.PrimarySkill != null ? Instantiate(config.PrimarySkill) : null;
        _runtimeSecondarySkill = config.SecondarySkill != null ? Instantiate(config.SecondarySkill) : null;
        
        stateMachine.AddState(Entry, new BS_Entry(this));
        stateMachine.AddState(Idle, new BS_Idle(this));
        stateMachine.AddState(Chase, new BS_Chase(this));
        stateMachine.AddState(Primary, new BS_UseSkillA(this));
        stateMachine.AddState(Secondary, new BS_UseSkillB(this));
        stateMachine.AddState(Die, new BS_Die(this));

        stateMachine.ChangeState(Entry);

        _goap = new GoapBrain();
        _stageIndex = 0;
    }

    private void Update()
    {
        _time = Time.time;
        if (player == null || config == null || config.StageCount == 0) return;

        var wm = BuildWorldModel();
        var intent = _goap.DecideNextIntent(wm, this, _runtimePrimarySkill, _runtimeSecondarySkill);

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
            _stageIndex = config.StageCount;
            TriggerFSM("Die");
            OnDeath?.Invoke();
            enabled = false;
        }
        else
        {
            OnStageChanged?.Invoke(_stageIndex);
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

    private WorldModel BuildWorldModel()
    {
        bool los = HasLineOfSight(transform.position, player.transform.position);
        return new WorldModel(this, los);
    }

    #region Uso de Skills (llamados desde estados)

    public bool TryUseSkillA()
    {
        Debug.Log("TryExecuteA");
        var wm = BuildWorldModel();
        return _runtimePrimarySkill != null && _runtimePrimarySkill.TryExecute(wm, this, _time);
    }

    public bool TryUseSkillB()
    {
        Debug.Log("TryExecuteB");
        var wm = BuildWorldModel();
        return _runtimeSecondarySkill != null && _runtimeSecondarySkill.TryExecute(wm, this, _time);
    }


    #endregion


    /// <summary>
    /// Puente simbólico → tu FSM. Mapea el intent string a tus estados reales.
    /// </summary>
    public void TriggerFSM(string intentOrEvent)
    {
        switch (intentOrEvent)
        {
            case "Entry":  stateMachine.ChangeState(Entry); break;
            case "Idle":   stateMachine.ChangeState(Idle); break;
            case "Chase":  stateMachine.ChangeState(Chase); break;
            case "Primary": stateMachine.ChangeState(Primary); break;
            case "Secondary": stateMachine.ChangeState(Secondary); break;
            case "Die":    stateMachine.ChangeState(Die); break;
            default:       Debug.LogWarning($"[BossActor] Intent desconocido: {intentOrEvent}"); break;
        }
        _lastIntent = intentOrEvent; // ← sincroniza el “recuerdo” del planner con la FSM
    }
}

/// <summary> Estados comunes “placeholder” para usar con tu StateMachine real. </summary>
public enum BossCommonState
{
    Entry,
    Idle,
    Chase,
    Primary,
    Secondary,
    Die
}

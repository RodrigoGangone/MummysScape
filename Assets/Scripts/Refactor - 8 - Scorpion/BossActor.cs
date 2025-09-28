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

    [Header("Percepción")] [Tooltip("Layers que bloquean la visión y largo del LoS")]
    [SerializeField] private LayerMask losObstacleMask;
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

    private bool _isEntry;
    public bool IsEntry => _isEntry = true;
    public void NotifyEntryEnded() => _isEntry = false;

    public bool IsExecutingSkill { get; private set; }
    public void NotifySkillStarted() => IsExecutingSkill = true;
    public void NotifySkillEnded() => IsExecutingSkill = false;

    public bool IsDamaged { get; private set; }
    private void NotifyDamaged() => IsDamaged = true;
    public void NotifyRecovery() => IsDamaged = false;

    public bool IsDie { get; private set; }
    private void NotifyDie() => IsDie = true;
    
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
        stateMachine.AddState(Damaged, new BS_Damaged(this));
        stateMachine.AddState(Primary, new BS_UseSkillA(this));
        stateMachine.AddState(Secondary, new BS_UseSkillB(this));
        stateMachine.AddState(Die, new BS_Die(this));

        stateMachine.ChangeState(Idle);
        //stateMachine.ChangeState(Entry); TODO: Descomentar

        _goap = new GoapBrain();
        _stageIndex = 0;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
            OnStageChanged?.Invoke(CurrentStageIndex + 1);
        
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
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            OnDamaged?.Invoke();
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
            OnDeath?.Invoke();
            enabled = false;
        }
        else
            OnStageChanged?.Invoke(_stageIndex);
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
            case "Damaged":  stateMachine.ChangeState(Damaged); break;
            case "Primary": stateMachine.ChangeState(Primary); break;
            case "Secondary": stateMachine.ChangeState(Secondary); break;
            case "Die":    stateMachine.ChangeState(Die); break;
            default:       Debug.LogWarning($"[BossActor] Intent desconocido: {intentOrEvent}"); break;
        }
        _lastIntent = intentOrEvent; // ← sincroniza el “recuerdo” del planner con la FSM
    }

    private void OnEnable()
    {
        OnDamaged += NotifyDamaged;
        OnDamaged += AdvanceStage;

        OnDeath += NotifyDie;
    }

    private void OnDisable()
    {
        OnDamaged -= NotifyDamaged;
        OnDamaged -= AdvanceStage;
        
        OnDeath -= NotifyDie;
    }
}

public enum BossCommonState { Entry, Idle, Chase, Damaged, Primary, Secondary, Die }

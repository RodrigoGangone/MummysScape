using UnityEngine;
using static BossCommonState;
using System;
using System.Collections;
using UnityEngine.Playables;
using static Animations.Boss;

/// <summary>
/// Controlador Central: Integra el sistema de estados (FSM), el planificador de decisiones (GOAP) 
/// y el contexto de batalla, gestionando además la progresión de fases y la secuencia de muerte.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossActor : MonoBehaviour, IPausable, IBossContext
{
    [Header("Config & Refs")] [SerializeField]
    private BossConfigSO config;

    [SerializeField] private PlayerController player;
    [SerializeField] private Animator animator;
    [SerializeField] private FxBank bank;
    //[SerializeField] public FocusOnActivation focus;

    private StateMachinePlayer _stateMachine;

    [Tooltip("Layers que bloquean la visión y largo del LoS")] [Header("Percepción")] [SerializeField]
    private LayerMask losObstacleMask;

    [SerializeField] private float losRayHeight = 1.5f;

    [Header("Cinematic's")]

    //[SerializeField] private PlayableDirector entryTimeLine;
    [SerializeField]
    private PlayableDirector angryTimeLine;

    [SerializeField] private PlayableDirector endedTimeLine;

    //[SerializeField] private ParticleSystem deathImpactFx;
    //[SerializeField] private Transform headSocket;

    public Transform Transform => transform;
    public Animator Animator => animator;
    public FxBank Bank => bank;
    public PlayerContext Player => player.Ctx;
    public int CurrentStageIndex => _stageIndex;
    public BossConfigSO Config => config;

    private GoapBrain _goap;
    private int _stageIndex;
    private float _time;
    private string _lastIntent = "";

    private BossSkillSO _runtimePrimarySkill;
    private BossSkillSO _runtimeSecondarySkill;

    public bool IsEntry { get; private set; } = true;
    public void NotifyEntryEnded() => IsEntry = false;

    public bool IsExecutingSkill { get; private set; }
    public void NotifySkillStarted() => IsExecutingSkill = true;
    public void NotifySkillEnded() => IsExecutingSkill = false;

    //public bool IsAngry { get; private set; }
    //private void NotifyAngry() => IsAngry = true;
    //public void NotifyRecovery() => IsAngry = false;
    public bool IsPreDie { get; private set; }
    public void NotifyPreDie() => IsPreDie = true;
    public bool IsDie { get; private set; }
    private void NotifyDie() => IsDie = true;

    public Func<bool> OnPrimarySkill;
    public Func<bool> OnSecondarySkill;

    private bool _paused;
    private bool _isLocked;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (_stateMachine == null) _stateMachine = gameObject.AddComponent<StateMachinePlayer>();

        _runtimePrimarySkill = config.PrimarySkill != null ? Instantiate(config.PrimarySkill) : null;
        _runtimeSecondarySkill = config.SecondarySkill != null ? Instantiate(config.SecondarySkill) : null;

        _stateMachine.AddState(Entry, new BS_Entry(this));
        _stateMachine.AddState(Idle, new BS_Idle(this));
        _stateMachine.AddState(Chase, new BS_Chase(this));
        _stateMachine.AddState(Angry, new BS_Angry(this));
        _stateMachine.AddState(Primary, new BS_UseSkillA(this));
        _stateMachine.AddState(Secondary, new BS_UseSkillB(this));
        _stateMachine.AddState(PreDie, new BS_Pre_Die(this));
        _stateMachine.AddState(Die, new BS_Die(this));

        _stateMachine.ChangeState(Entry);

        _goap = new GoapBrain();
        _stageIndex = 0;
    }

    private void Update()
    {
        if (_paused || _isLocked) return;

        _time = Time.time;
        if (player == null || config == null || config.StageCount == 0) return;

        var wm = BuildWorldModel();
        var intent = _goap.DecideNextIntent(wm, this, _runtimePrimarySkill, _runtimeSecondarySkill);

        if (intent != _lastIntent)
        {
            _lastIntent = intent;
            TriggerFsm(intent);
        }
    }

    private void AdvanceStage()
    {
        if (IsDie) return;
        StartCoroutine(WaitAndAdvanceStageRoutine());
    }

    private IEnumerator WaitAndAdvanceStageRoutine()
    {
        // 1. Pausamos la ejecución hasta que AE_Skill_Ended devuelva la bandera a false
        yield return new WaitUntil(() => !IsExecutingSkill);

        // 2. Retomamos la lógica de progresión y disparamos el Timeline
        _stageIndex++;

        if (_stageIndex >= config.StageCount)
        {
            _stageIndex = config.StageCount;
        }
        else
        {
            
            Animator.SetBool(PRIMARY_ANIM_SCORPION, false);
            Animator.SetBool(SECONDARY_ANIM_SCORPION, false);
            
            if (angryTimeLine != null) angryTimeLine.Play();
        }
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        var dir = to - from;
        var dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return true;
        dir /= dist;

        return !Physics.Raycast(from + Vector3.up * losRayHeight, dir, dist, losObstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private WorldModel BuildWorldModel()
    {
        bool los = HasLineOfSight(transform.position, player.transform.position);
        return new WorldModel(this, los);
    }

    #region Uso de Skills

    private bool TryUseSkillA()
    {
        var wm = BuildWorldModel();
        return _runtimePrimarySkill != null && _runtimePrimarySkill.TryExecute(wm, this, _time);
    }

    private bool TryUseSkillB()
    {
        var wm = BuildWorldModel();
        return _runtimeSecondarySkill != null && _runtimeSecondarySkill.TryExecute(wm, this, _time);
    }

    #endregion

    private void TriggerFsm(string intentOrEvent)
    {
        switch (intentOrEvent)
        {
            case "Entry":
                _stateMachine.ChangeState(Entry);
                break;
            case "Idle":
                _stateMachine.ChangeState(Idle);
                break;
            case "Chase":
                _stateMachine.ChangeState(Chase);
                break;
            case "Angry":
                _stateMachine.ChangeState(Angry);
                break;
            case "Primary":
                _stateMachine.ChangeState(Primary);
                break;
            case "Secondary":
                _stateMachine.ChangeState(Secondary);
                break;
            case "PreDie":
                _stateMachine.ChangeState(PreDie);
                break;
            case "Die":
                _stateMachine.ChangeState(Die);
                break;
            default:
                Debug.LogWarning($"[BossActor] Intent desconocido: {intentOrEvent}");
                break;
        }

        _lastIntent = intentOrEvent;
    }

    // private void StartDeathSequence() => StartCoroutine(DeathSequenceCo());
    //
    // private IEnumerator DeathSequenceCo()
    // {
    //     NotifyDie();
    //     _isLocked = true;
    //     UpdateControlState();
    //
    //     GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Boss", true);
    //
    //     GameEventManager.Instance.bossEvents.OnDeath.Raise();
    //
    //     yield return new WaitForSecondsRealtime(0.15f);
    //
    //     _stateMachine.ChangeState(Die);
    //
    //     //GameEventManager.Instance.bossEvents.OnStageCompleted.Raise(_stageIndex);
    // }

    private void UpdateControlState()
    {
        if (_paused || _isLocked)
            return;

        bool shouldFreezeByLock = _isLocked && !IsEntry && !IsDie;

        //if (animator != null) animator.enabled = !shouldFreezeByLock;
        if (_stateMachine != null) _stateMachine.enabled = !shouldFreezeByLock;
    }

    private void Death()
    {
        NotifyDie(); // Es mejor usar el método que ya tenías creado para esto
        
        // 1. Bloqueamos al jefe y al jugador durante la cinemática final
        _isLocked = true;
        UpdateControlState();
        //GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Boss", true);

        // 2. Avisamos a la máquina de estados para que lance el trigger en el Animator
        _stateMachine.ChangeState(Die);

        // 3. Reproducimos el Timeline
        if (endedTimeLine != null)
        {
            endedTimeLine.Play(); 
        }
    }

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Register<bool>(OnLockChanged);

        //GameEventManager.Instance.bossEvents.OnAngry.Register(NotifyAngry);
        GameEventManager.Instance.bossEvents.OnAngry.Register(AdvanceStage);

        GameEventManager.Instance.bossEvents.OnDeath.Register(Death);

        //GameEventManager.Instance.bossEvents.OnDeath.Register(StartDeathSequence);

        OnPrimarySkill += TryUseSkillA;
        OnSecondarySkill += TryUseSkillB;
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.playerEvents.OnLocked.Unregister<bool>(OnLockChanged);

        //GameEventManager.Instance.bossEvents.OnAngry.Unregister(NotifyAngry);
        GameEventManager.Instance.bossEvents.OnAngry.Unregister(AdvanceStage);

        GameEventManager.Instance.bossEvents.OnDeath.Unregister(Death);
        //GameEventManager.Instance.bossEvents.OnDeath.Unregister(StartDeathSequence);

        OnPrimarySkill -= TryUseSkillA;
        OnSecondarySkill -= TryUseSkillB;
    }

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;
        animator.enabled = !paused;
        _stateMachine.enabled = !paused;
        
        if (_goap != null) _goap.Paused = paused;
    }

    private void OnLockChanged(bool locked)
    {
        _isLocked = locked;
        //animator.enabled = !locked;
        //_stateMachine.enabled = !locked;
        
        UpdateControlState();
        
        if (_goap != null) _goap.Locked = locked;
    }
}

public enum BossCommonState
{
    Entry,
    Idle,
    Chase,
    Angry,
    Primary,
    Secondary,
    PreDie,
    Die
}
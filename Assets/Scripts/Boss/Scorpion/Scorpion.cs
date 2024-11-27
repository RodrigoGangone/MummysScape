using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Scorpion : Boss
{
    [Header("Scorpion Propertys")] public StateMachinePlayer stateMachine;
    public GameObject viewScorpion;
    public LevelManager LevelManager;

    [Header("First Attack")] internal StoneScorpionTrigger stoneprefab;
    [SerializeField] internal Transform targetShoot;
    internal GameObject stoneView;

    [Header("Second Attack")] private List<Geyser> _allGeysers = new();

    private List<ParticleSystem> _geyserParticlesList = new();
    [SerializeField] private ParticleSystem _geysersParticles;

    [SerializeField] internal List<Geyser> geysersStageOne = new();
    [SerializeField] internal List<Geyser> geysersStageTwo = new();
    [SerializeField] internal List<Geyser> geysersStageThree = new();

    [Header("Stage Propertys")] private Vector3 downPosStage = new(0, -7, 0);
    private StageScorpion _currentStage;

    [SerializeField] private List<Transform> _stages;
    [SerializeField] private List<GameObject> _cameraNodes;
    [SerializeField] private Transform _winPlatform;

    #region Old

    // [SerializeField] internal GameObject viewScorpion;
    // [SerializeField] public LevelManager levelManager;
    //
    // [Header("First Attack")] [SerializeField]
    // internal Transform pointAttackSand;
    //
    // [SerializeField] internal Transform pointAttackPlatform;
    // [SerializeField] private ParticleSystem _firstAttackFX;
    //
    // private const int ATTACK_RADIUS = 50;
    //
    // [Header("Second Attack")] public List<Geyser> geysersStageOne;
    // public List<Geyser> geysersStageTwo;
    // public List<Geyser> geysersStageThree;
    //
    // private List<Geyser> allGeysers = new();
    //
    // [SerializeField] private List<ParticleSystem> _geysersParticlesList = new();
    //
    // [SerializeField] private ParticleSystem _geysersParticles;
    //
    // [Header("Third Attack")] [SerializeField]
    // internal GameObject _stoneView;
    //
    // [SerializeField] internal StoneScorpionTrigger _stonePrefab;
    // [SerializeField] public Transform _targetShoot;
    //
    // [Header("LEVEL CONTROL")] [SerializeField]
    // internal Vector3 _downPosStage; // Vector para bajar stages
    //
    // [SerializeField] private List<Transform> _stages;
    // [SerializeField] private List<GameObject> _cameraNodes;
    // [SerializeField] private StageScorpion _currentStage;
    // [SerializeField] internal Transform _winPlatform;
    //
    // [Header("STATE MACHINE")] public StateMachinePlayer stateMachine;

    #endregion

    void Start()
    {
        stoneprefab = GetComponentInChildren<StoneScorpionTrigger>();

        _allGeysers.AddRange(geysersStageOne);
        _allGeysers.AddRange(geysersStageTwo);
        _allGeysers.AddRange(geysersStageThree);

        for (int i = 0; i < _allGeysers.Count; i++)
        {
            _geyserParticlesList.Add(
                Instantiate(_geysersParticles, transform.position, Quaternion.identity, transform));
        }

        stateMachine = gameObject.AddComponent<StateMachinePlayer>();

        stateMachine.AddState(BossScorpionState.EntryScorpion, new EntryBossScorpion(this));
        stateMachine.AddState(BossScorpionState.IdleScorpion, new IdleBossScorpion(this, player));
        stateMachine.AddState(BossScorpionState.FirstAttackScorpion, new FirstAttackBossScorpion(this, player));
        stateMachine.AddState(BossScorpionState.SecondAttackScorpion, new SecondAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.DeathScorpion, new DeathBossScorpion(this));

        stateMachine.ChangeState(BossScorpionState.IdleScorpion);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            other.gameObject.SetActive(false);
            AdvanceToNextStage();
        }
    }

    private void AdvanceToNextStage()
    {
        int totalStages = _stages.Count;
        int nextStageIndex = (int)_currentStage + 1;

        if (nextStageIndex < totalStages)
        {
            // Actualizar la etapa actual
            _currentStage = (StageScorpion)nextStageIndex;

            Transform currentStageTransform = _stages[nextStageIndex - 1];
            Transform nextStageTransform = nextStageIndex < totalStages ? _stages[nextStageIndex] : null;

            Vector3 targetPosition = currentStageTransform.position + downPosStage;

            // Mover la etapa actual hacia abajo y manejar el estado de las etapas
            StartCoroutine(MoveAndHandleStages(currentStageTransform, nextStageTransform, targetPosition));
        }
        else
        {
            // Cambiar a estado de muerte si no hay más etapas
            stateMachine.ChangeState(BossScorpionState.DeathScorpion);
        }
    }

    private IEnumerator MoveAndHandleStages(Transform currentStage, Transform nextStage, Vector3 targetPosition)
    {
        float speed = 5f;

        // Mover la etapa actual hacia abajo
        while (Vector3.Distance(currentStage.position, targetPosition) > 0.1f)
        {
            currentStage.position = Vector3.MoveTowards(currentStage.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        // Apagar la etapa actual
        currentStage.gameObject.SetActive(false);

        // Apagar el nodo de cámara correspondiente a la etapa actual, si existe
        int currentStageIndex = _stages.IndexOf(currentStage);
        if (currentStageIndex >= 0 && currentStageIndex < _cameraNodes.Count)
        {
            _cameraNodes[currentStageIndex].SetActive(false);
        }

        // Encender la siguiente etapa, si existe
        if (nextStage != null)
        {
            nextStage.gameObject.SetActive(true);

            // Encender el nodo de cámara correspondiente a la siguiente etapa
            int nextStageIndex = _stages.IndexOf(nextStage);
            if (nextStageIndex >= 0 && nextStageIndex < _cameraNodes.Count)
            {
                _cameraNodes[nextStageIndex].SetActive(true);
            }
        }
        else
        {
            // Si no hay más etapas, el jefe muere
            stateMachine.ChangeState(BossScorpionState.DeathScorpion);
        }
    }
    
    #region SecondAttack

    internal void SecondAttack(Action onGeyserCompleted)
    {
        List<Geyser> selectedGeysers = GetCurrentStageGeysers();

        if (selectedGeysers == null || selectedGeysers.Count == 0)
        {
            Debug.LogWarning("No geysers available for the current stage.");
            return;
        }

        StartCoroutine(LineOfSecondAttack(selectedGeysers, onGeyserCompleted));
    }

    public List<Geyser> GetCurrentStageGeysers()
    {
        return _currentStage switch
        {
            StageScorpion.OneStage => geysersStageOne,
            StageScorpion.TwoStage => geysersStageTwo,
            StageScorpion.ThreeStage => geysersStageThree,
            _ => null
        };
    }

    IEnumerator LineOfSecondAttack(List<Geyser> selectedGeysers, Action onGeyserCompleted)
    {
        // Activar partículas para cada géiser seleccionado
        for (int i = 0; i < selectedGeysers.Count; i++)
        {
            if (i >= _geysersParticlesList.Count)
            {
                Debug.LogWarning("Not enough particle systems for geysers.");
                break;
            }

            _geysersParticlesList[i].transform.position = viewScorpion.transform.position;
            _geysersParticlesList[i].Play();
        }

        float speed = 10f;
        bool allReached;

        do
        {
            allReached = true;

            for (int i = 0; i < selectedGeysers.Count; i++)
            {
                if (i >= _geysersParticlesList.Count)
                    continue;

                Vector3 targetPosition = selectedGeysers[i].transform.position;

                if (Vector3.Distance(_geysersParticlesList[i].transform.position, targetPosition) > 0.1f)
                {
                    _geysersParticlesList[i].transform.position = Vector3.MoveTowards(
                        _geysersParticlesList[i].transform.position,
                        targetPosition,
                        speed * Time.deltaTime
                    );

                    allReached = false;
                }
            }

            yield return null;
        } while (!allReached);

        // Desactivar partículas y activar los géiseres
        for (int i = 0; i < selectedGeysers.Count; i++)
        {
            if (i >= _geysersParticlesList.Count)
                continue;

            _geysersParticlesList[i].Stop();
        }

        foreach (var geyser in selectedGeysers)
        {
            geyser.ActivateIntenseMode(onGeyserCompleted);
        }
    }

    #endregion
}

internal enum BossScorpionState
{
    EntryScorpion,
    IdleScorpion,
    FirstAttackScorpion,
    SecondAttackScorpion,
    DeathScorpion
}

internal enum StageScorpion
{
    OneStage,
    TwoStage,
    ThreeStage
}
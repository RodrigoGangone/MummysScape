using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FirstAttackProperties
{
    [SerializeField] public GameObject stone;
    [SerializeField] public Transform targetShoot;
    [SerializeField] public int countStone;
}

[Serializable]
public class SecondAttackProperties
{
    [SerializeField] public List<Geyser> geysersStageOne;
    [SerializeField] public List<Geyser> geysersStageTwo;
    [SerializeField] public List<Geyser> geysersStageThree;

    private List<Geyser> _allGeysers = new(); // Lista privada
    private List<ParticleSystem> _geyserParticlesList = new(); // Lista privada

    //Metodos para manejar las listas de Geysers dentro del script y hacer que sigan siendo privadas
    public void AddToAllGeysers(IEnumerable<Geyser> geysers) => _allGeysers.AddRange(geysers);
    public void AddToAllGeysersParticles(ParticleSystem particle) => _geyserParticlesList.Add(particle);
    public List<Geyser> GetAllGeysers() => _allGeysers;
    public List<ParticleSystem> GetAllGeyserParticles() => _geyserParticlesList;
}

[Serializable]
public class StageProperties
{
    [HideInInspector] public Vector3 downPosStage = new(0, -7, 0);
    [HideInInspector] public StageScorpion currentStage;

    [SerializeField] public List<Transform> stages;
    [SerializeField] public List<GameObject> cameraNodes;
    [SerializeField] public Transform winPlatform;
}

public class Scorpion : Boss
{
    public FirstAttackProperties FirstAttack => _firstAttack;
    public SecondAttackProperties SecondAttack => _secondAttack;

    [SerializeField] private FirstAttackProperties _firstAttack;
    [SerializeField] private SecondAttackProperties _secondAttack;
    [SerializeField] private StageProperties _stageProperties;

    [System.Serializable]
    public struct VFX
    {
        [SerializeField] internal ParticleSystem entryScorpion;
        [SerializeField] internal ParticleSystem preGeyser;
        [SerializeField] internal ParticleSystem preStone;
        [SerializeField] internal ParticleSystem stone;
        [SerializeField] internal ParticleSystem damage;
        [SerializeField] internal ParticleSystem death;
    }

    public VFX Effects => effects;

    [SerializeField] private VFX effects;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();

        FirstAttackBossScorpion.SetAnimator(anim);
        SecondAttackBossScorpion.SetAnimator(anim);

        FirstAttackBossScorpion.SetPositions(player.transform, transform);
        SecondAttackBossScorpion.SetPositions(player.transform, transform);

        stateMachine = gameObject.AddComponent<StateMachinePlayer>();


        InitializedGeysers();

        stateMachine.AddState(ScorpionState.EntryScorpion, new EntryBossScorpion(this));
        stateMachine.AddState(ScorpionState.IdleScorpion, new IdleBossScorpion(this));
        stateMachine.AddState(ScorpionState.FirstAttackScorpion, new FirstAttackBossScorpion(FirstAttack));
        stateMachine.AddState(ScorpionState.SecondAttackScorpion, new SecondAttackBossScorpion(SecondAttack));
        stateMachine.AddState(ScorpionState.DeathScorpion, new DeathBossScorpion(this));

        stateMachine.ChangeState(ScorpionState.EntryScorpion);
    }

    //TODO: CUANDO FUNCIONE TODO, REALIZAR LA CREACION DE PARTICULAS POR POOL FACTORY
    private void InitializedGeysers()
    {
        //Concateno toddas las listas de Geysers en una sola para saber la cantidad de particulas que necesito
        _secondAttack.AddToAllGeysers(_secondAttack.geysersStageOne
            .Concat(_secondAttack.geysersStageTwo)
            .Concat(_secondAttack.geysersStageThree));

        _secondAttack.GetAllGeysers().ForEach(_ =>
            _secondAttack.AddToAllGeysersParticles(Instantiate(effects.preGeyser,
                transform.position,
                Quaternion.identity)));
    }

    #region Stage Methods

    private void AdvanceToNextStage()
    {
        int totalStages = _stageProperties.stages.Count;
        int nextStageIndex = (int)_stageProperties.currentStage + 1;

        if (nextStageIndex < totalStages)
        {
            _stageProperties.currentStage = (StageScorpion)nextStageIndex;
            Transform currentStageTransform = _stageProperties.stages[nextStageIndex - 1];
            Transform nextStageTransform =
                nextStageIndex < totalStages ? _stageProperties.stages[nextStageIndex] : null;
            Vector3 targetPosition = currentStageTransform.position + _stageProperties.downPosStage;

            StartCoroutine(MoveAndHandleStages(currentStageTransform, nextStageTransform, targetPosition));
        }
        else
            stateMachine.ChangeState(ScorpionState.DeathScorpion);
    }

    private IEnumerator MoveAndHandleStages(Transform currentStage, Transform nextStage, Vector3 targetPosition)
    {
        float speed = 5f;

        while (Vector3.Distance(currentStage.position, targetPosition) > 0.1f)
        {
            currentStage.position = Vector3.MoveTowards(currentStage.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        currentStage.gameObject.SetActive(false);

        int currentStageIndex = _stageProperties.stages.IndexOf(currentStage);

        if (currentStageIndex >= 0 && currentStageIndex < _stageProperties.cameraNodes.Count)
            _stageProperties.cameraNodes[currentStageIndex].SetActive(false);

        if (nextStage != null)
        {
            nextStage.gameObject.SetActive(true);

            int nextStageIndex = _stageProperties.stages.IndexOf(nextStage);

            if (nextStageIndex >= 0 && nextStageIndex < _stageProperties.cameraNodes.Count)
                _stageProperties.cameraNodes[nextStageIndex].SetActive(true);
        }
        else
            stateMachine.ChangeState(ScorpionState.DeathScorpion);
    }

    public List<Geyser> GetCurrentStageGeysers()
    {
        return _stageProperties.currentStage switch
        {
            StageScorpion.OneStage => _secondAttack.geysersStageOne,
            StageScorpion.TwoStage => _secondAttack.geysersStageTwo,
            StageScorpion.ThreeStage => _secondAttack.geysersStageThree,
            _ => null
        };
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            other.gameObject.SetActive(false);

            AdvanceToNextStage();
        }
    }

    #endregion
}

public enum ScorpionState
{
    EntryScorpion,
    IdleScorpion,
    FirstAttackScorpion,
    SecondAttackScorpion,
    DeathScorpion
}

public enum StageScorpion
{
    OneStage,
    TwoStage,
    ThreeStage
}
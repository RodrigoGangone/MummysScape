using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Scorpion : Boss
{
    [SerializeField] internal GameObject viewScorpion;
    [SerializeField] public LevelManager levelManager;

    [Header("First Attack")] [SerializeField]
    internal Transform pointAttackSand;

    [SerializeField] internal Transform pointAttackPlatform;
    [SerializeField] private ParticleSystem _firstAttackFX;

    private const int ATTACK_RADIUS = 50;

    [Header("Second Attack")] public List<Geyser> geysersStageOne;
    public List<Geyser> geysersStageTwo;
    public List<Geyser> geysersStageThree;

    private List<Geyser> allGeysers = new();

    [SerializeField] private List<ParticleSystem> _geysersParticlesList = new();

    [SerializeField] private ParticleSystem _geysersParticles;

    [Header("Third Attack")] [SerializeField]
    internal GameObject _stoneView;

    [SerializeField] internal StoneScorpionTrigger _stonePrefab;
    [SerializeField] public Transform _targetShoot;

    [Header("LEVEL CONTROL")] [SerializeField]
    internal Vector3 _downPosStage; // Vector para bajar stages

    [SerializeField] private List<Transform> _stages;
    [SerializeField] private List<GameObject> _cameraNodes;
    [SerializeField] private StageScorpion _currentStage;
    [SerializeField] internal Transform _winPlatform;

    [Header("STATE MACHINE")] public StateMachinePlayer stateMachine;

    void Start()
    {
        allGeysers.AddRange(geysersStageOne);
        //allGeysers.AddRange(geysersStageTwo);
        //allGeysers.AddRange(geysersStageThree);

        for (int i = 0; i < allGeysers.Count; i++)
        {
            _geysersParticlesList.Add(
                Instantiate(_geysersParticles, transform.position, Quaternion.identity, transform));
        }

        stateMachine = gameObject.AddComponent<StateMachinePlayer>();

        stateMachine.AddState(BossScorpionState.EntryScorpion, new EntryBossScorpion(this));
        stateMachine.AddState(BossScorpionState.IdleScorpion, new IdleBossScorpion(this));
        stateMachine.AddState(BossScorpionState.FirstAttackScorpion, new FirstAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.SecondAttackScorpion, new SecondAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.ThirdAttackScorpion, new ThirdAttackBossScorpion(this));
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
        if ((int)_currentStage < Enum.GetValues(typeof(StageScorpion)).Length - 1)
        {
            _currentStage = (StageScorpion)((int)_currentStage + 1);
            HandleStageChange();
        }
        else
            stateMachine.ChangeState(BossScorpionState.DeathScorpion);
    }

    private void HandleStageChange()
    {
        int currentStageIndex = (int)_currentStage - 1; // La etapa actual es el índice anterior
        int nextStageIndex = (int)_currentStage; // La nueva etapa es el índice actual

        // Validar que la etapa actual es válida para apagarse
        if (currentStageIndex >= 0 && currentStageIndex < _stages.Count)
        {
            Transform currentStageTransform = _stages[currentStageIndex];
            Vector3 targetPosition = currentStageTransform.position + _downPosStage;

            // Mover y apagar la etapa actual, y encender la siguiente
            Transform nextStageTransform = nextStageIndex < _stages.Count ? _stages[nextStageIndex] : null;
            StartCoroutine(MoveAndHandleStages(currentStageTransform, nextStageTransform, targetPosition));
        }
        else if (nextStageIndex < _stages.Count)
        {
            // Si no hay etapa actual, encender directamente la siguiente
            _stages[nextStageIndex].gameObject.SetActive(true);
        }
        else
        {
            // Si no hay más etapas, el jefe muere
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


    #region FirstAttack

    internal void FirstAreaAttack(Transform originRaycast)
    {
        _firstAttackFX.Play();

        LayerMask playerLayerMask = LayerMask.GetMask("Player");
        LayerMask wallLayerMask = LayerMask.GetMask("Wall");
        LayerMask boxLayerMask = LayerMask.GetMask("Box");
        LayerMask wallAndBoxLayerMask = wallLayerMask | boxLayerMask;

        Vector3 rayOriginPlatform = originRaycast.position;
        Vector3 targetPosition =
            new Vector3(player.transform.position.x, rayOriginPlatform.y, player.transform.position.z);
        Vector3 direction = (targetPosition - rayOriginPlatform).normalized;

        if (Physics.Raycast(rayOriginPlatform, direction, out RaycastHit wallHit, ATTACK_RADIUS, wallAndBoxLayerMask))
        {
            Debug.DrawRay(rayOriginPlatform, direction * wallHit.distance, Color.red, 1f);
        }
        else if (Physics.Raycast(rayOriginPlatform, direction, out RaycastHit playerHit, ATTACK_RADIUS,
                     playerLayerMask))
        {
            Debug.DrawRay(rayOriginPlatform, direction * playerHit.distance, Color.green, 1f);
            player._modelPlayer.IsDamage = true;

            StartCoroutine(MovePlayer(direction));
        }

        stateMachine.ChangeState(BossScorpionState.IdleScorpion);
    }

    public IEnumerator MovePlayer(Vector3 dir)
    {
        float speed = 8f;
        Vector3 normalizedDir = dir.normalized;
        float rayLength = 1f; // Longitud del rayo para detectar la capa Sand

        while (player._modelPlayer.CheckGround() && !IsOnSand(rayLength))
        {
            player.transform.position += normalizedDir * (speed * Time.deltaTime);
            yield return null;
        }

        player.GetComponent<Rigidbody>().AddForce(normalizedDir * (IsOnSand(rayLength) ? 10f : 5f), ForceMode.Impulse);

        yield return new WaitForSeconds(0.5f);

        player._modelPlayer.IsDamage = false;
    }

    private bool IsOnSand(float rayLength)
    {
        Vector3 rayOrigin = player.transform.position + Vector3.up * 0.1f; // Origen del rayo
        Ray ray = new Ray(rayOrigin, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayLength))
        {
            // Comprueba si la capa del objeto detectado es "Sand"
            return hit.collider.gameObject.layer == LayerMask.NameToLayer("Sand");
        }

        return false;
    }

    #endregion

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

        float speed = 5f;
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
    ThirdAttackScorpion,
    DeathScorpion
}

internal enum StageScorpion
{
    OneStage,
    TwoStage,
    ThreeStage
}
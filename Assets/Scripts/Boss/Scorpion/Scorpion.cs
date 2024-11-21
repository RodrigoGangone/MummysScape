using System;
using System.Collections;
using System.Collections.Generic;
using static Utils;
using UnityEngine;
using UnityEngine.Serialization;

public class Scorpion : Boss
{
    [SerializeField] internal GameObject viewScorpion;

    [Header("First Attack")] [SerializeField]
    internal Transform pointAttackSand;

    [SerializeField] internal Transform pointAttackPlatform;
    [SerializeField] private ParticleSystem _firstAttackFX;

    private const int ATTACK_RADIUS = 50;

    [Header("Second Attack")] public List<Geyser> geysers;

    [SerializeField] private List<ParticleSystem> _geysersParticlesList = new();
    [SerializeField] private ParticleSystem _geysersParticles;

    [Header("Third Attack")] [SerializeField]
    internal GameObject _stoneView;

    [SerializeField] internal StoneScorpionTrigger _stonePrefab;
    [SerializeField] public Transform _targetShoot;

    [Header("STATE MACHINE")] public StateMachinePlayer stateMachine;

    void Start()
    {
        for (int i = 0; i < geysers.Count; i++)
        {
            _geysersParticlesList.Add(
                Instantiate(_geysersParticles, transform.position, Quaternion.identity, transform));
            _geysersParticlesList[i].gameObject.SetActive(false);
        }

        stateMachine = gameObject.AddComponent<StateMachinePlayer>();

        stateMachine.AddState(BossScorpionState.EntryScorpion, new EntryBossScorpion(this));
        stateMachine.AddState(BossScorpionState.IdleScorpion, new IdleBossScorpion(this));
        stateMachine.AddState(BossScorpionState.FirstAttackScorpion, new FirstAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.SecondAttackScorpion, new SecondAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.ThirdAttackScorpion, new ThirdAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.DeathScorpion, new DeathBossScorpion(this));

        //stateMachine.ChangeState(BossScorpionState.EntryScorpion);
        stateMachine.ChangeState(BossScorpionState.IdleScorpion);
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
            Debug.DrawRay(rayOriginPlatform, direction * wallHit.distance, Color.red, 1f);

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

        while (true)
        {
            player.transform.position += normalizedDir * (speed * Time.deltaTime);

            if (!player._modelPlayer.CheckGround() && !player.WalkingSand)
            {
                player.GetComponent<Rigidbody>().AddForce(normalizedDir * 5f, ForceMode.Impulse);

                yield return new WaitForSeconds(0.5f);

                player._modelPlayer.IsDamage = false;

                yield break;
            }

            if (player.WalkingSand)
                player.GetComponent<Rigidbody>().AddForce(normalizedDir * 0.01f, ForceMode.Impulse);

            yield return null;
        }
    }

    #endregion

    #region SecondAttack

    internal void SecondAttack(Action onGeyserCompleted)
    {
        StartCoroutine(LineOfSecondAttack(onGeyserCompleted));
    }

    IEnumerator LineOfSecondAttack(Action onGeyserCompleted)
    {
        for (int i = 0; i < _geysersParticlesList.Count; i++)
        {
            _geysersParticlesList[i].transform.position = viewScorpion.transform.position;
            _geysersParticlesList[i].gameObject.SetActive(true);
        }

        float speed = 20f;
        bool allReached;

        do
        {
            allReached = true;

            for (int i = 0; i < _geysersParticlesList.Count; i++)
            {
                Vector3 targetPosition = geysers[i].transform.position;

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

        foreach (var geyserParticle in _geysersParticlesList)
        {
            geyserParticle.gameObject.SetActive(false);
        }

        foreach (var geyser in geysers)
        {
            geyser.ActivateIntenseMode(onGeyserCompleted);
        }
    }

    #endregion

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
            stateMachine.ChangeState(BossScorpionState.DeathScorpion);
    }
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
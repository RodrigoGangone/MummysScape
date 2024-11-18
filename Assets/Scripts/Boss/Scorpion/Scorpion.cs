using System;
using System.Collections;
using System.Collections.Generic;
using static Utils;
using UnityEngine;

public class Scorpion : Boss
{
    [SerializeField] public GameObject _viewScorpion;

    [Header("Area Attack")] //First Attack
    private int _numberOfRays = 25;

    private int _attackRadius = 50;

    [SerializeField] internal Transform _pointAttackSand;
    [SerializeField] internal Transform _pointAttackPlatform;

    [SerializeField] private ParticleSystem _firstAttackFX;

    [Header("Geysers")] //Second Attack
    [SerializeField]
    internal List<Geyser> _geysers;

    [SerializeField] private ParticleSystem _geysersParticles;
    private List<ParticleSystem> _geysersParticlesList = new();

    public StateMachinePlayer stateMachine;

    void Start()
    {
        for (int i = 0; i < _geysers.Count; i++)
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

        if (Physics.Raycast(rayOriginPlatform, direction, out RaycastHit wallHit, _attackRadius, wallAndBoxLayerMask))
            Debug.DrawRay(rayOriginPlatform, direction * wallHit.distance, Color.red, 1f);

        else if (Physics.Raycast(rayOriginPlatform, direction, out RaycastHit playerHit, _attackRadius,
                     playerLayerMask))
        {
            Debug.DrawRay(rayOriginPlatform, direction * playerHit.distance, Color.green, 1f);

            player._modelPlayer.IsDamage = true;

            StartCoroutine(MovePlayer(direction));
        }

        stateMachine.ChangeState(BossScorpionState.IdleScorpion);
    }

    IEnumerator MovePlayer(Vector3 dir)
    {
        float speed = 8f;

        Vector3 normalizedDir = dir.normalized;

        while (true)
        {
            player.transform.position += normalizedDir * (speed * Time.deltaTime);

            if (!player._modelPlayer.CheckGround())
            {
                player.GetComponent<Rigidbody>().AddForce(normalizedDir * 5f, ForceMode.Impulse);

                yield return new WaitForSeconds(0.5f);

                player._modelPlayer.IsDamage = false;

                yield break;
            }

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
            _geysersParticlesList[i].transform.position = _viewScorpion.transform.position;
            _geysersParticlesList[i].gameObject.SetActive(true);
        }

        float speed = 20f;
        bool allReached;

        do
        {
            allReached = true;

            for (int i = 0; i < _geysersParticlesList.Count; i++)
            {
                Vector3 targetPosition = _geysers[i].transform.position;

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

        foreach (var geyser in _geysers)
        {
            geyser.ActivateIntenseMode(onGeyserCompleted);
        }
    }

    #endregion

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MovableBox"))
            _isDead = true;
    }
}

enum BossScorpionState
{
    EntryScorpion,
    IdleScorpion,
    FirstAttackScorpion,
    SecondAttackScorpion,
    DeathScorpion
}
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Utils;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

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
    private Geyser _geyser;

    [SerializeField] private List<Transform> _geyserPositions;

    private Vector3 _boxSize = new(5f, 10f, 5f); // Ancho, alto, profundidad


    private Vector3 _normalGeyserScale;
    private Vector3 _bigGeyserScale = new(1.5f, 1.5f, 1.5f);

    public StateMachinePlayer stateMachine;

    void Start()
    {
        _normalGeyserScale = _geyser.transform.localScale;

        stateMachine = gameObject.AddComponent<StateMachinePlayer>();

        stateMachine.AddState(BossScorpionState.EntryScorpion, new EntryBossScorpion(this));
        stateMachine.AddState(BossScorpionState.IdleScorpion, new IdleBossScorpion(this));
        stateMachine.AddState(BossScorpionState.FirstAttackScorpion, new FirstAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.SecondAttackScorpion, new SecondAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.DeathScorpion, new DeathBossScorpion(this));

        //stateMachine.ChangeState(BossScorpionState.EntryScorpion);
        stateMachine.ChangeState(BossScorpionState.IdleScorpion);
    }

    internal void FirstAreaAttack(Transform originRaycast)
    {
        float coneAngle = 45f;
        float angleStep = coneAngle / (_numberOfRays - 1);

        _firstAttackFX.Play();

        LayerMask playerLayerMask = LayerMask.GetMask("Player");
        LayerMask wallLayerMask = LayerMask.GetMask("Wall");

        Vector3 rayOriginPlatform = originRaycast.position;

        for (int i = 0; i < _numberOfRays; i++)
        {
            float angle = -coneAngle / 2 + i * angleStep;

            Vector3 forward = new Vector3(_viewScorpion.transform.forward.x, 0, _viewScorpion.transform.forward.z)
                .normalized;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;

            if (Physics.Raycast(rayOriginPlatform, direction, out RaycastHit wallHit, _attackRadius, wallLayerMask))
                Debug.DrawRay(rayOriginPlatform, direction * wallHit.distance, Color.red, 1f);
            else
            {
                if (Physics.Raycast(rayOriginPlatform, direction, out RaycastHit playerHit, _attackRadius,
                        playerLayerMask))
                {
                    Debug.DrawRay(rayOriginPlatform, direction * playerHit.distance, Color.green, 1f);

                    player._modelPlayer.IsDamage = true;

                    StartCoroutine(MovePlayer(direction));
                }
                else
                    Debug.DrawRay(rayOriginPlatform, direction * _attackRadius, Color.yellow, 1f);
            }
        }

        stateMachine.ChangeState(BossScorpionState.IdleScorpion);
    }

    internal void SecondAttack()
    {
        LayerMask playerLayerMask = LayerMask.GetMask("Player");

        foreach (var geyserPosition in _geyserPositions)
        {
            Collider[] hitColl = Physics.OverlapBox(geyserPosition.position,
                _boxSize / 2,
                quaternion.identity,
                playerLayerMask);

            foreach (Collider hit in hitColl)
            {
                if (hit.CompareTag(PLAYER_TAG))
                {
                    float playerHeight = hit.transform.position.y - geyserPosition.position.y;

                    ActivateGeyser(geyserPosition.position, playerHeight > 5 ? _bigGeyserScale : _normalGeyserScale);
                }
            }
        }
    }

    private void ActivateGeyser(Vector3 position, Vector3 scale)
    {
        // Reposicionar y escalar el geyser
        _geyser.transform.position = position;
        _geyser.transform.localScale = scale;
    }


    IEnumerator MovePlayer(Vector3 dir)
    {
        float speed = 8f;

        Vector3 normalizedDir = dir.normalized;

        while (true)
        {
            player.transform.position += normalizedDir * speed * Time.deltaTime;

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


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MovableBox"))
        {
            _isDead = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (_geyserPositions == null || _geyserPositions.Count == 0) return;

        Gizmos.color = Color.cyan;

        foreach (var geyserPosition in _geyserPositions)
        {
            if (geyserPosition != null)
            {
                // Dibujar el área de detección usando directamente la posición y el tamaño de la caja
                Gizmos.DrawWireCube(geyserPosition.position, _boxSize);
            }
        }
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
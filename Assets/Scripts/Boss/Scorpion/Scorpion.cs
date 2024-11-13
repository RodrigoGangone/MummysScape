using System;
using System.Collections;
using System.Collections.Generic;
using static Utils;
using UnityEngine;
using UnityEngine.Serialization;

public class Scorpion : Boss
{
    [Header("Area Attack")] //First Attack //Usar LineOfSight 
    private GameObject _areaGameObject;

    [Header("Geysers")] //Second Attack
    private GameObject _geyserGameObject;

    private List<Transform> _geysersPositions;

    [Header("STATES")] //GOAP State
    internal StateMachinePlayer stateMachine;

    private void Start()
    {
        stateMachine = new StateMachinePlayer();

        stateMachine.AddState(BossScorpionState.EntryScorpion, new EntryBossScorpion(this));
        stateMachine.AddState(BossScorpionState.IdleScorpion, new IdleBossScorpion(this));
        stateMachine.AddState(BossScorpionState.FirstAttackScorpion, new FirstAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.SecondAttackScorpion, new SecondAttackBossScorpion(this));
        stateMachine.AddState(BossScorpionState.DeathScorpion, new DeathBossScorpion(this));

        stateMachine.ChangeState(BossScorpionState.EntryScorpion);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MovableBox"))
        {
            _isDead = true;
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
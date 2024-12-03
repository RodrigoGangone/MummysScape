using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("BOSS PROPERTY'S")] public Player player;
    public LevelManager levelManager;

    [HideInInspector] public StateMachinePlayer stateMachine;

    [HideInInspector] public CurrentAttack currentAttack;

    [HideInInspector] public float coolDownFirst = 3f;
    [HideInInspector] public float coolDownSecond = 3f;

    [HideInInspector] public Animator anim;
}

public enum CurrentAttack
{
    First,
    Second
}
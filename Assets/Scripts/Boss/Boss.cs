using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("BOSS PROPERTY'S")] public Player player;
    public LevelManager levelManager;

    [HideInInspector] public StateMachinePlayer stateMachine;

    [HideInInspector] public CurrentAttack currentAttack;

     public float coolDownFirst = 10f;
     public float coolDownSecond = 10f;

    [HideInInspector] public Animator anim;
}

public enum CurrentAttack
{
    First,
    Second
}
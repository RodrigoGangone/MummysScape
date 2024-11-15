using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Boss : MonoBehaviour
{
    [Header("BOSS PROPERTY'S")] //Propiedades que comparten todos los Boss 
    internal bool _isDead; //1 Golpe

    [SerializeField] internal CurrentAttack _currentAttack;

    [SerializeField] internal float _angerLevel;

    [SerializeField] internal Animator _anim;

    [SerializeField] internal float _cdAttack1;
    [SerializeField] internal float _cdAttack2;

    [SerializeField] public Player player;
}

public enum CurrentAttack
{
    First,
    Second
}
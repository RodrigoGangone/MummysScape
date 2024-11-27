using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("BOSS PROPERTY'S")] 
    
    public Player player;
    
    public CurrentAttack currentAttack;
    public Animator anim;
    
    public float coolDownFirst;
    public float coolDownSecond;
}

public enum CurrentAttack
{
    First,
    Second
}
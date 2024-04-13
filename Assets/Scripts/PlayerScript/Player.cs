using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    
    ModelPlayer _modelPlayer;
    ViewPlayer _viewPlayer;
    ControllerPlayer _controllerPlayer;
    
    public StateMachinePlayer _StateMachinePlayer { get; private set; }
    private string _currentState;
    
    private float _life;
    private float _speed;
    private float _stockBandages;
    private float _speedRotation ;
    
    public float Life { get => _life; set => _life = value; }
    public float Speed { get => _speed; set { } }
    public float SpeedRotation { get => _speedRotation; set => _speedRotation = value; }

    private void Start()
    {
        _modelPlayer = new ModelPlayer(this);
        _controllerPlayer = new ControllerPlayer(_modelPlayer);
        _StateMachinePlayer = new StateMachinePlayer();
    }
    
    
    

    public enum PlayerState
    {
        Idle,
        Shoot,
        Walk,
        Hook,
        Grab,
        Damage,
        Dead
        
    }
}

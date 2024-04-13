using System;
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
        
        _StateMachinePlayer.AddState(PlayerState.Idle, new SM_Idle());
        _StateMachinePlayer.AddState(PlayerState.Shoot, new SM_Shoot());
        _StateMachinePlayer.AddState(PlayerState.Walk, new SM_Walk());
        _StateMachinePlayer.AddState(PlayerState.Hook, new SM_Hook());
        _StateMachinePlayer.AddState(PlayerState.Grab, new SM_Grab());
        _StateMachinePlayer.AddState(PlayerState.Damage, new SM_Damage());
        _StateMachinePlayer.AddState(PlayerState.Dead, new SM_Dead());
    }

    
    private void Update()
    {
        _StateMachinePlayer.Update();
    }

    private void FixedUpdate()
    {
        _StateMachinePlayer.FixedUpdate();
    }

    #region StateMachineMetods
    
        void ChangeState(PlayerState playerState)
        {
            _StateMachinePlayer.ChangeState(playerState);
        }
    
        string CurrentState()
        {
            return _StateMachinePlayer.getCurrentState();
        }


        private enum PlayerState
        {
            Idle,
            Shoot,
            Walk,
            Hook,
            Grab,
            Damage,
            Dead
        }    
    
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    ModelPlayer _modelPlayer;
    ViewPlayer _viewPlayer;
    ControllerPlayer _controllerPlayer;
    StateMachinePlayer _machinePlayer;

    private float _life;
    private float _speed;
    private float _stockBandages;



    public float Life { get => _life; set => _life = value; }
    public float Speed { get => _speed; set { } }

    private void Start()
    {
        _modelPlayer = new ModelPlayer();
        _controllerPlayer = new ControllerPlayer();
        _machinePlayer = new StateMachinePlayer();
        
    }
}

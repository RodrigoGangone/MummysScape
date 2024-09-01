using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Grab : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;
    
    private Transform _grabbedObject;
    private Rigidbody _grabbedObjectRb;
    
    public SM_Grab(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;
    }

    public override void OnEnter()
    {
        
        // Obtengo el objeto a empujar/tirar
        _grabbedObject = _player.GrabbedObject;
        _grabbedObjectRb = _grabbedObject.GetComponent<Rigidbody>();
        
        FreezeIncorrectAxis();
    }
    
    public override void OnExit()
    {
        // Desfreezar ambos ejes cuando se suelta la caja
        _grabbedObjectRb.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
    }

    public override void OnUpdate()
    {
        // No necesitas implementar nada en OnUpdate
    }

    public override void OnFixedUpdate()
    {
        HandleGrabMovement();
    }

    private void HandleGrabMovement()
    {
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        // Mueve al jugador y a la caja en la misma dirección
        _model.Move(moveHorizontal, moveVertical);

        // Calcula la dirección de movimiento en el eje permitido
        Vector3 movement = Vector3.zero;

        if (_grabbedObjectRb.constraints == RigidbodyConstraints.FreezePositionX)
        {
            movement = new Vector3(0, 0, moveVertical) * (_player.Speed * Time.deltaTime);
        }
        else if (_grabbedObjectRb.constraints == RigidbodyConstraints.FreezePositionZ)
        {
            movement = new Vector3(moveHorizontal, 0, 0) * (_player.Speed * Time.deltaTime);
        }

        // Mover la caja
        _grabbedObjectRb.MovePosition(_grabbedObject.position + movement);
    }

    private void FreezeIncorrectAxis()
    {
        Vector3 directionToPlayer = (_player.transform.position - _grabbedObject.position).normalized;

        if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.z))
        {
            _grabbedObjectRb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            _grabbedObjectRb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotation;
        }
    }
}

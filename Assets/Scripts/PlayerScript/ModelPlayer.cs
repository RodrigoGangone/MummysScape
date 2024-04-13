using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class ModelPlayer
{
    Player _player;
    Rigidbody _rb;


    public ModelPlayer(Player p) { _player = p; _rb = _player.GetComponent<Rigidbody>(); }

    public void MoveTank (float rotationInput, float moveInput)
    {
        Quaternion _rotation = Quaternion.Euler(0f, rotationInput * _player.SpeedRotation * Time.deltaTime, 0f);
        _rb.rotation = (_rb.rotation * _rotation);

        Vector3 movemente = _player.transform.forward * moveInput * _player.Speed*Time.deltaTime;
        _rb.MovePosition(_rb.position + movemente);
    }
    
    public void MoveVariant (float movimientoHorizontal, float movimientoVertical)
    {
        Vector3 movimiento = new Vector3(movimientoHorizontal, 0f, movimientoVertical) * _player.Speed;

        if (movimiento != Vector3.zero)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(movimiento);
            _player.transform.rotation = Quaternion.RotateTowards(_player.transform.rotation, rotacionDeseada, _player.SpeedRotation * Time.deltaTime);
        }

        _rb.velocity = new Vector3(movimiento.x, _rb.velocity.y, movimiento.z);
    }
}

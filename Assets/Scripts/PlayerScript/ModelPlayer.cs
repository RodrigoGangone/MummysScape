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

    public void MoveVariant(float movimientoHorizontal, float movimientoVertical)
    {
        Vector3 forward = ( new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z));
        
        Vector3 right = Quaternion.Euler( new Vector3 (0, 90, 0) ) * forward;

        Vector3 righMovement = right * (_player.Speed * Time.deltaTime * movimientoHorizontal);
        Vector3 upMovement = forward * (_player.Speed * Time.deltaTime * movimientoVertical);

        Vector3 heading = Vector3.Normalize(righMovement + upMovement);

        Quaternion targetRotation = Quaternion.LookRotation(heading, Vector3.up);
        
        _player.transform.rotation = Quaternion.Lerp(_player.transform.rotation, targetRotation, Time.deltaTime * _player.SpeedRotation);
        
        _player.transform.position += righMovement;
        _player.transform.position += upMovement;
        
    }
}

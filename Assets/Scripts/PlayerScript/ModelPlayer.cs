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
    
    public void MoveCameraTransform (float movimientoHorizontal, float movimientoVertical, Transform camaraTransform )
    {
        Vector3 movimiento = new Vector3(movimientoHorizontal, 0f, movimientoVertical);

        // Rotar hacia la dirección de movimiento
        if (movimiento.magnitude > 0.1f) // Solo rotar si hay movimiento significativo
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(movimiento, Vector3.up);
            _player.transform.rotation = Quaternion.RotateTowards(_player.transform.rotation, rotacionDeseada, _player.SpeedRotation * Time.deltaTime);
        }
        
        // Rotar hacia la dirección de la cámara en el plano XZ
        Vector3 direccionMovimiento = camaraTransform.forward;
        direccionMovimiento.y = 0f; // Mantener la dirección en el plano horizontal

        // Aplicar el movimiento al Rigidbody en la dirección del input de movimiento relativo a la orientación de la cámara
        Vector3 movimientoRelativoCamara = Quaternion.Euler(0f, camaraTransform.eulerAngles.y, 0f) * movimiento.normalized;
        _rb.velocity = movimientoRelativoCamara * _player.Speed;
    }
    
    
}

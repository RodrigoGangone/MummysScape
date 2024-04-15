using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class ModelPlayer
{
    Player _player;
    Rigidbody _rb;
    Vector3 _currentHook;
    Vector3 _objectToHook = Vector3.zero;
    private LineRenderer _bandage;
    public SpringJoint _joint;
    public bool _isHook = false;
    public ModelPlayer(Player p, SpringJoint springJoint)
    {
        _player = p; 
        _rb = _player.GetComponent<Rigidbody>();
        _bandage = _player.GetComponent<LineRenderer>();
        _joint = springJoint;
    }

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

    public void Hook()
    {
        var minDistanceHook = 5;
        bool objectToHookUpdated = false;

        Collider[] _hooks = Physics.OverlapSphere(_player.transform.position, minDistanceHook, LayerMask.GetMask("Hookeable"));

        if (_hooks.Length > 0)
        {
            foreach (var graps in _hooks)
            {
                var distance = Vector3.Distance(_player.transform.position, graps.transform.position);

                if (distance < minDistanceHook) 
                { 
                    _objectToHook = graps.transform.position;
                    objectToHookUpdated = true;
                }
            }
            
            _isHook = true;
        }
        

        if (objectToHookUpdated)
        {
            _joint.connectedAnchor = _objectToHook;

            float distanceFromHook = Vector3.Distance(_player.transform.position, _objectToHook);

            _joint.maxDistance = distanceFromHook * 0.8f;
            _joint.minDistance = distanceFromHook * 0.25f;

            _joint.spring = 7f;
            _joint.damper = 12f;
            _joint.massScale = 7f;
        }
    }
    
    public void ResetHook()
    {
        _bandage.enabled = false;
        
        _isHook = false;
        
        _joint.connectedAnchor = Vector3.zero;
        _joint.maxDistance = 0f;
        _joint.minDistance = 0f;
        _joint.spring = 0f;
        _joint.damper = 0f;
        _joint.massScale = 0f;
    
        _objectToHook = Vector3.zero;
    }
    
    public void DrawHook() 
    {            
        _bandage.enabled = true;
        
        _bandage.SetPosition(0, _player.transform.position);
        _bandage.SetPosition(1, _objectToHook); 
    }
    
}

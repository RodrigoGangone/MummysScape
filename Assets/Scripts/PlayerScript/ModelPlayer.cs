using System;
using UnityEngine;


public class ModelPlayer
{
    Player _player;
    Rigidbody _rb;
    
    Vector3 _objectToHook = Vector3.zero;
    public LineRenderer _bandage;
    public SpringJoint _joint;
    public bool objectToHookUpdated = false;

    public Action reset;
    public Action lineCurrent;

    public ModelPlayer(Player p, SpringJoint springJoint)
    {
        _player = p; 
        _rb = _player.GetComponent<Rigidbody>();
        _bandage = _player.GetComponent<LineRenderer>();
        _joint = springJoint;

        reset = () => { UnityEngine.Object.Destroy(_joint); _bandage.enabled = false; objectToHookUpdated = false; };
        lineCurrent = () => { _bandage.enabled = true; _bandage.positionCount = 2; _bandage.SetPosition(0, _player.transform.position); _bandage.SetPosition(1, _objectToHook); };
    }

    public void MoveTank (float rotationInput, float moveInput)
    {
        Quaternion rotation = Quaternion.Euler(0f, rotationInput * _player.SpeedRotation * Time.deltaTime, 0f);
        _rb.rotation = (_rb.rotation * rotation);

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
        
        if (_rb.velocity.magnitude > _player.Speed) { _rb.velocity = _rb.velocity.normalized * _player.Speed; }
    }

    public void Hook()
    {
        var minDistanceHook = 5;
        
        if(_joint == null) { _joint = _player.gameObject.AddComponent<SpringJoint>(); _joint.autoConfigureConnectedAnchor = false; }
        
        Collider[] hooks = Physics.OverlapSphere(_player.transform.position, minDistanceHook, LayerMask.GetMask("Hookeable"));

        if (hooks.Length > 0 && !objectToHookUpdated)
        {
            foreach (var grapes in hooks)
            {
                var distance = Vector3.Distance(_player.transform.position, grapes.transform.position);
                if (distance < minDistanceHook) { _objectToHook = grapes.transform.position; objectToHookUpdated = true; }
            }
        }
        
        if (objectToHookUpdated)
        {
            _joint.connectedAnchor = _objectToHook;
            
            _joint.maxDistance = 2f;
            _joint.minDistance = 0.1f;
            
            _joint.spring = 100;
            _joint.damper = 100f;
            _joint.breakTorque = 1;
            _joint.massScale = 100f;
        }
    }
    
    public void DrawHook()
    {
        int quality = 50;

        //if (!_isHook) { reset?.Invoke(); _bandage.positionCount = 0; return; }
        if (_bandage.positionCount == 0){ _bandage.positionCount = quality + 1; }
        if (_bandage.positionCount < quality + 1) { _bandage.positionCount = quality + 1; }
    
        var grapplePoint = _objectToHook;
        var gunTipPosition = _player.transform.position;
        var up = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.up;
        float distance = Vector3.Distance(gunTipPosition, grapplePoint);

        for (var i = 0; i < quality + 1; i++)
        {
            var delta = i / (float)quality;
            Vector3 ropePosition = Vector3.Lerp(gunTipPosition, grapplePoint, delta);
            float distanceFromGunTip = Vector3.Distance(gunTipPosition, ropePosition);
            float percentage = distanceFromGunTip / distance;
            //Vector3 offset = up * Mathf.Sin(percentage * 3 * Mathf.PI) * 5f * _affectcurve.Evaluate(delta);

            //_bandage.SetPosition(i, ropePosition + offset);
        }
        
        _bandage.enabled = false;
        //_drawLineHook = false;
    }

}

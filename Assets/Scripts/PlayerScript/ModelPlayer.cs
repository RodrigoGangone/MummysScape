using System;
using UnityEngine;
using Object = UnityEngine.Object;


public class ModelPlayer
{
    Player _player;
    ViewPlayer _view;
    Rigidbody _rb;
    
    //HOOK
    Vector3 _objectToHook = Vector3.zero;
    public LineRenderer _bandage;
    public SpringJoint _joint;
    public bool objectToHookUpdated = false;

    public Action reset;
    public Action lineCurrent;
    public Action limitVelocity;
    public Action <Vector3> AimGizmo;
    //HOOK
    
    //AIM SHOOT
    public Vector3 posShoot;
    //AIM SHOOT
    public ModelPlayer(Player p, SpringJoint springJoint, ViewPlayer v)
    {
        _player = p;
        _view = v;
        _rb = _player.GetComponent<Rigidbody>();
        _bandage = _player.GetComponent<LineRenderer>();
        _joint = springJoint;

        reset = () => { UnityEngine.Object.Destroy(_joint); _bandage.enabled = false; objectToHookUpdated = false; };
        lineCurrent = () => { _bandage.enabled = true; _bandage.SetPosition(0, _player.transform.position); _bandage.SetPosition(1, _objectToHook); };
        limitVelocity = () => { if (_rb.velocity.magnitude > _player.Speed) { _rb.velocity = _rb.velocity.normalized * _player.Speed; } };
    }

    #region Move

        public void MoveTank (float rotationInput, float moveInput)
        {
            Quaternion rotation = Quaternion.Euler(0f, rotationInput * _player.SpeedRotation * Time.deltaTime, 0f);
            _rb.rotation = (_rb.rotation * rotation);
    
            Vector3 movemente = _player.transform.forward * (moveInput * _player.Speed * Time.deltaTime);
            _rb.MovePosition(_rb.position + movemente);
        }
    
        public void MoveVariant(float movimientoHorizontal, float movimientoVertical)
        {
            Vector3 forward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z).normalized;
            Vector3 right = new Vector3(Camera.main.transform.right.x, 0, Camera.main.transform.right.z).normalized;
    
            Vector3 righMovement = right * (_player.Speed * Time.deltaTime * movimientoHorizontal);
            Vector3 upMovement = forward * (_player.Speed * Time.deltaTime * movimientoVertical);
            
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.Normalize(righMovement + upMovement), Vector3.up);
            
            _player.transform.rotation = Quaternion.Lerp(_player.transform.rotation, targetRotation, Time.deltaTime * _player.SpeedRotation);
    
            _player.transform.position += righMovement;
            _player.transform.position += upMovement;
        }

    #endregion


    #region Mouse

    public void Aim()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            GameObject bandageGameObject = Object.Instantiate(_player._bandagesPrefab, _player.transform.position, Quaternion.identity);
            Rigidbody rb = bandageGameObject.GetComponent<Rigidbody>();
            float velocity = Vector3.Distance( _player.transform.position, hitInfo.point) / 1f;
            
            if (rb != null ) { Vector3 dir = (hitInfo.point - _player.transform.position); rb.velocity = dir.normalized * velocity; }
        }
    }


    #endregion

    
    #region Skills

    #region Hook

    public void Hook()
        {
            var minDistanceHook = 5;
            
            Collider[] hooks = Physics.OverlapSphere(_player.transform.position, minDistanceHook, LayerMask.GetMask("Hookeable"));
    
            if (hooks.Length > 0 ){
                if(_joint == null) { _joint = _player.gameObject.AddComponent<SpringJoint>(); _joint.autoConfigureConnectedAnchor = false; }
    
                if (!objectToHookUpdated){
                    foreach (var grapes in hooks){
                        var distance = Vector3.Distance(_player.transform.position, grapes.transform.position);
                        if (distance < minDistanceHook) { _objectToHook = grapes.transform.position; objectToHookUpdated = true; }
                    }
                }
            }
            
            if (objectToHookUpdated){
                _joint.connectedAnchor = _objectToHook;
                
                _joint.maxDistance = 2f;
                _joint.minDistance = 0.1f;
                
                _joint.spring = 100;
                _joint.damper = 100f;
                _joint.breakTorque = 1;
                _joint.massScale = 100f;
            }
        }

    #endregion
    
    #endregion
    
    
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

using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;


public class ModelPlayer
{
    Player _player;
    ControllerPlayer _controller;
    Rigidbody _rb;

    //HOOK
    Vector3 _objectToHook = Vector3.zero;
    private LineRenderer _bandage;
    private SpringJoint _joint;
    public bool objectToHookUpdated = false;
    private Collider[] _hookBeetle;


    public Action reset;
    public Action lineCurrent;
    public Action limitVelocity;
    public Action jointPreferencesBalanced;
    public Action jointPreferencesJump;
    public Action createSpring;
    public Action prueba;
    //HOOK

    //AIM
    public BandageBullet BandageBullet;
    //AIM

    //PICK UP
    private LayerMask pickableLayer = LayerMask.GetMask("Pickable");
    public bool hasObject { get; private set; }
    private Transform _objSelected;

    private float _objRotation = 10f;
    private float _objSpeed = 5f;


    public bool _isHook = false;

    //PICK UP
    public ModelPlayer(Player p)
    {
        _player = p;

        _rb = _player._rigidbody;
        _bandage = _player._bandage;
        _joint = _player._springJoint;

        reset = () =>
        {
            Object.Destroy(_joint);
            _bandage.enabled = false;
            objectToHookUpdated = false;
            _hookBeetle = null;
        }; //RESET DE HOOK

        lineCurrent = () =>
        {
            _bandage.enabled = true;
            _bandage.SetPosition(0, _player.transform.position);
            _bandage.SetPosition(1, _objectToHook);
        }; //VISUAL PROVISOARIO, PARA MOSTRAR EL LINERENDERER

        limitVelocity = () =>
        {
            if (_rb.velocity.magnitude > _player.Speed)
            {
                _rb.velocity = _rb.velocity.normalized * _player.Speed;
            }
        }; //LIMITO LA VELOCIDAD DEL RIGIDBODY

        jointPreferencesBalanced = () =>
        {
            createSpring?.Invoke();
            _joint.connectedAnchor = _objectToHook; //SETEO DE LAS PREFERENCES DEL SPRINGJOINT
            _joint.maxDistance = 2.5f;
            _joint.minDistance = 1.5f;
            _joint.spring = 75;
            _joint.damper = 12f;
            //_joint.breakTorque = 1;
            //_joint.massScale = 10;
            //_joint.tolerance = 0;
        };

        jointPreferencesJump = () =>
        {
            createSpring?.Invoke();
            _joint.connectedAnchor = _objectToHook; //SETEO DE LAS PREFERENCES DEL SPRINGJOINT
            _joint.maxDistance = 0.2f;
            _joint.minDistance = 0.1f;
            _joint.spring = 100;
            _joint.damper = 7;
            _joint.breakTorque = 1;
            _joint.massScale = 4.5f;
        };

        createSpring = () =>
        {
            if (_joint == null)
            {
                _joint = _player.gameObject.AddComponent<SpringJoint>();
                _joint.autoConfigureConnectedAnchor = false;
                objectToHookUpdated = true;
            }
        };
    }

    public void MoveTank(float rotationInput, float moveInput)
    {
        _player.SpeedRotation = 150;

        Quaternion _rotation = Quaternion.Euler(0f, rotationInput * _player.SpeedRotation * Time.deltaTime, 0f);
        _rb.rotation = (_rb.rotation * _rotation);

        Vector3 movemente = _player.transform.forward * (moveInput * _player.Speed * Time.deltaTime);
        _rb.MovePosition(_rb.position + movemente);
    }

    public void MoveVariant(float movimientoHorizontal, float movimientoVertical)
    {
        _player.SpeedRotation = 10;

        Vector3 forward =
            new Vector3(_player._cameraTransform.forward.x, 0, _player._cameraTransform.transform.forward.z).normalized;

        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;

        Vector3 righMovement = right * (_player.Speed * Time.deltaTime * movimientoHorizontal);
        Vector3 upMovement = forward * (_player.Speed * Time.deltaTime * movimientoVertical);

        Vector3 heading = (righMovement + upMovement).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(heading, Vector3.up);

        _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, targetRotation, Time.deltaTime * _player.SpeedRotation));

        _rb.MovePosition(_player.transform.position + heading * (_player.Speed * Time.deltaTime));
    }

    public void Shoot()
    {
        //GameObject bandage = ObjectPool.instance.GetPooledObjectBullet();
        //bandage.transform.position = _player.transform.position;
        //bandage.SetActive(true);
        BulletFactory.Instance.GetObjectFromPool();
        _player._stateMachinePlayer.ChangeState(PlayerState.Idle);

        var originalSize = new Vector3(0.5f, 0.5f, 0.5f);

        if (_player.transform.localScale.x > originalSize.x || _player.transform.localScale.y > originalSize.y ||
            _player.transform.localScale.z > originalSize.z)
        {
            _player.transform.localScale -= new Vector3(0.1f, 0.1f, 0.1f);
        }
    }

    public void HookBalanced()
    {
        _isHook = true;

        var minDistanceHook = 8;
        var minDistanceJump = 100;

        var pos = _player.transform.position;
        var rot = _player.transform.rotation;

        Vector3 offset = rot * new Vector3(0, 2, 5f);
        Vector3 boxcastPos = pos + offset;

        _hookBeetle = Physics.OverlapBox(boxcastPos, new Vector3(5, 8, 10) / 2, rot);

        foreach (var beetle in _hookBeetle)
        {
            if (_hookBeetle.Length > 0 && !objectToHookUpdated &&
                beetle.gameObject.layer == LayerMask.NameToLayer("Beetle"))
            {
                var distance = Vector3.Distance(pos, beetle.transform.position);

                if (beetle.gameObject.CompareTag("Hook") && distance <= minDistanceHook)
                {
                    _objectToHook = beetle.transform.position;
                    jointPreferencesBalanced?.Invoke();
                }
                else if (beetle.gameObject.CompareTag("BeetleJump") && distance <= minDistanceJump)
                {
                    _objectToHook = beetle.transform.position;
                    jointPreferencesJump?.Invoke();
                }
            }
        }
    }

    public void PickObject()
    {
        Debug.DrawRay(_player.transform.position, _player.transform.forward * 30, Color.green, 0.5f);
        RaycastHit hit;
        if (Physics.Raycast(_player.transform.position, _player.transform.forward, out hit, Mathf.Infinity,
                pickableLayer))
        {
            Debug.Log("Objeto recogido: " + hit.collider.gameObject.name);
            hasObject = true;
            _objSelected = hit.transform;
        }
    }

    public void MoveObject(float movimientoHorizontal, float movimientoVertical)
    {
        Debug.DrawRay(_objSelected.transform.position, _player.transform.position - _objSelected.transform.position,
            Color.red, 0.5f);
        RaycastHit hit;

        if (Physics.Raycast(_objSelected.transform.position,
                _player.transform.position - _objSelected.transform.position, out hit))
        {
            if (hit.collider.gameObject.tag != "Player")
                DropObject();
            else
            {
                Vector3 forward = new Vector3(_player._cameraTransform.forward.x, 0,
                    _player._cameraTransform.transform.forward.z).normalized;

                Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;

                Vector3 righMovement = right * (_objSpeed * Time.deltaTime * movimientoHorizontal);
                Vector3 upMovement = forward * (_objSpeed * Time.deltaTime * movimientoVertical);

                Vector3 heading = (righMovement + upMovement).normalized;

                Quaternion targetRotation = Quaternion.LookRotation(heading, Vector3.up);

                var _rbObj = _objSelected.GetComponent<Rigidbody>();
                _rbObj.MoveRotation(Quaternion.Lerp(_rbObj.rotation, targetRotation, Time.deltaTime * _objRotation));
                _rbObj.MovePosition(_objSelected.transform.position + heading * (_objSpeed * Time.deltaTime));
            }
        }
    }

    public void DropObject()
    {
        Debug.Log("Objeto soltado: " + _objSelected.name);
        hasObject = false;
        _objSelected = null;
    }
}
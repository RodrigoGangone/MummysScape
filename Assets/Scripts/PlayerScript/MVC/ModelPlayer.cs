using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class ModelPlayer
{
    Player _player;
    ControllerPlayer _controller;
    Rigidbody _rb;
    public DetectionBeetle _detectionBeetle;

    //HOOK
    public LineRenderer _bandageHook;
    private SpringJoint _springJoint;
    private Rigidbody _hookBeetle;

    public bool isHooking;

    public Action resetSpringForHook;
    public Action drawBandageHook;
    public Action limitVelocityRB;

    //PICK UP
    private LayerMask pickableLayer = LayerMask.GetMask("Pickable");
    public bool hasObject { get; private set; }
    private Transform _objSelected;

    private float _objRotation = 10f;
    private float _objSpeed = 5f;

    public ModelPlayer(Player p)
    {
        _player = p;

        _rb = _player._rigidbody;
        _bandageHook = _player._bandage;
        _springJoint = _player._springJoint;
        _detectionBeetle = _player._detectionBeetle;

        drawBandageHook = () => //Feedback visual de vendas //TODO: Esto deberia ir en la maquina de estados
        {
            _bandageHook.SetPosition(0, _player.transform.position);
            _bandageHook.SetPosition(1, _hookBeetle.transform.position);
        };

        limitVelocityRB = () => //Limitar velocidad del player //TODO: Esto deberia ir en la maquina de estados
        {
            if (_rb.velocity.magnitude > _player.Speed)
                _rb.velocity = _rb.velocity.normalized * _player.Speed;
        };

        resetSpringForHook = () => //Reset del springJoint
        {
            Object.Destroy(_springJoint);
            isHooking = false;
            _bandageHook.enabled = false;
        };
    }

    public void MoveTank(float rotationInput, float moveInput)
    {
        Quaternion _rotation = Quaternion.Euler(0f, rotationInput * _player.SpeedRotation * Time.deltaTime, 0f);
        _rb.rotation = (_rb.rotation * _rotation);

        Vector3 movemente = _player.transform.forward * (moveInput * _player.Speed * Time.deltaTime);
        _rb.MovePosition(_rb.position + movemente);
    }

    public void Move(float movimientoHorizontal, float movimientoVertical)
    {
        Vector3 forward =
            new Vector3(_player._cameraTransform.forward.x, 0, _player._cameraTransform.transform.forward.z).normalized;

        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;

        Vector3 righMovement = right * (_player.Speed * Time.deltaTime * movimientoHorizontal);
        Vector3 upMovement = forward * (_player.Speed * Time.deltaTime * movimientoVertical);

        Vector3 heading = (righMovement + upMovement).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(heading, Vector3.up);

        _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, targetRotation, Time.deltaTime * _player.SpeedRotation));

        _rb.velocity += heading;

        if (Math.Abs(_rb.velocity.x) > _player.Speed || Math.Abs(_rb.velocity.z) > _player.Speed)
        {
            var velocity = Vector3.ClampMagnitude(_rb.velocity, _player.Speed);
            velocity.y = _rb.velocity.y;
            _rb.velocity = velocity;
        }
    }

    public void ClampMovement()
    {
        var velocity = _rb.velocity;
        velocity.x = 0;
        velocity.z = 0;

        _rb.velocity = velocity;
    }

    public void MoveHooked(float movimientoHorizontal, float movimientoVertical)
    {
        Debug.Log("MOVE HOOKED");
        Vector3 forward = new Vector3(_player._cameraTransform.forward.x, 0, _player._cameraTransform.forward.z)
            .normalized;
        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;
        Vector3 rightMovement = right * (movimientoHorizontal * _player.Speed);
        Vector3 forwardMovement = forward * (movimientoVertical * _player.Speed);

        Vector3 movement = rightMovement + forwardMovement;
        if (movement.sqrMagnitude > 0.01f)
        {
            _rb.AddForce(movement, ForceMode.Acceleration);
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, targetRotation, Time.deltaTime * _player.SpeedRotation));
        }
    }


    public void Shoot()
    {
        if (_player.CurrentBandageStock > _player.MinBandageStock)
        {
            _player.CurrentBandageStock--;
            SizeHandler();
            BulletFactory.Instance.GetObjectFromPool();
        }
    }


    //TODO: Hay un componente de Unity que es 'ConfigurableSpringJoint'
    //TODO: sirve para limitar los movimientos en X/Y/Z, verificar eso
    public void Hook()
    {
        if (isHooking) return;

        _springJoint = _player.gameObject.AddComponent<SpringJoint>();
        _springJoint.autoConfigureConnectedAnchor = false;
        _hookBeetle = _detectionBeetle.currentBeetle;
        isHooking = true;

        switch (_detectionBeetle.currentBeetle.gameObject.tag)
        {
            case "Hook":
                _springJoint.anchor = Vector3.zero;
                _springJoint.connectedBody = _detectionBeetle.currentBeetle;
                _springJoint.maxDistance = 5f;
                _springJoint.minDistance = 4f;
                _springJoint.spring = 75;
                _springJoint.damper = 12f;
                break;

            case "BeetleJump":
                _springJoint.anchor = Vector3.zero;
                _springJoint.connectedBody = _detectionBeetle.currentBeetle;
                _springJoint.maxDistance = 1.5f;
                _springJoint.minDistance = 2f;
                _springJoint.spring = 100;
                _springJoint.damper = 12;
                break;
        }
    }

    //TODO:Al cambiar el tamaño del pj: cambiar mesh del body_low ... cambiar el tamaño del capsule collider
    public void SizeHandler() //Ejecutar este metodo cada vez que se dispare o agarre una venda.
    {
        _player._viewPlayer.PLAY_PUFF();
        
        switch (_player.CurrentBandageStock)
        {
            case 2:
                //Normal size
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int) PlayerSize.Normal]);
                _player._anim.SetLayerWeight(1, 0);
                _player.CurrentPlayerSize = PlayerSize.Normal;
                break;
            case 1:
                //Small size
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int) PlayerSize.Small]);
                _player._anim.SetLayerWeight(1, 1);
                _player.CurrentPlayerSize = PlayerSize.Small;
                break;
            case 0:
                //Head size
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int) PlayerSize.Small]);
                _player.CurrentPlayerSize = PlayerSize.Head;
                break;
            default:
                //size def
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int) PlayerSize.Normal]);
                _player._anim.SetLayerWeight(1, 0);
                _player.CurrentPlayerSize = PlayerSize.Normal;
                break;
        }
    }

    //TODO: ver que hacer con "Tomar objetos"

    #region PickUpItems

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

    #endregion
}
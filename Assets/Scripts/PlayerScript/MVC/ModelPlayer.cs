using System;
using System.Collections;
using UnityEngine;

public class ModelPlayer
{
    Player _player;
    ControllerPlayer _controller;
    Rigidbody _rb;

    //HOOK
    public DetectionHook detectionBeetle;
    public SpringJoint springJoint;
    public Rigidbody hookBeetle;

    public bool isHooking;
    public bool finishAnimationHook;

    //PUSH OBJECT
    private Transform _currentBox;
    private Vector3 _dirToPush;

    public Transform CurrentBox => _currentBox;

    //PICK UP
    private LayerMask _pickableLayer = LayerMask.GetMask("Pickable");

    public bool hasObject { get; private set; }

    private Transform _objSelected;

    private float _objRotation = 10f;
    private float _objSpeed = 5f;

    public Action SizeModify;
    public Func<Transform, GameObject> CreateBandage;

    public ModelPlayer(Player p)
    {
        _player = p;
        _rb = _player._rigidbody;

        springJoint = _player._springJoint;
        detectionBeetle = _player._detectionBeetle;
    }

    public void CountBandage(int sum)
    {
        _player.CurrentBandageStock += sum;
        SizeHandler();
    }

    public void SpawnBandage(Transform trans = null)
    {
        CreateBandage(trans ?? _player.dropTarget);
    }

    public void Move(float moveHorizontal, float moveVertical)
    {
        Vector3 forward =
            new Vector3(_player._cameraTransform.forward.x, 0, _player._cameraTransform.transform.forward.z).normalized;

        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;

        Vector3 righMovement = right * (_player.Speed * Time.deltaTime * moveHorizontal);
        Vector3 upMovement = forward * (_player.Speed * Time.deltaTime * moveVertical);

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

    public void MovePush(float moveHorizontal, float moveVertical)
    {
        Vector3 forward = new Vector3(_player._cameraTransform.forward.x, 0, _player._cameraTransform.forward.z)
            .normalized;
        Vector3 right = Quaternion.Euler(0, 90, 0) * forward;

        Vector3 rightMovement = right * (_player.SpeedPush * Time.deltaTime * moveHorizontal);
        Vector3 forwardMovement = forward * (_player.SpeedPush * Time.deltaTime * moveVertical);

        Vector3 movement = rightMovement + forwardMovement;

        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            _player.transform.rotation = Quaternion.Lerp(_player.transform.rotation, targetRotation,
                Time.deltaTime * _player.SpeedRotation);
        }

        _player.transform.position += movement;

        if (_currentBox != null)
        {
            _currentBox.transform.position += _dirToPush * _player.SpeedPush * Time.deltaTime;
        }
    }

    public void ClampMovement()
    {
        var velocity = _rb.velocity;
        velocity.x = 0;
        velocity.z = 0;

        _rb.velocity = velocity;
    }

    public void MoveHooked(float moveHorizontal, float moveVertical)
    {
        Debug.Log("MOVE HOOKED");
        Vector3 forward = new Vector3(_player._cameraTransform.forward.x, 0, _player._cameraTransform.forward.z)
            .normalized;
        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;
        Vector3 rightMovement = right * (moveHorizontal * _player.Speed);
        Vector3 forwardMovement = forward * (moveVertical * _player.Speed);

        Vector3 movement = rightMovement + forwardMovement;
        if (movement.sqrMagnitude > 0.01f)
        {
            _rb.AddForce(movement, ForceMode.Acceleration);
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, targetRotation, Time.deltaTime * _player.SpeedRotation));
        }
    }

    public bool IsTouchingWall()
    {
        // Pos del player pero a la altura del "Target"
        var _rayCheckShootPos = new Vector3(_player.transform.position.x,
            _player.ShootTargetTransform.position.y,
            _player.transform.position.z);

        // Solo detectar la capa "Wall"
        int wallLayer = LayerMask.NameToLayer("Wall");
        int layerMaskWall = 1 << wallLayer;

        RaycastHit hit;
        if (Physics.Raycast(_rayCheckShootPos, _player.transform.forward, out hit, _player.RayCheckShootDistance,
                layerMaskWall))
        {
            Debug.Log("Raycast IsTouchingWall: true");
            return true;
        }

        return false;
    }

    public void Shoot()
    {
        if (_player.CurrentBandageStock > _player.MinBandageStock)
        {
            BulletFactory.Instance.GetObjectFromPool();
            CountBandage(-1);
        }
    }

    public void RotatePreShoot()
    {
        var hitPoint = ButtonHit()?.transform.position;

        if (hitPoint != null)
            _player.StartCoroutine(SmoothRotation(hitPoint.Value));
    }

    private IEnumerator SmoothRotation(Vector3 buttonPosition)
    {
        Quaternion startRotation = _player.transform.rotation;
        Vector3 directionToButton = (buttonPosition - _player.transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToButton);

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            _player.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _player.transform.rotation = targetRotation;
    }

    private RaycastHit? ButtonHit()
    {
        Vector3[] origins =
        {
            _player.ShootTargetTransform.position + _player.transform.right * 0.75f,
            _player.ShootTargetTransform.position - _player.transform.right * 0.75f,
        };

        foreach (var origin in origins)
        {
            RaycastHit hit;

            if (Physics.Raycast(origin, _player.transform.forward, out hit, 12f))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Button")) return hit;
            }
        }

        return null;
    }

    public bool CanPushBox()
    {
        var rayOrigin = new Vector3(
            _player.transform.position.x,
            _player.ShootTargetTransform.position.y,
            _player.transform.position.z
        );

        var movableBoxLayer = LayerMask.NameToLayer("MovableBox");
        var layerMaskBox = 1 << movableBoxLayer;

        if (Physics.Raycast(rayOrigin, _player.transform.forward, out var hit,
                _player.RayCheckPushDistance, layerMaskBox))
        {
            // Obtengo a la caja entera
            _currentBox = hit.collider.transform.parent;

            // Mueve al jugador y la caja en la direcc opuesta
            _dirToPush = hit.collider.gameObject.name switch
            {
                "Forward" => Vector3.back,
                "Backward" => Vector3.forward,
                "Left" => Vector3.right,
                "Right" => Vector3.left,
                _ => _dirToPush
            };

            return true;
        }

        _currentBox = null;
        _dirToPush = Vector3.zero;
        return false;
    }

    public void ActivateParticleButtonInView()
    {
        //Check si hay boton chocando con raycast de disparo
        if (ButtonHit().HasValue)
        {
            var activateObjects = ButtonHit()?.collider.gameObject.GetComponent<ActivateObjectsBullet>();
            if (activateObjects != null)
                activateObjects.ActivateParticles();
        }
    }

    //TODO: Hay un componente de Unity que es 'ConfigurableSpringJoint'
    //TODO: sirve para limitar los movimientos en X/Y/Z, verificar eso
    public void Hook()
    {
        if (isHooking) return;
        isHooking = true;

        springJoint = _player.gameObject.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;

        switch (hookBeetle.gameObject.tag)
        {
            case "Hook":
                springJoint.anchor = Vector3.zero;
                springJoint.connectedBody = hookBeetle;
                springJoint.maxDistance = 5f;
                springJoint.minDistance = 4f;
                springJoint.spring = 75;
                springJoint.damper = 12f;
                break;

            case "HookJump":
                springJoint.anchor = Vector3.zero;
                springJoint.connectedBody = hookBeetle;
                springJoint.maxDistance = 1.5f;
                springJoint.minDistance = 2f;
                springJoint.spring = 100;
                springJoint.damper = 12;
                break;
        }

        finishAnimationHook = true;
    }

    private void SizeHandler() //Ejecutar este metodo cada vez que se dispare o agarre una venda.
    {
        _player._viewPlayer.PLAY_PUFF();
        SizeModify?.Invoke();
        //TODO: cambiar el tamaño del capsule collider dependiendo el tamaño
        switch (_player.CurrentBandageStock)
        {
            case 2:
                //Normal size
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int)PlayerSize.Normal]);
                _player._anim.SetLayerWeight(1, 0);
                _player._anim.SetLayerWeight(2, 0); //TODO MODIFICAR ESTO PARA QUE QUEDE EN LA VIEW
                _player.CurrentPlayerSize = PlayerSize.Normal;
                break;
            case 1:
                //Small size
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int)PlayerSize.Small]);
                _player._anim.SetLayerWeight(1, 1);
                _player._anim.SetLayerWeight(2, 0);
                _player.CurrentPlayerSize = PlayerSize.Small;
                break;
            case 0:
                //Head size
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int)PlayerSize.Head]);
                _player._anim.SetLayerWeight(1, 0);
                _player._anim.SetLayerWeight(2, 1);
                _player.CurrentPlayerSize = PlayerSize.Head;
                break;
            default:
                //size def
                _player._viewPlayer.ChangeMesh(_player._Meshes[(int)PlayerSize.Normal]);
                _player._anim.SetLayerWeight(1, 0);
                _player._anim.SetLayerWeight(2, 0);
                _player.CurrentPlayerSize = PlayerSize.Normal;
                break;
        }

        _player._viewPlayer.AdjustColliderSize();
    }

    public void LimitVelocityRb()
    {
        if (_rb.velocity.magnitude > _player.Speed)
            _rb.velocity = _rb.velocity.normalized * _player.Speed;
    }

    #region PickUpItems

    public void PickObject()
    {
        Debug.DrawRay(_player.transform.position, _player.transform.forward * 30, Color.green, 0.5f);
        RaycastHit hit;
        if (Physics.Raycast(_player.transform.position, _player.transform.forward, out hit, Mathf.Infinity,
                _pickableLayer))
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
            if (hit.collider.gameObject.tag != "PlayerFather")
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
                _rbObj.MoveRotation(Quaternion.Lerp(_rbObj.rotation, targetRotation,
                    Time.deltaTime * _objRotation));
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

    public bool CheckGround()
    {
        Debug.DrawRay(_player.transform.position, Vector3.down, Color.red, 0.05f);

        return Physics.Raycast(_player.transform.position, Vector3.down, out _, 0.05f);
    }

    #endregion
}
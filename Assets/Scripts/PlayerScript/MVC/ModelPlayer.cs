using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static Utils;
using Object = UnityEngine.Object;

public class ModelPlayer
{
    Player _player;
    ControllerPlayer _controller;
    Rigidbody _rb;

    //DROP
    public Vector3 dropPosition;

    //HOOK
    public DetectionHook detectionBeetle;
    public SpringJoint springJoint;
    public Rigidbody hookBeetle;

    public bool isHooking;

    //PUSH OBJECT
    private Transform _currentBox;
    public bool isPulling;
    private Vector3 _dirToPush;
    private Vector3 _dirToPull;
    
    public Transform CurrentBox => _currentBox;
    public Vector3 DirToPush => _dirToPush;

    public Action SizeModify;

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

    public void CreateBandageAtPosition(Vector3 position)
    {
        Object.Instantiate(_player._prefabBandage, position, Quaternion.identity);
    }

    public void DropBandage()
    {
        CountBandage(-1);
        Object.Instantiate(_player._prefabBandage, dropPosition, Quaternion.identity);
    }

    public bool CanDropBandage()
    {
        LayerMask wallLayerMask = LayerMask.GetMask("Wall");
        bool isTouchingWall;

        Vector3[] directions =
        {
            _player.transform.forward,
            -_player.transform.forward,
            _player.transform.right,
            -_player.transform.right
        };

        Vector3[] localOffsets =
        {
            _player.transform.forward * 0.65f + new Vector3(0, 1f, 0), // NO TOCAR ESTOS VALORES
            -_player.transform.forward * 0.65f + new Vector3(0, 1f, 0), // NO TOCAR ESTOS VALORES
            _player.transform.right * 0.65f + new Vector3(0, 1f, 0), // NO TOCAR ESTOS VALORES
            -_player.transform.right * 0.65f + new Vector3(0, 1f, 0) // NO TOCAR ESTOS VALORES
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 origin = _player.transform.position + localOffsets[i];
            Quaternion orientation = _player.transform.rotation;

            RaycastHit[] hits = Physics.BoxCastAll(
                origin,
                _player.BoxHalfExt,
                directions[i],
                orientation,
                _player.MaxDistance,
                wallLayerMask
            );

            isTouchingWall = false; // Reinicia el estado 

            foreach (var hit in hits)
            {
                if (((1 << hit.collider.gameObject.layer) & wallLayerMask) != 0) // Verif si es "Wall"
                {
                    isTouchingWall = true;
                    break;
                }
            }

            if (!isTouchingWall)
            {
                dropPosition = origin + directions[i] * _player.MaxDistance;
                return true;
            }
        }

        dropPosition = Vector3.zero;
        return false;
    }

    public void Move(float moveHorizontal, float moveVertical, float speed, float rotation)
    {
        Vector3 forward =
            new Vector3(_player._cameraTransform.forward.x, 0, _player._cameraTransform.transform.forward.z).normalized;

        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;

        Vector3 righMovement = right * (speed * Time.deltaTime * moveHorizontal);
        Vector3 upMovement = forward * (speed * Time.deltaTime * moveVertical);

        Vector3 heading = (righMovement + upMovement).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(heading, Vector3.up);

        _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, targetRotation, Time.deltaTime * rotation));

        _rb.velocity += heading;

        if (Math.Abs(_rb.velocity.x) > speed || Math.Abs(_rb.velocity.z) > speed)
        {
            var velocity = Vector3.ClampMagnitude(_rb.velocity, speed);
            velocity.y = _rb.velocity.y;
            _rb.velocity = velocity;
        }
    }

    public void MovePull()
    {
        Vector3 playerPosition = _player.transform.position;
        Vector3 boxPosition = _currentBox.transform.position;

        Vector3 directionToBox = (boxPosition - playerPosition).normalized;

        directionToBox.y = 0;

        if (directionToBox != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToBox, Vector3.up);
            _player.transform.rotation = Quaternion.Lerp(_player.transform.rotation, targetRotation,
                Time.deltaTime * _player.SpeedRotation);
        }

        Vector3 directionToPlayer = (playerPosition - boxPosition).normalized;

        _currentBox.transform.position += directionToPlayer * (_player.SpeedPull * Time.deltaTime);
    }

    public bool IsBoxCloseToPlayer(float maxDistance = 2f)
    {
        Vector3 playerPosition = _player.transform.position;
        Vector3 boxPosition = _currentBox.transform.position;

        float distance = Vector3.Distance(playerPosition, boxPosition);

        return distance <= maxDistance;
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
        Vector3 origin = _player.ShootTargetTransform.position;

        Quaternion leftRotation = Quaternion.Euler(0, -10, 0);
        Quaternion rightRotation = Quaternion.Euler(0, 10, 0);

        Vector3 leftDirection = leftRotation * _player.transform.forward;
        Vector3 rightDirection = rightRotation * _player.transform.forward;
        Vector3 centerDirection = _player.transform.forward;

        Vector3[] directions = { leftDirection, rightDirection, centerDirection };

        foreach (var direction in directions)
        {
            RaycastHit hit;

            if (Physics.Raycast(origin, direction, out hit, 12f))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Button"))
                {
                    hit.transform.GetComponent<InteractableOutline>().UpdateMaterialStatus(1);
                    return hit;
                }
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

            // Dir opuesta para mov de caja
            _dirToPush = hit.collider.gameObject.name switch
            {
                BOX_SIDE_FORWARD => Vector3.back,
                BOX_SIDE_BACKWARD => Vector3.forward,
                BOX_SIDE_LEFT => Vector3.right,
                BOX_SIDE_RIGHT => Vector3.left,
                _ => Vector3.zero
            };
            
            hit.transform.GetComponent<InteractableOutline>().UpdateMaterialStatus(1);

            return true;
        }

        _currentBox = null;
        _dirToPush = Vector3.zero;
        return false;
    }

    public bool CanPullBox()
    {
        var rayOrigin = new Vector3(
            _player.transform.position.x,
            _player.ShootTargetTransform.position.y,
            _player.transform.position.z
        );

        var movableBoxLayer = LayerMask.NameToLayer("MovableBox");
        var layerMaskBox = 1 << movableBoxLayer;

        if (Physics.Raycast(rayOrigin, _player.transform.forward, out var hit,
                _player.RayCheckPullDistance, layerMaskBox))
        {
            _currentBox = hit.collider.transform.parent;

            //TODO: mejorar esto o morir en el intento
            if (_currentBox.GetComponent<PushPullObject>().CheckPlayerRaycast() != null &&
                _currentBox.GetComponent<PushPullObject>().CheckPlayerRaycast()
                    .Equals(hit.collider.gameObject.name))
            {
                _dirToPull = hit.collider.gameObject.name switch
                {
                    BOX_SIDE_FORWARD => Vector3.forward,
                    BOX_SIDE_BACKWARD => Vector3.back,
                    BOX_SIDE_LEFT => Vector3.left,
                    BOX_SIDE_RIGHT => Vector3.right,
                    _ => Vector3.zero
                };

                hit.transform.GetComponent<InteractableOutline>().UpdateMaterialStatus(1);
                
                return true;
            }
        }

        _currentBox = null;
        _dirToPull = Vector3.zero;
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
    }

    private void SizeHandler() //Ejecutar este metodo cada vez que se dispare o agarre una venda.
    {
        _player._viewPlayer.PLAY_PUFF();
        SizeModify?.Invoke();

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

    public bool CheckGround()
    {
        Debug.DrawRay(_player.transform.position, Vector3.down, Color.red, 0.1f);

        return Physics.Raycast(_player.transform.position, Vector3.down, out _, 0.1f);
    }
}
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;


public class ModelPlayer
{
    Player _player;
    ViewPlayer _view;
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
    //HOOK

    //AIM
    public BandageBullet BandageBullet;
    //AIM

    //PICK UP
    private LayerMask _mask;

    private Transform _objSelected;

    private float _pickLimit = 5f;

    Ray _click;

    //PICK UP
    public ModelPlayer(Player p, SpringJoint springJoint, ViewPlayer v)
    {
        _player = p;
        _view = v;
        _rb = _player.GetComponent<Rigidbody>();
        _bandage = _player.GetComponent<LineRenderer>();
        _joint = springJoint;

        reset = () => { Object.Destroy(_joint); _bandage.enabled = false; objectToHookUpdated = false; _hookBeetle = null; }; //RESET DE HOOK
        
        lineCurrent = () => { _bandage.enabled = true; _bandage.SetPosition(0, _player.transform.position); _bandage.SetPosition(1, _objectToHook); }; //VISUAL PROVISOARIO, PARA MOSTRAR EL LINERENDERER
        
        limitVelocity = () => { if (_rb.velocity.magnitude > _player.Speed) { _rb.velocity = _rb.velocity.normalized * _player.Speed; } }; //LIMITO LA VELOCIDAD DEL RIGIDBODY
        
        jointPreferencesBalanced = () =>
        {
            createSpring?.Invoke();
            _joint.connectedAnchor = _objectToHook; //SETEO DE LAS PREFERENCES DEL SPRINGJOINT
            _joint.maxDistance = 2f;
            _joint.minDistance = 0.1f;
            _joint.spring = 15;
            _joint.damper = 10;
            _joint.breakTorque = 1;
            _joint.massScale = 100f;
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

        Vector3 forward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z).normalized;

        Vector3 right = Quaternion.Euler(new Vector3(0, 90, 0)) * forward;

        Vector3 righMovement = right * (_player.Speed * Time.deltaTime * movimientoHorizontal);
        Vector3 upMovement = forward * (_player.Speed * Time.deltaTime * movimientoVertical);

        Vector3 heading = (righMovement + upMovement).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(heading, Vector3.up);

        _rb.MoveRotation(Quaternion.Lerp(_rb.rotation, targetRotation, Time.deltaTime * _player.SpeedRotation));

        _rb.MovePosition(_player.transform.position + heading * _player.Speed * Time.deltaTime);

        //_player.transform.rotation = Quaternion.Lerp(_player.transform.rotation, targetRotation,
        //    Time.deltaTime * _player.SpeedRotation);

        //_player.transform.position += heading*_player.Speed*Time.deltaTime;
    }

    //TODO:REHACER UTILIZANDO UNA POOL DE OBJETOS [LISTO]

    //TODO: MEJORAR CODIGO, POR EJEMPLO, LO LOGICO SERIA QUE LA MOMIA PUEDA DISPARAR 2 VENDAS AL MISMO TIEMPO COMO MAXIMO
    //TODO: POR ESO, DEBERIAMOS TENER EN CUENTA ESO PARA APLICAR LOS FEEDBACKS DE LOS INDICADORES DE AIM. 
    public void Aim()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            float velocity = Vector3.Distance(_player.transform.position, hitInfo.point) / 1f;

            GameObject bandage = ObjectPool.instance.GetPooledObjectBullet();

            bandage.transform.position = _player.transform.position;
            bandage.SetActive(true);

            Rigidbody rb = bandage.GetComponent<Rigidbody>();
            Vector3 dir = (hitInfo.point - _player.transform.position);
            rb.velocity = dir.normalized * velocity;

            _view.IndicatorAimOn(hitInfo.point);
        }
    }
    
    public void HookBalanced() //TODO: CONFIGURAR PARA QUE EL RAYCAST SOLO TOME OBJETOS EN EL RANGO DE VISION DEL PERSONAJE
    {
        var minDistanceHook = 5;
        var minDistanceJump = 100;

        var pos = _player.transform.position;
        var rot = _player.transform.rotation;

        Vector3 offset = rot * new Vector3(0, 2, 5f);
        Vector3 boxcastPos = pos + offset;

        _hookBeetle = Physics.OverlapBox(boxcastPos, new Vector3(5, 5, 10) / 2, rot);

        foreach (var beetle in _hookBeetle)
        {
            if (_hookBeetle.Length > 0 && !objectToHookUpdated && beetle.gameObject.layer == LayerMask.NameToLayer("Beetle"))
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

    public void CheckPick()
    {
        _click = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(_click, out hit) && hit.collider.CompareTag("MoveObject"))
        {
            _objSelected = hit.transform;
        }
    }

    public void Pick()
    {
        if (_objSelected != null )
        {
            Ray rayo = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            Debug.DrawRay(_player.transform.position, _objSelected.position - _player.transform.position, Color.red, 0.5f);
            if (Physics.Raycast(_player.transform.position,_objSelected.position - _player.transform.position, out var hit) && !hit.collider.CompareTag("MoveObject"))
            {
                Drop();
                return;
            }
            if (Physics.Raycast(rayo, out var hitInfo, Mathf.Infinity, LayerMask.GetMask("Ground")))
            {
                Vector3 nuevaPosicion = hitInfo.point;
                nuevaPosicion.y = _objSelected.position.y;
                _objSelected.position = nuevaPosicion;
            }
        }
    }

    public void Drop()
    {
        // Si se suelta el botón izquierdo del ratón, restablecer el objeto seleccionado
        _objSelected = null;
    }
}

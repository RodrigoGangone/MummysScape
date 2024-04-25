using System;
using Unity.VisualScripting.Dependencies.NCalc;
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
    //HOOK
    
    //AIM
    public BandageBullet BandageBullet;
    //AIM
    
    //PICK UP
    private LayerMask _mask;

    private Transform _objSelected;

    private float _pickLimit = 5f;

    //PICK UP
    public ModelPlayer(Player p, SpringJoint springJoint, ViewPlayer v)
    {
        _player = p;
        _view = v;
        _rb = _player.GetComponent<Rigidbody>();
        _bandage = _player.GetComponent<LineRenderer>();
        _joint = springJoint;

        reset = () => { Object.Destroy(_joint); _bandage.enabled = false; objectToHookUpdated = false; };
        lineCurrent = () => { _bandage.enabled = true; _bandage.SetPosition(0, _player.transform.position); _bandage.SetPosition(1, _objectToHook); };
        limitVelocity = () => { if (_rb.velocity.magnitude > _player.Speed) { _rb.velocity = _rb.velocity.normalized * _player.Speed; } };
    }

    #region Move
    public void MoveTank (float rotationInput, float moveInput)
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

    #endregion

    #region Mouse

    //TODO:REHACER UTILIZANDO UNA POOL DE OBJETOS [LISTO]

    //TODO: MEJORAR CODIGO, POR EJEMPLO, LO LOGICO SERIA QUE LA MOMIA PUEDA DISPARAR 2 VENDAS AL MISMO TIEMPO COMO MAXIMO
    //TODO: POR ESO, DEBERIAMOS TENER EN CUENTA ESO PARA APLICAR LOS FEEDBACKS DE LOS INDICADORES DE AIM. 
    public void Aim() 
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            float velocity = Vector3.Distance( _player.transform.position, hitInfo.point) / 1f;
            
            GameObject bandage = ObjectPool.instance.GetPooledObjectBullet();
            
            bandage.transform.position = _player.transform.position;
            bandage.SetActive(true);
            
            Rigidbody rb = bandage.GetComponent<Rigidbody>();
            Vector3 dir = (hitInfo.point - _player.transform.position); rb.velocity = dir.normalized * velocity;

            _view.IndicatorAimOn(hitInfo.point);
        }
    }
    #endregion
    
    #region Abilities
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

        #region  PickUp Item

        public void CheckPick()
        {
            /// Obtener la posición del ratón en el mundo
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            //
            if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("MoveObject"))
            {
                if (Physics.Raycast(_player.transform.position, hit.point - _player.transform.position) && hit.collider.CompareTag("MoveObject"))
                {
                    Debug.Log("Objeto seleccionado: " + hit.collider.gameObject.name);
                    _objSelected = hit.transform;
                }
            }
            Debug.DrawRay(_player.transform.position, (hit.point - _player.transform.position), Color.green, 0.5f);
            //
            //------------------------------------------------------------------------------------------------------------------------//

            // Obtener la posición del ratón en el mundo
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //RaycastHit hit;
//
            //if (Physics.Raycast(ray, out hit, _pickLimit, _mask)) // Ajuste para que solo interactúe con objetos en la capa "MoveObject"
            //{
            //    Debug.Log("Objeto seleccionado: " + hit.collider.gameObject.name);
//
            //    // Verificar si el objeto está dentro del límite de alcance
            //    float distanceToHit = Vector3.Distance(_player.transform.position, hit.point);
            //    if (distanceToHit <= _pickLimit)
            //    {
            //        _objSelected = hit.transform;
            //    }
            //}
//
            //// Calcular el final del rayo
            //Vector3 endRay = ray.GetPoint(_pickLimit);
//
            //// Debug del rayo
            //Debug.DrawRay(_player.transform.position, endRay, Color.red, 1f);

        } 
        
        public void Pick()
        {
            RaycastHit hit;
            if (_objSelected != null && Physics.Raycast(_player.transform.position, _objSelected.position, out hit) && hit.collider.CompareTag("MoveObject"))
            {

                Debug.DrawRay(_player.transform.position, _objSelected.position, Color.cyan);


                // Convertir la posición del mouse desde la pantalla a un rayo en el mundo
                Ray rayo = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane plano = new Plane(Vector3.up, _objSelected.position);
                if (plano.Raycast(rayo, out float distancia))
                {
                    // Calcular la nueva posición del objeto en el plano
                    Vector3 nuevaPosicion = rayo.GetPoint(distancia);
                    // Mantener la misma altura que el objeto seleccionado
                    nuevaPosicion.y = _objSelected.position.y;
                    // Actualizar la posición del objeto seleccionado
                    _objSelected.position = nuevaPosicion;
                }
            }
            else
                Drop();

        }

        public void Drop()
        {
            // Si se suelta el botón izquierdo del ratón, restablecer el objeto seleccionado
            _objSelected = null;
        }
   

        #endregion
    #endregion

}

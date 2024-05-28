using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickSandNEW : MonoBehaviour
{
    [Header("ATRIBUTES INVISIBLE")]
    private Vector3 _startPosInvisible; //posicion a la que va a regresar la plat invisible
    private Transform _invisiblePlatform; //referencia de la plataforma invisible
    
    [Header("MOVE QUICKSAND")]
    private Vector3 _startPosQuickSand;
    private Vector3 _endPosQuickSand; //Se setea por hierarchy
    [SerializeField] private float yVariant;

    [Header("VARIABLES")] 
    [SerializeField] private float _speedSink; //Velocidad de hundision
    [SerializeField] private float _speedMove; //Velocidad de movision
    private bool _onQuicksand; //verifico si el player esta en la arena movediza
    [SerializeField] private bool isActivated; //Si se activo el metodo para mover la arena
    private float _timeInvPlat;
    private float _timeActSand;


    private void Awake()
    {
        //Posicion inicial de la arena (visual e invisible)
        _startPosQuickSand = transform.position;
        _endPosQuickSand = new Vector3(_startPosQuickSand.x,yVariant, _startPosQuickSand.z);
        
        //La posicion inicial a donde debe volver la plat invisible
        _startPosInvisible = transform.position;
        //Referencia de plataforma invisible
        _invisiblePlatform = transform.GetChild(0);
    }

    void Update()
    {
        if (isActivated)
            ActivateSand();
        
        if (!_onQuicksand) return;
        DownInvisiblePlatform();
    }

    public void ActivateSand()
    {
        isActivated = true;
        
        _timeActSand += Time.deltaTime * _speedMove;
        
        transform.position = Vector3.Lerp(_startPosQuickSand, _endPosQuickSand, _timeActSand);

        if (Vector3.Distance(transform.position, _endPosQuickSand) <= 0.1f)
        {
            isActivated = false;

            var newStartPos = _endPosQuickSand;
            var oldStartPos = _startPosQuickSand;

            _startPosQuickSand = newStartPos;
            _endPosQuickSand = oldStartPos;
            _timeActSand = 0;
        }
    }

    private void DownInvisiblePlatform()
    {
        _timeInvPlat += Time.deltaTime * _speedSink;
        var y = _speedSink != 0 ? _timeInvPlat : 0f;
        _invisiblePlatform.position = _startPosInvisible + new Vector3(0, -y, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;
        _onQuicksand = true;

        var player = other.gameObject.GetComponent<Player>();
        player.ChangeSpeed();

        other.transform.SetParent(_invisiblePlatform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;
        
            _onQuicksand = false;
            _timeInvPlat = 0;

            var player = other.gameObject.GetComponent<Player>();
            player.Speed = player.SpeedOriginal;
            other.transform.SetParent(null);

            transform.position = _startPosInvisible;
    }
}

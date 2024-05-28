using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Quicksand : MonoBehaviour
{
    [Header("ATRIBUTES INVISIBLE")] [SerializeField]
    private Transform _startPosInvisible;

    [SerializeField] Transform _invisiblePlatform;

    [Header("VARIABLES")] [SerializeField] private float _speedSink;
    private bool _onQuicksand;
    private Player _player;
    private float _time;

    public int _nextPos = 0;
    [SerializeField] private List<Transform> _sandPos;

    private void Start()
    {
        transform.position = _sandPos[0].position;
    }


    private void Update()
    {
        if (Vector3.Distance(transform.position, _sandPos[_nextPos].position) > 0)
        {
            MoveSand();
        }

        if (!_onQuicksand) return;
        DownPlatform();
    }

    #region DownQuickSand

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _player = other.gameObject.GetComponent<Player>();
        _player.ChangeSpeed();
        _onQuicksand = true;
        other.transform.SetParent(_invisiblePlatform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerFather")) ;
        {
            _player = other.gameObject.GetComponent<Player>();
            _player.Speed = _player.SpeedOriginal;
            _onQuicksand = false;
            other.transform.SetParent(null);
            transform.position = _startPosInvisible.position;
        }
    }

    private void DownPlatform()
    {
        _time += Time.deltaTime * _speedSink;

        var y = _speedSink != 0 ? _time : 0f;

        transform.position = _startPosInvisible.position + new Vector3(0, -y, 0);
    }

    #endregion

    #region MoveQuickSand

    //public void NextPosSand(TypeSandButton type) //Recibe por medio del boton si debe subir o bajar su posicion
    //{
    //    Debug.Log("ENTRE A NEXT POST SAND");
    //    if (_nextPos < _sandPos.Count || _nextPos > 0)
    //    {
    //        if (type == TypeSandButton.DownSand)
    //            _nextPos--;
//
    //        if (type == TypeSandButton.UpSand)
    //            _nextPos++;
    //    }
    //}

    private void MoveSand()
    {
        Debug.Log("MoveSand");

        _time += Time.deltaTime * 0.01f;
        
        transform.position += new Vector3(0, _time, 0);
    }

    #endregion
}
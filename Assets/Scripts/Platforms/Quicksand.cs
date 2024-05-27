using System;
using TMPro;
using UnityEngine;

public class Quicksand : MonoBehaviour
{
    [SerializeField] private float _speedOriginal;
    [SerializeField] private float _speedSink;
    [SerializeField] private Transform _startPosition;
    private bool _onQuicksand;
    private Player _player;
    private float _time;

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;
        
        _player = other.gameObject.GetComponent<Player>();
        _player.ChangeSpeed();
        _onQuicksand = true;
        other.transform.SetParent(transform);
    }

    private void Update()
    {
        if (!_onQuicksand) return;
        DownPlatform();
    }

    private void DownPlatform()
    {
        _time += Time.deltaTime * _speedSink;

        var y = _speedSink != 0 ? _time : 0f;
        transform.position = _startPosition.position + new Vector3(0, -y, 0);
    }

    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("PlayerFather")) ;
        {
            _player = other.gameObject.GetComponent<Player>();
            _player.Speed = _player.SpeedOriginal;
            _onQuicksand = false;
            other.transform.SetParent(null);
            transform.position = _startPosition.position;
        }
    }
}
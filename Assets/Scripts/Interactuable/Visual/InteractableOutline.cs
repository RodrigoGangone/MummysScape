using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableOutline : MonoBehaviour
{
    private enum InteractableType
    {
        Hook,
        Eagle,
        MovableBox
    }

    [System.Serializable]
    private struct OperablesMapping
    {
        [SerializeField] private PlayerSize _size;
        [SerializeField] private bool _operable;

        public PlayerSize Size => _size;
        public bool IsOperable => _operable;
    }

    [SerializeField] private InteractableType _interactableType;

    public Player _player;

    [SerializeField] private List<OperablesMapping> _operablesMapping = new();

    [SerializeField][ColorUsage(true, true)] private Color _functional = new Color();
    [SerializeField][ColorUsage(true, true)] private Color _inoperable;

    private const string _inRange = "_IsInRange";
    private const string _color = "_Outline_Color";
    private const string _detected = "Detected";
    private const string _materialNameToFind = "InteractableOutline_Ma";

    [SerializeField] private List<Material> _materials = new();

    private bool _isCalling;
    private float _timer;
    [SerializeField] private float _timeoutDuration = 2f; // Tiempo en segundos para el temporizador

    private void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material.name.Contains(_materialNameToFind))
                {
                    _materials.Add(material);
                }
            }
        }

        _player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        //_player._modelPlayer.CanPullBox();
        //_player._modelPlayer.CanPushBox();

        if (!_isCalling)
        {
            _timer += Time.deltaTime;
            if (_timer >= _timeoutDuration)
            {
                _timer = 0f;
                SetOff();
            }
        }
        else
            _timer = 0f;
    }

    public void UpdateMaterialStatus(bool status = false)
    {
        _isCalling = true;
        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, status ? 1 : 0);
        }

        Color colorToSet = _inoperable; // Default color
        bool isOperable = false;

        foreach (var mapping in _operablesMapping)
        {
            if (mapping.Size == _player.CurrentPlayerSize)
            {
                isOperable = mapping.IsOperable;
                break;
            }
        }

        colorToSet = isOperable ? _functional : _inoperable;

        SetMaterialColor(colorToSet);
        _isCalling = false;
    }

    private void SetMaterialColor(Color color)
    {
        foreach (var material in _materials)
        {
            material.SetColor(_color, color);
        }
    }

    private void SetOff()
    {
        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, 0f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(_detected) || _interactableType != InteractableType.Hook) return;
        UpdateMaterialStatus(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(_detected) || _interactableType != InteractableType.Hook) return;
        SetOff();
    }
}
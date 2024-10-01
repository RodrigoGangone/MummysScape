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

    [SerializeField] [ColorUsage(true, true)]
    private Color _functional = new Color();

    [SerializeField] [ColorUsage(true, true)]
    private Color _inoperable;

    private const string _inRange = "_IsInRange";
    private const string _color = "_Outline_Color";
    private const string _detected = "Detected";
    private const string _materialNameToFind = "InteractableOutline_Ma";

    [SerializeField] private List<Material> _materials = new();

    private bool _materialOff;
    //TODO: HAY QUE USAR EL CURRENTBOX PARA GUARDAR EL OBJETO Y ENCENDER SU OUTLINE, MODIFICAR EL SCRIPT DEL PULL PARA QUE LO HAGA SIN LA NECESIDAD DE TOCAR EL INPUT

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
        if (!_player._modelPlayer.GetCurrentHit().HasValue &&
            _materialOff &&
            _interactableType != InteractableType.Hook)
            OffMaterial();
    }

    public void OnMaterial()
    {
        _materialOff = true;

        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, 1);
        }

        Color colorToSet = _inoperable;
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

        foreach (var material in _materials)
        {
            material.SetColor(_color, colorToSet);
        }
    }

    private void OffMaterial()
    {
        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, 0f);
        }

        _materialOff = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(_detected) || _interactableType != InteractableType.Hook) return;
        OnMaterial();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(_detected) || _interactableType != InteractableType.Hook) return;
        OffMaterial();
    }
}
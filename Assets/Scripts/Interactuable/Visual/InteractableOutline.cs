using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractableOutline : MonoBehaviour
{
    private enum InteractableType
    {
        Hook,
        Eagle,
        MovableBox,
        Teleport
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
    private const string _materialNameToFindForBox = "InteractableOutline_Ma_Box";

    [SerializeField] private List<Material> _materials = new();
    [SerializeField] public ParticleSystem _shiningParticles;

    private bool _materialOff;
    //TODO: HAY QUE USAR EL CURRENTBOX PARA GUARDAR EL OBJETO Y ENCENDER SU OUTLINE, MODIFICAR EL SCRIPT DEL PULL PARA QUE LO HAGA SIN LA NECESIDAD DE TOCAR EL INPUT

    private void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material.name.Contains(_materialNameToFind) || material.name.Contains(_materialNameToFindForBox))
                {
                    _materials.Add(material);
                }
            }
        }

        _player = FindObjectOfType<Player>();

        _player.SizeModify += SetColor;
    }

    public void OnMaterial()
    {
        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, 1);
        }

        SetColor();
    }

    private void SetColor()
    {
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

    public void OffMaterial()
    {
        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, 0f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(_detected) && _interactableType == InteractableType.Hook)
        {
            OnMaterial();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_detected) && _interactableType == InteractableType.Hook)
        {
            OffMaterial();
        }
    }


    //TODO: ESTE METODO SE CREO PARA MANEJAR LAS PARTICULAS, PERO COMO RECIBE CONSTNATES GOLPES DEL RAYCAST FUNCIONA MAL
    //TODO: EN CASO DE CORREGIRLO, TRABAJARLO DESDE ESTE METODO Y INTERACTABLEMANAGER
    //public void ShinningParticles()
    //{
    //    if (isOperable && !_shiningParticles.isPlaying)
    //        _shiningParticles.Play();
    //    else
    //    {
    //        _shiningParticles.Stop();
    //        _shiningParticles.Clear();
    //    }
    //}
}
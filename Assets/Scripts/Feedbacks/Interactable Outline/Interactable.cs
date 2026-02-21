using System.Collections.Generic;
using UnityEngine;
using static PlayerEnum; 

/// <summary> 
/// Feedback de Interacción: Controla el realce visual (outline) de los objetos, cambiando su color 
/// dinámicamente según si el tamaño actual del jugador permite operar el objeto. 
/// </summary>

public class Interactable : MonoBehaviour
{
    [Header("Configuración de Operable (SIMPLE)")]
    [SerializeField] private bool _operableByHead = false;
    [SerializeField] private bool _operableBySmall = false;
    [SerializeField] private bool _operableByNormal = false;

    [Header("Configuración de Material")]
    [SerializeField] [ColorUsage(true, true)]
    private Color _functional = new Color(0.5f, 0, 1, 1);

    [SerializeField] [ColorUsage(true, true)]
    private Color _inoperable = new Color(1, 0, 0, 1);

    private const string _inRange = "_IsInRange";
    private const string _color = "_Outline_Color";
    private const string _materialNameToFind = "InteractableOutline_Ma";
    private const string _materialNameToFindForBox = "InteractableOutline_Ma_Box";

    private List<Material> _materials = new();
    
    private PlayerSize _currentReportedPlayerSize;
    private bool _isOutlineOn = false;

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
    }
         
    private void OnPlayerSizeChanged(PlayerSize newSize)
    {
        _currentReportedPlayerSize = newSize;
        
        if (_isOutlineOn)
            SetColor();
    }
    
    public void OnMaterial()
    {
        if (_isOutlineOn) return; 
        _isOutlineOn = true;

        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, 1);
        }
        
        SetColor(); 
    }

    public void OffMaterial()
    {
        if (!_isOutlineOn) return; 
        _isOutlineOn = false;
        
        foreach (var material in _materials)
        {
            material.SetFloat(_inRange, 0f);
        }
    }

    private void SetColor()
    {
        if (_materials.Count == 0) return;

        bool isOperable = false;
        switch (_currentReportedPlayerSize)
        {
            case PlayerSize.Head:
                isOperable = _operableByHead;
                break;
            case PlayerSize.Small:
                isOperable = _operableBySmall;
                break;
            case PlayerSize.Normal:
                isOperable = _operableByNormal;
                break;
        }

        Color colorToSet = isOperable ? _functional : _inoperable;

        foreach (var material in _materials)
        {
            material.SetColor(_color, colorToSet);
        }
    }
    
    private void OnEnable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(OnPlayerSizeChanged);
    private void OnDisable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(OnPlayerSizeChanged);

}

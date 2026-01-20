using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine;
using static PlayerEnum; // Importante para que reconozca PlayerSize

/// <summary>
/// Gestiona el feedback visual (outline) de un objeto interactuable.
/// Se enciende/apaga desde PlayerInteractionManager y actualiza su
/// color (funcional/inoperable) escuchando el GameEvent OnSizeChanged.
/// </summary>
public class Interactable : MonoBehaviour
{
    [Header("Configuración de Operable (SIMPLE)")]
    // --- SIMPLIFICADO ---
    // Reemplazamos la lista de Mappings por 3 booleanos.
    // Es más fácil de configurar en el Inspector.
    [SerializeField] private bool _operableByHead = false;
    [SerializeField] private bool _operableBySmall = false;
    [SerializeField] private bool _operableByNormal = false;
    // --- FIN SIMPLIFICADO ---

    [Header("Configuración de Material")]
    [SerializeField] [ColorUsage(true, true)]
    private Color _functional = new Color(0.5f, 0, 1, 1);

    [SerializeField] [ColorUsage(true, true)]
    private Color _inoperable = new Color(1, 0, 0, 1);

    private const string _inRange = "_IsInRange";
    private const string _color = "_Outline_Color";
    private const string _materialNameToFind = "InteractableOutline_Ma";
    private const string _materialNameToFindForBox = "InteractableOutline_Ma_Box";

    private List<Material> _materials = new List<Material>();
    
    private PlayerSize _currentReportedPlayerSize;
    private bool _isOutlineOn = false;

    private void Start()
    {
        // 1. Encontrar todos los materiales de outline en los hijos
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
    
    private void OnEnable()
    {
        // Nos suscribimos al GameEvent global
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(OnPlayerSizeChanged);
        }
    }

    private void OnDisable()
    {
        // Nos desuscribimos
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(OnPlayerSizeChanged);
        }
    }

    /// <summary>
    /// Llamado por el GameEvent 'OnSizeChanged'
    /// </summary>
    private void OnPlayerSizeChanged(PlayerSize newSize)
    {
        _currentReportedPlayerSize = newSize;
        
        // Si el outline está encendido, actualiza su color inmediatamente
        if (_isOutlineOn)
        {
            SetColor();
        }
    }
    
    // --- API Pública (para PlayerInteractionManager) ---

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

    /// <summary>
    /// Ajusta el color del outline basado en el tamaño actual.
    /// </summary>
    private void SetColor()
    {
        if (_materials.Count == 0) return;

        // --- LÓGICA SIMPLIFICADA ---
        // Usamos un switch en lugar de un bucle foreach
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
        // --- FIN LÓGICA SIMPLIFICADA ---

        Color colorToSet = isOperable ? _functional : _inoperable;

        foreach (var material in _materials)
        {
            material.SetColor(_color, colorToSet);
        }
    }
}

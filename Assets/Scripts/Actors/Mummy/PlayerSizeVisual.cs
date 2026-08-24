using UnityEngine;
using static PlayerEnum;

/// <summary> 
/// Conmutador de Mallas: Escucha los cambios de tamaño del modelo para activar o desactivar 
/// los GameObjects visuales correspondientes (Normal, Small, Head). 
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerSizeVisual : MonoBehaviour
{
    [Header("Mesh Roots (activar uno a la vez)")] [SerializeField]
    private GameObject _meshNormal;

    [SerializeField] private GameObject _meshSmall;
    [SerializeField] private GameObject _meshHead;
    [SerializeField] private GameObject _meshEmpowered;

    private PlayerModel _model;

    public void Bind(PlayerModel model)
    {
        _model = model;
        Apply(_model.Size);
    }

    private void Apply(PlayerSize size)
    {
        if (_meshNormal) _meshNormal.SetActive(size == PlayerSize.Normal);
        if (_meshSmall) _meshSmall.SetActive(size == PlayerSize.Small);
        if (_meshHead) _meshHead.SetActive(size == PlayerSize.Head);
        if (_meshEmpowered) _meshEmpowered.SetActive(size == PlayerSize.Empowered);
    }

    public void MeshTurn(bool show)
    {
        _meshHead.SetActive(show);
    }

    #region Unity Editor

// #if UNITY_EDITOR
//     private void OnValidate()
//     {
//         // En editor, si hay referencias, asegura que nunca haya más de una activa cuando se cambien a mano.
//         int activeCount = 0;
//         if (_meshNormal && _meshNormal.activeSelf) activeCount++;
//         if (_meshSmall && _meshSmall.activeSelf) activeCount++;
//         if (_meshHead && _meshHead.activeSelf) activeCount++;
//         if (activeCount > 1)
//         {
//             // Por simplicidad: dejamos sólo Normal activa si hay conflicto visual en editor.
//             if (_meshNormal)
//             {
//                 _meshNormal.SetActive(true);
//             }
//
//             if (_meshSmall)
//             {
//                 _meshSmall.SetActive(false);
//             }
//
//             if (_meshHead)
//             {
//                 _meshHead.SetActive(false);
//             }
//         }
//     }
// #endif

    #endregion

    private void OnEnable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(Apply);
    

    private void OnDisable() => GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(Apply);
    
    
}
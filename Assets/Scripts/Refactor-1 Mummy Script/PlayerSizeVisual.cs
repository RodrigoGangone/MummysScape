using UnityEngine;
using static PlayerEnum;

/// <summary>
/// PlayerSizeVisual
/// Responsabilidad: conmutar entre 3 mallas (Normal/Small/Head) según PlayerModel.Size.
/// Se suscribe a OnSizeChanged y garantiza 1 sola malla activa.
/// No conoce reglas de juego ni física: solo visual.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerSizeVisual : MonoBehaviour
{
    [Header("Mesh Roots (activar uno a la vez)")]
    [SerializeField] private GameObject _meshNormal;
    [SerializeField] private GameObject _meshSmall;
    [SerializeField] private GameObject _meshHead;

    private PlayerModel _model;

    public void Bind(PlayerModel model)
    {
        if (_model != null) _model.OnSizeChanged -= Apply;
        _model = model;
        Apply(_model.Size);
        _model.OnSizeChanged += Apply;
    }

    private void OnDestroy()
    {
        if (_model != null) _model.OnSizeChanged -= Apply;
    }

    private void Apply(PlayerSize size)
    {
        if (_meshNormal) _meshNormal.SetActive(size == PlayerSize.Normal);
        if (_meshSmall)  _meshSmall .SetActive(size == PlayerSize.Small);
        if (_meshHead)   _meshHead  .SetActive(size == PlayerSize.Head);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // En editor, si hay referencias, asegura que nunca haya más de una activa cuando se cambien a mano.
        int activeCount = 0;
        if (_meshNormal && _meshNormal.activeSelf) activeCount++;
        if (_meshSmall  && _meshSmall.activeSelf)  activeCount++;
        if (_meshHead   && _meshHead.activeSelf)   activeCount++;
        if (activeCount > 1)
        {
            // Por simplicidad: dejamos sólo Normal activa si hay conflicto visual en editor.
            if (_meshNormal) { _meshNormal.SetActive(true); }
            if (_meshSmall)  { _meshSmall .SetActive(false); }
            if (_meshHead)   { _meshHead  .SetActive(false); }
        }
    }
#endif
}
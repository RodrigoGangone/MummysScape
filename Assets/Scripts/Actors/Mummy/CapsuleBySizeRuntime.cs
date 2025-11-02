using System;
using UnityEngine;
using static PlayerEnum;

/// <summary>
/// CapsuleBySizeRuntime
/// Responsabilidad: aplicar presets de CapsuleCollider (center, radius, height) según PlayerModel.Size.
/// Se suscribe a OnSizeChanged del model. No conoce visual/UI.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class CapsuleBySizeRuntime : MonoBehaviour
{
    [Serializable]
    public struct CapsulePreset
    {
        public Vector3 center;
        public float radius;
        public float height;
    }

    [Header("Presets por tamaño")]
    [SerializeField] private CapsulePreset _normal = new() { center = new(0f, 1f, 0f), radius = 0.5f, height = 2f };
    [SerializeField] private CapsulePreset _small  = new() { center = new(0f, 0.62f,0f), radius = 0.5f, height = 1.25f };
    [SerializeField] private CapsulePreset _head   = new() { center = new(0f, 0.5f,  0f), radius = 0.5f, height = 1f };

    private CapsuleCollider _capsule;
    private PlayerModel _model;

    public void Bind(PlayerModel model)
    {
        if (_capsule == null) _capsule = GetComponent<CapsuleCollider>();
        _model = model;
        Apply(_model.Size);
    }

    private void Apply(PlayerSize size)
    {
        var p = size switch
        {
            PlayerSize.Small => _small,
            PlayerSize.Head  => _head,
            _                => _normal
        };

        _capsule.center = p.center;
        _capsule.radius = p.radius;
        _capsule.height = p.height;
        _capsule.direction = 1; // Y (por claridad; es el default)
    }
    
    private void OnEnable()
    {
        GameEventManager.Instance.playerEvents.OnSizeChanged
            .Register<PlayerSize>(Apply);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.playerEvents.OnSizeChanged
            .Unregister<PlayerSize>(Apply);
    }
}
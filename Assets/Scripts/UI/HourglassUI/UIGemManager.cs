using UnityEngine;
using static Save;

/// <summary> 
/// Monitor de Progreso: Sincroniza los materiales de la interfaz de gemas con los datos de 
/// persistencia, actualizando el estado de "recogido" en tiempo real durante la partida. 
/// </summary>

public class UIGemManager : MonoBehaviour
{
    [SerializeField] private Material[] _gemMaterials;
    [SerializeField] private Transform[] _gemTargets;
    [SerializeField] private ParticleSystem _arrivalVfxPrefab;
    
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");


    private void Start()
    {
        if (_gemMaterials == null)
            return;

        for (int i = 0; i < _gemMaterials.Length; i++)
        {
            int gemNum = i + 1;
            SetGemUI(gemNum, WasGemPicked(gemNum));
        }
    }

    private void OnGemReachedUI(int gemNum)
    {
        SetGemUI(gemNum, true);
        PlayArrivalVfx(gemNum);
    }

    private void SetGemUI(int gemNum, bool picked)
    {
        var mat = GetMaterialByGemNum(gemNum);
        if (!mat) return;  

        if (mat.HasProperty(IsPickedProp))
        {
            mat.SetFloat(IsPickedProp, picked ? 1 : 0);
        }
    }

    private Material GetMaterialByGemNum(int gemNum)
    {
        if (gemNum < 1 || gemNum > _gemMaterials.Length) return null;
        return _gemMaterials[gemNum - 1];
    }

    private void PlayArrivalVfx(int gemNum)
    {
        if (_arrivalVfxPrefab == null)
            return;

        int index = gemNum - 1;
        if (_gemTargets == null || index < 0 || index >= _gemTargets.Length || _gemTargets[index] == null)
            return;

        Transform target = _gemTargets[index];
        ParticleSystem instance = Instantiate(_arrivalVfxPrefab, target);
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        SetLayerRecursively(instance.gameObject, target.gameObject.layer);
        instance.Play(true);

        ParticleSystem.MainModule main = instance.main;
        float lifetime = main.duration + main.startLifetime.constantMax;
        Destroy(instance.gameObject, Mathf.Max(0.1f, lifetime));
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnGemReachedUI.Register<int>(OnGemReachedUI);

    private void OnDisable()
    {
        if (GameEventManager.Instance != null)
            GameEventManager.Instance.levelEvents.OnGemReachedUI.Unregister<int>(OnGemReachedUI);
    }
}

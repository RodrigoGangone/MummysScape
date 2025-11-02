using UnityEngine;
using UnityEngine.Serialization;
using static Save; // Asumes que Save.cs está disponible

public class UIGemManager : MonoBehaviour
{
    [SerializeField] private Material[] _gemMaterials;
    
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");


    private void Start()
    {
        // El bucle ahora itera sobre los materiales que guardamos
        for (int i = 0; i < _gemMaterials.Length; i++)
        {
            // El número de gema es i + 1
            int gemNum = i + 1;
            SetGemUI(gemNum, WasGemPicked(gemNum));
        }
    }

    private void OnGemPicked(int gemNum) => SetGemUI(gemNum, true);

    private void SetGemUI(int gemNum, bool picked)
    {
        // Obtenemos el material cacheado
        var mat = GetMaterialByGemNum(gemNum);
        if (!mat) return; // Si no hay material, salimos

        if (mat.HasProperty(IsPickedProp))
        {
            // --- LÓGICA CORREGIDA ---
            // Si 'picked' es true, usa 1. Si es false, usa 0.
            mat.SetFloat(IsPickedProp, picked ? 1 : 0);
        }
    }

    // Helper modificado para obtener el material
    private Material GetMaterialByGemNum(int gemNum)
    {
        if (gemNum < 1 || gemNum > _gemMaterials.Length) return null;
        return _gemMaterials[gemNum - 1];
    }

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPickedGem.Register<int>(OnGemPicked);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPickedGem.Unregister<int>(OnGemPicked);
}
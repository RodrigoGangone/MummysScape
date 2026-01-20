using UnityEngine;
using static Save;

public class UIGemManager : MonoBehaviour
{
    [SerializeField] private Material[] _gemMaterials;
    
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");


    private void Start()
    {
        for (int i = 0; i < _gemMaterials.Length; i++)
        {
            int gemNum = i + 1;
            SetGemUI(gemNum, WasGemPicked(gemNum));
        }
    }

    private void OnGemPicked(int gemNum) => SetGemUI(gemNum, true);

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

    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPickedGem.Register<int>(OnGemPicked);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPickedGem.Unregister<int>(OnGemPicked);
}
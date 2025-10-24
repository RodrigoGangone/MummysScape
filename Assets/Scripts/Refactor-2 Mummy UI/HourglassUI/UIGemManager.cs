using UnityEngine;

public class UIGemManager : MonoBehaviour
{
    [SerializeField] private Renderer Gem01;
    [SerializeField] private Renderer Gem02;
    [SerializeField] private Renderer Gem03;

    private Renderer[] _gems;
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");

    private void Awake()
    {
        _gems = new[] { Gem01, Gem02, Gem03 };
    }

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPickedGem.Register<int>(OnGemPicked);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPickedGem.Unregister<int>(OnGemPicked);
    }

    private void Start()
    {
        for (int i = 1; i <= _gems.Length; i++)
        {
            bool picked = Save.WasGemPicked(i);
            SetGemUI(i, picked);
        }
    }

    void OnGemPicked(int gemNum) => SetGemUI(gemNum, true); // en UI al tomar: 0
    
    private void SetGemUI(int gemNum, bool picked)
    {
        var r = GetRendererByGemNum(gemNum);
        if (!r) return;

        var mat = r.material;
        if (mat.HasProperty(IsPickedProp))
        {
            // UI: no tomada => 1 ; tomada => 0   (INVERTIDO respecto a escena)
            mat.SetFloat(IsPickedProp, picked ? 1 : 0);
        }
    }

    private Renderer GetRendererByGemNum(int gemNum)
    {
        if (gemNum < 1 || gemNum > _gems.Length) return null;
        return _gems[gemNum - 1];
    }
}
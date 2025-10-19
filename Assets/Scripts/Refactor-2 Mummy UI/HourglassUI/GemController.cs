using UnityEngine;
using UnityEngine.SceneManagement;

public class GemController : MonoBehaviour
{
    [SerializeField] private PlayerPrefsRegistry gemRegistry;

    private void Awake()
    {
        if (gemRegistry != null)
            PlayerPrefsManager.BindRegistry(gemRegistry);
    }

    private void OnEnable()
    {
        // Nos suscribimos tipado a int
        GameEventManager.Instance.levelEvents.OnPickedGem.Register<int>(OnGemPicked);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPickedGem.Unregister<int>(OnGemPicked);
    }

    private void OnGemPicked(int gemNum)
    {
        // ... tu switch/Debug ...
        string key = MakeGemKey(gemNum);

        // Guardar como booleano
        PlayerPrefsManager.Set(key, gemNum);

        // total por escena (opcional)
        string totalKey = MakeSceneTotalKey();
        int total = PlayerPrefsManager.Get(totalKey, 0);
        PlayerPrefsManager.Set(totalKey, total + 1);
    }


    public static string MakeGemKey(int gemNum)
    {
        string scene = SceneManager.GetActiveScene().name;
        return $"gem.{gemNum}.{scene}";
    }

    private static string MakeSceneTotalKey()
    {
        string scene = SceneManager.GetActiveScene().name;
        return $"gemTotal.{scene}";
    }
}
using UnityEngine;

// Este script se encarga de inicializar sistemas globales
public class RegisterData : MonoBehaviour
{
    [Header("Registries de Debug")] [SerializeField]
    private PlayerPrefsRegistry[] registriesToBind;

    void Awake()
    {
        // Bindea todos los registries asignados en el inspector
        // para que escuchen los cambios del PlayerPrefsManager.
        PlayerPrefsManager.BindRegistries(registriesToBind);

        // Opcional: Asegura que este objeto no se destruya
        // si lo pones en una escena que carga otras.
        // DontDestroyOnLoad(gameObject);
    }
}
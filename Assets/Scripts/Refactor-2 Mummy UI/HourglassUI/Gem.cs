using System;
using UnityEngine;
// 'using static Save;' ya no es necesario aquí
// si prefieres, puedes mantenerlo y llamar a 'MarkGemPicked(gemNum)'
// en lugar de 'Save.MarkGemPicked(gemNum)'

public class Gem : MonoBehaviour
{
    [SerializeField] private int gemNum;
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");

    private Renderer Renderer => GetComponentInChildren<Renderer>();
    
    private void Start()
    {
        // Comprueba si ya fue recogida (usando el sistema de guardado)
        // y actualiza el shader
        if (Renderer && Renderer.material.HasProperty(IsPickedProp))
        {
            bool alreadyPicked = Save.WasGemPicked(gemNum);
            Renderer.material.SetFloat(IsPickedProp, alreadyPicked ? 0 : 1f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        // --- LÓGICA CORREGIDA ---
        
        // 1. Llama al guardado DIRECTAMENTE.
        //    (Usa el gemNum de ESTE script)
        Save.MarkGemPicked(gemNum);

        // 2. Notifica a otros sistemas (como la UI) que la gema fue recogida.
        GameEventManager.Instance.levelEvents.OnPickedGem.Raise(gemNum);
        
        // 3. Desactiva este objeto gema.
        gameObject.SetActive(false);
    }
    
    // --- ELIMINADO ---
    // Ya no necesitamos que la gema se suscriba a su propio evento.
    // private void OnEnable() => ...
    // private void OnDisable() => ...
}
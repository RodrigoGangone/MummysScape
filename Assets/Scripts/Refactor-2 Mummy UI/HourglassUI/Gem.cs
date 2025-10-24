using UnityEngine;

public class Gem : MonoBehaviour
{
    [SerializeField] private int gemNum;
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");
    private bool _alreadyPicked;

    private void Start()
    {
        bool picked = Save.WasGemPicked(gemNum);
        
        var rend = GetComponentInChildren<Renderer>();

        if (rend && rend.material.HasProperty(IsPickedProp))
            rend.material.SetFloat(IsPickedProp, picked ? 0 : 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_alreadyPicked) return; // guard contra múltiples colisiones
        if (!other.CompareTag("PlayerFather")) return;

        _alreadyPicked = true;
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // Feedback visual en escena: al tomar => 1
        var rend = GetComponentInChildren<Renderer>();
        if (rend && rend.material.HasProperty(IsPickedProp))
            rend.material.SetFloat(IsPickedProp, 1f);

        // Emitir el evento UNA VEZ
        GameEventManager.Instance.levelEvents.OnPickedGem.Raise(gemNum);

        // Si querés ocultarla físicamente:
        gameObject.SetActive(false);
    }
}
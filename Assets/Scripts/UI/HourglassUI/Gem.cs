using UnityEngine;

/// <summary> 
/// Lógica de Coleccionable: Gestiona la recolección de gemas, sincronizando su estado visual con 
/// el sistema de guardado y disparando eventos globales de recolección. 
/// </summary>

public class Gem : MonoBehaviour
{
    [SerializeField] private int gemNum;
    [SerializeField] private GameObject fxGemPick;
    [SerializeField] private FxBank gemBank;
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");

    private Renderer Renderer => GetComponentInChildren<Renderer>();
    
    private void Start()
    {
        if (Renderer && Renderer.material.HasProperty(IsPickedProp))
        {
            bool alreadyPicked = Save.WasGemPicked(gemNum);
            Renderer.material.SetFloat(IsPickedProp, alreadyPicked ? 0 : 1f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        gemBank.Play3D("Pick", transform.position);
        
        Save.MarkGemPicked(gemNum);

        GameEventManager.Instance.levelEvents.OnPickedGem.Raise(gemNum);

        Instantiate(fxGemPick, transform.position, Quaternion.identity, null);
        
        gameObject.SetActive(false);
    }
}
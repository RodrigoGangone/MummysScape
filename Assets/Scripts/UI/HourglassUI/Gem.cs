using UnityEngine;
using static SfxIDs;
using static Tags;

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
        bool alreadyPicked = Save.WasGemPicked(gemNum);
        if (alreadyPicked)
        {
            gameObject.SetActive(false);
            return;
        }

        if (Renderer && Renderer.material.HasProperty(IsPickedProp))
        {
            Renderer.material.SetFloat(IsPickedProp, 1f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;

        Vector3 pickupPosition = transform.position;
        if (!Save.TryMarkGemPicked(gemNum))
        {
            gameObject.SetActive(false);
            return;
        }

        gemBank.Play3D(SfxIDs.Gem.Pick, pickupPosition);

        GameEventManager.Instance.levelEvents.OnPickedGem.Raise(new GemPickupData(gemNum, pickupPosition));
        GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(0.5f, 0.25f);

        Instantiate(fxGemPick, pickupPosition, Quaternion.identity, null);
        
        gameObject.SetActive(false);
    }
}

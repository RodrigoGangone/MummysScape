using UnityEngine;

public class Gem : MonoBehaviour
{
    [SerializeField] private int gemNum;
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

        Save.MarkGemPicked(gemNum);

        GameEventManager.Instance.levelEvents.OnPickedGem.Raise(gemNum);
        
        gameObject.SetActive(false);
    }
}
using System.Collections;
using UnityEngine;
using static PauseUtils;

public class Bandage : MonoBehaviour, IPausable
{
    [Header("Visuals")]
    [SerializeField] private Renderer meshRenderer;

    [Header("Settings")]
    [SerializeField] private float defaultDelay = 2f;
    
    private Collider _collider;
    private bool _paused;
    private Material _instancedMaterial;
    private Breakable _sourceJar;
    
    private const int AMOUNT = 1;
    private static readonly int IsActive = Shader.PropertyToID("_isActive");

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (meshRenderer != null) _instancedMaterial = meshRenderer.material;
    }

    public void SetupPickupDelay(float duration, Breakable source = null)
    {
        _sourceJar = source;
        if (_collider != null) _collider.enabled = false;
        if (_instancedMaterial != null) _instancedMaterial.SetFloat(IsActive, 0);

        StopAllCoroutines(); 
        StartCoroutine(EnableColliderRoutine(duration));
    }

    private IEnumerator EnableColliderRoutine(float duration)
    {
        yield return WaitForSecondsPausable(duration, () => _paused);
        if (_collider != null) _collider.enabled = true;
        if (_instancedMaterial != null) _instancedMaterial.SetFloat(IsActive, 1);
    }

    private void OnTriggerStay (Collider collision)
    {
        if (!collision.gameObject.CompareTag("PlayerFather")) return;
        var ctrl = collision.gameObject.GetComponentInParent<PlayerController>();
        if (ctrl != null && ctrl.TryCollectBandage(AMOUNT)) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_sourceJar != null) _sourceJar.NotifyItemPickedUp();
    }

    public void OnPauseChanged(bool paused) => _paused = paused;
}
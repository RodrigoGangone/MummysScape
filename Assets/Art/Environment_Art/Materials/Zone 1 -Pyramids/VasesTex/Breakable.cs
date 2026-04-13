using UnityEngine;
using System.Collections;

public class Breakable : MonoBehaviour
{
    [Header("Settings")] [SerializeField] private GameObject destroyedVersion;

    [Header("Drop Settings")] [SerializeField]
    private GameObject drop;

    [SerializeField] private bool withDrop;
    [SerializeField] private float dropActivationDelay;

    [Header("References")] [SerializeField]
    private FxBank bank;

    private Collider _collider;
    private Renderer _renderer;
    private bool _isBroken = false;
    private GameObject _currentFractured;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _renderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isBroken) return;
        if (!other.gameObject.CompareTag("Bullet") && !other.gameObject.CompareTag("PlayerFather")) return;

        Break();
    }

    private void Break()
    {
        _isBroken = true;
        _currentFractured = Instantiate(destroyedVersion, transform.position, transform.rotation);

        // Aseguramos que la escala sea idéntica
        _currentFractured.transform.localScale = transform.localScale;

        if (withDrop && drop != null)
        {
            var spawnedDrop = Instantiate(drop, transform.position, transform.rotation);
            GameEventManager.Instance.levelEvents.OnRequestBandageSpawn.Raise(transform.position);

            if (spawnedDrop.TryGetComponent<Bandage>(out var bandage))
            {
                bandage.SetupPickupDelay(dropActivationDelay, this);
            }
        }

        bank.Play3D("Break", transform.position);
        GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(0.5f, 0.25f);
        _renderer.enabled = false;
        _collider.enabled = false;
    }

    public void NotifyItemPickedUp()
    {
        if (gameObject.activeInHierarchy) StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // Pequeño delay tras recoger el objeto para que no sea instantáneo
        yield return new WaitForSeconds(0.3f);

        if (_currentFractured != null && _currentFractured.TryGetComponent<ReassemblePieces>(out var reassemble))
        {
            float wait = reassemble.WaitBeforeFlyBack;
            float duration = reassemble.AssembleDuration;

            reassemble.StartReassembling();

            // 1. Esperamos el tiempo que están las piezas en el suelo
            yield return new WaitForSeconds(wait);

            // 2. Esperamos exactamente el 90% de la duración del vuelo
            yield return new WaitForSeconds(duration * 0.9f);

            if (_currentFractured != null) Destroy(_currentFractured);

            // 3. HABILITAMOS EL JARRÓN ARMADO
            _renderer.enabled = true;
            _collider.enabled = true;

            // 4. Esperamos el 10% restante para limpiar las piezas
            yield return new WaitForSeconds(duration * 0.1f);
        }
        else
        {
            // Fail-safe
            _renderer.enabled = true;
            _collider.enabled = true;
        }

        _isBroken = false;
    }
}
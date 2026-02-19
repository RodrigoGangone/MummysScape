using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private GameObject destroyedVersion;
    [SerializeField] private GameObject drop;
    [SerializeField] private bool withDrop;
    [SerializeField] private float dropActivationDelay; // <--- Nuevo: Tiempo específico para el jarrón
    [SerializeField] private FxBank bank;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Bullet") &&
            !other.gameObject.CompareTag("PlayerFather")) return;

        Instantiate(destroyedVersion, transform.position, transform.rotation);

        if (withDrop && drop != null)
        {
            var spawnedDrop = Instantiate(drop, transform.position, transform.rotation);
           
            // Intentamos obtener el componente Bandage y le pasamos el nuevo tiempo
            if(spawnedDrop.TryGetComponent<Bandage>(out var bandage))
            {
                bandage.SetupPickupDelay(dropActivationDelay);
            }
        }

        bank.Play3D("Break", transform.position);
        Destroy(gameObject);
    }
}
using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private GameObject destroyedVersion;
    [SerializeField] private GameObject drop;

    [SerializeField] private bool withDrop;

    [SerializeField] private FxBank bank;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Bullet") &&
            !other.gameObject.CompareTag("PlayerFather")) return;

        //AudioManager.Instance.PlaySFX(NameSounds.SFX_BreakJar);

        Instantiate(destroyedVersion, transform.position, transform.rotation);

        if (withDrop)
            Instantiate(drop, transform.position, transform.rotation);

        bank.Play3D("Break", transform.position);

        Destroy(gameObject);
    }
}
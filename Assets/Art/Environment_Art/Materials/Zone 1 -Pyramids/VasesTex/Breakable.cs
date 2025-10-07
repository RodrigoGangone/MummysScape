using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private GameObject _destroyeVersion;
    [SerializeField] private GameObject _drop;
    [SerializeField] private bool _droped;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet") || other.gameObject.CompareTag("PlayerFather"))
        {
            AudioManager.Instance.PlaySFX(NameSounds.SFX_BreakJar);
            
            Instantiate(_destroyeVersion, transform.position, transform.rotation);

            if (_droped)
                Instantiate(_drop, transform.position, transform.rotation);

            Destroy(gameObject);
        }
    }
}
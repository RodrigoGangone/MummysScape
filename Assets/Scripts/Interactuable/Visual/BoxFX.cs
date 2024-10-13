using UnityEngine;

public class BoxFX : MonoBehaviour
{
    [SerializeField] private ParticleSystem sandMoundParticle;
    //[SerializeField] private GameObject sandMound;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Sand"))
        {
            Debug.Log("Sand");
            if (!sandMoundParticle.isPlaying)
            {
                sandMoundParticle.Play();
            }
        }
    }
}

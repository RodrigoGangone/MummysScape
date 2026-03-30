using UnityEngine;
using static Tags;
using static Layers;

public class WaterSplash : MonoBehaviour
{
    [SerializeField] private ParticleSystem _waterSplashFX;

    private PlayerController _player;
    private readonly Vector3 _offset = new(0, -0.5f, 0);

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(BOX_LAYER) ||
            other.gameObject.layer == LayerMask.NameToLayer(PLAYER_LAYER) ||
            other.gameObject.layer == LayerMask.NameToLayer(BANDAGE_MOUND_LAYER))
        {
            _waterSplashFX.transform.position = other.transform.position + _offset;

            _waterSplashFX.Play();
        }

        if (other.gameObject.layer == LayerMask.NameToLayer(BANDAGE_MOUND_LAYER))
            other.gameObject.SetActive(false);

        if (other.gameObject.CompareTag(PLAYER_TAG))
        {
            _player = other.GetComponent<PlayerController>();

            _player.Ctx.Model.TryConsumeBandage(_player.Ctx.Model.Bandages);
        }
    }
}
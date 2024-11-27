using UnityEngine;
using static Utils;

public class Collectible : MonoBehaviour
{
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private GameObject _congratsParticles;

    [SerializeField] private CollectibleNumber _collectibleNumber;

    public CollectibleNumber CollectibleNumber => _collectibleNumber;

    private AudioSource collectableProximityAudio;


    private void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();

        _levelManager.AddCollectible += AddCollectFX;

        collectableProximityAudio = AudioManager.Instance.GetClipByName(NameSounds.SFX_CollectableProximity);
        collectableProximityAudio.Play();
        StartCoroutine(AudioManager.Instance.FollowTransform(collectableProximityAudio, transform, -1));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;
        collectableProximityAudio.Pause();
        AudioManager.Instance.ReturnAudioSourceToPool(collectableProximityAudio, 0.1f);
        AudioManager.Instance.PlaySFX(NameSounds.SFX_Collectable);
        _levelManager.AddCollectible.Invoke(_collectibleNumber);
    }

    private void AddCollectFX(CollectibleNumber number)
    {
        if (number != _collectibleNumber) return;

        Instantiate(_congratsParticles, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
    }
}

public enum CollectibleNumber
{
    One,
    Two,
    Three
}
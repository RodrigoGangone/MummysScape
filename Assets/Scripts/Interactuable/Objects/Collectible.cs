using UnityEngine;
using static Utils;

public class Collectible : MonoBehaviour
{
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private GameObject _congratsParticles;

    [SerializeField] private CollectibleNumber _collectibleNumber;

    public CollectibleNumber CollectibleNumber => _collectibleNumber;


    private void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();

        _levelManager.AddCollectible += AddCollectFX;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;
        
        AudioManager.Instance.PlaySFX(NameSounds.Collectable);
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
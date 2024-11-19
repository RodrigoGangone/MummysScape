using System.Collections;
using UnityEngine;

public class StoneScorpionTrigger : MonoBehaviour
{
    private Scorpion _scorpion;
    private ParticleSystem _hitFx;

    private void Start()
    {
        _scorpion = GetComponentInParent<Scorpion>();
        _hitFx = GetComponentInChildren<ParticleSystem>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall") ||
            other.gameObject.layer == LayerMask.NameToLayer("Box") ||
            other.gameObject.layer == LayerMask.NameToLayer("Floor"))
        {
            CollisionWithAll();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            HandleCollisionWithPlayer();
        }
    }

    private void CollisionWithAll()
    {
        _scorpion._stoneView.SetActive(false);
        _scorpion._stonePrefab.transform.position = _scorpion._targetShoot.position;
        _hitFx.Play();
    }

    private void HandleCollisionWithPlayer()
    {
        CollisionWithAll(); // Reutiliza la lógica común

        Vector3 targetPosition = new Vector3(
            _scorpion.player.transform.position.x,
            _scorpion.viewScorpion.transform.position.y,
            _scorpion.player.transform.position.z
        );
        Vector3 direction = (targetPosition - _scorpion.viewScorpion.transform.position).normalized;

        _scorpion.player._modelPlayer.IsDamage = true;
        StartCoroutine(_scorpion.MovePlayer(direction));
    }
}
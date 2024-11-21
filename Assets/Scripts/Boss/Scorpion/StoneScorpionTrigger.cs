using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class StoneScorpionTrigger : MonoBehaviour
{
    private Scorpion _scorpion;
    [SerializeField] private ParticleSystem _hitFx;

    private void Start()
    {
        _scorpion = GetComponentInParent<Scorpion>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall") ||
            other.gameObject.layer == LayerMask.NameToLayer("Box") ||
            other.gameObject.layer == LayerMask.NameToLayer("Floor"))
        {
            CollisionWithAll(other.transform.position);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            HandleCollisionWithPlayer(other.transform.position);
        }
    }

    private void CollisionWithAll(Vector3 pos)
    {
        Instantiate(_hitFx, pos, quaternion.identity);

        _scorpion._stoneView.SetActive(false);
        _scorpion._stonePrefab.transform.position = _scorpion._targetShoot.position;
    }


    private void HandleCollisionWithPlayer(Vector3 pos)
    {
        CollisionWithAll(pos); // Reutiliza la lógica común

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
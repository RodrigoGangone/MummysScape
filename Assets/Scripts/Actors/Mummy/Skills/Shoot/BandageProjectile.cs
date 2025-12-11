using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PauseUtils;

[RequireComponent(typeof(Rigidbody))]
public class BandageProjectile : MonoBehaviour, IPausable
{
    [Header("Settings")]
    [SerializeField] private LayerMask collisionLayers; // Selecciona aquí las layers con las que choca (ej: Default, Wall, Enemy)
    
    [Header("References")]
    [SerializeField] private GameObject drop;
    [SerializeField] private GameObject view;

    private Rigidbody _rb;
    private bool _paused;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
    }

    public void Play(IReadOnlyList<Vector3> path, float speed) => StartCoroutine(RunPhysics(path, speed));

    private IEnumerator RunPhysics(IReadOnlyList<Vector3> path, float speed)
    {
        foreach (var target in path)
        {
            while (Vector3.Distance(_rb.position, target) > 0.1f)
            {
                if (_paused)
                {
                    yield return WaitWhilePaused(() => _paused);
                    continue;
                }

                var newPosition = Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime);
                _rb.MovePosition(newPosition);

                var dir = target - _rb.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    var targetRot = Quaternion.LookRotation(dir.normalized);
                    var newRot = Quaternion.Slerp(_rb.rotation, targetRot, Time.fixedDeltaTime);
                    _rb.MoveRotation(newRot);
                }

                yield return new WaitForFixedUpdate();
            }
        }

        _rb.useGravity = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Operación bitwise para verificar si la layer del objeto está dentro de la máscara seleccionada
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            Instantiate(drop, transform.position, Quaternion.identity);
        
            Destroy(view);
            GetComponent<Collider>().enabled = false;
        }
    }

    public void OnPauseChanged(bool paused) => _paused = paused;
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}

public static class SimpleShootData
{
    public static List<Vector3> Path;
}
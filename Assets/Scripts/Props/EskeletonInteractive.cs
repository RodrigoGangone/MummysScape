using System.Collections;
using UnityEngine;
using static Tags;

public class EskeletonInteractive : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Punto donde aparece/se teletransporta inicialmente")]
    [SerializeField]
    private Transform _initialTarget;

    [Tooltip("Punto final hacia donde camina")] [SerializeField]
    private Transform _finalTarget;

    [SerializeField] private float _moveSpeed = 2f;
    
    [SerializeField] private ParticleSystem vfx01;
    [SerializeField] private ParticleSystem vfx02;
    [SerializeField] private ParticleSystem vfx03;

    private Animator _animator;
    private BoxCollider _collider;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<BoxCollider>();
    }

    private void Activate()
    {
        _collider.enabled = false;
        
        _animator.SetBool("Entry", true);
    }

    public void WalkToObjetive()
    {
        _animator.SetBool("Entry", false);
        _animator.SetBool("Walk", true);

        if (_finalTarget != null)
            StartCoroutine(MoveToTargetCoroutine());
    }

    private IEnumerator MoveToTargetCoroutine()
    {
        transform.position = _initialTarget.position;
        
        Vector3 lookAtPoint = new Vector3(_finalTarget.position.x, transform.position.y, _finalTarget.position.z);

        if (lookAtPoint != transform.position)
        {
            transform.LookAt(lookAtPoint);
        }

        while (Vector3.Distance(transform.position, _finalTarget.position) > 0.01f)
        {
            transform.position =
                Vector3.MoveTowards(transform.position, _finalTarget.position, _moveSpeed * Time.deltaTime);

            lookAtPoint = new Vector3(_finalTarget.position.x, transform.position.y, _finalTarget.position.z);
            if (lookAtPoint != transform.position)
            {
                transform.LookAt(lookAtPoint);
            }

            yield return null;
        }

        transform.position = _finalTarget.position;
        
        gameObject.SetActive(false);
    }

    public void ExecuteFx01() => vfx01.Play();
    public void ExecuteFx02() => vfx02.Play();
    public void ExecuteFx03() => vfx03.Play();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PLAYER_TAG))
            Activate();
    }
}
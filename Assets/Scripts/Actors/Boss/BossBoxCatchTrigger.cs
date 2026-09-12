using UnityEngine;
using System.Collections;
using static Tags;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class BossBoxCatchTrigger : MonoBehaviour
{
    [SerializeField] private BossActor bossActor;
    [SerializeField] private Transform socket;
    [SerializeField] private GameObject fakeBox;
    [SerializeField] private ParticleSystem impactFirstBox;

    [Header("Collider Offsets")]
    [Tooltip("El 'Center' local del collider esperando el 1er hit")]
    [SerializeField] private Vector3 firstHitOffset;
    
    [Tooltip("El 'Center' local del collider esperando el 2do hit de la CAJA")]
    [SerializeField] private Vector3 secondHitOffset;
    
    [Tooltip("El 'Center' local del collider esperando el 2do hit del PLAYER")]
    [SerializeField] private Vector3 playerHitOffset;

    [Header("Catch Settings")] 
    [SerializeField] private float catchDuration = 0.5f; 
    [SerializeField] private AnimationCurve catchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private HashSet<GameObject> _caughtBoxes = new();
    
    private Collider _triggerCollider; // El collider original (se usa para Caja 1 y Caja 2)
    private Collider _playerTriggerCollider; // El collider dinámico (se usa solo para Player)

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        SetColliderCenter(firstHitOffset);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ESTADO 1: Esperando la primera caja
        if (_caughtBoxes.Count == 0)
        {
            if (other.CompareTag(HEAVY_BOX_TAG))
            {
                GameObject box = other.gameObject;
                if (_caughtBoxes.Add(box))
                {
                    HandleFirstBoxCatch(box);
                }
            }
        }
        // ESTADO 2: Esperando el segundo impacto (Caja o Player)
        else if (_caughtBoxes.Count == 1)
        {
            if (other.CompareTag(HEAVY_BOX_TAG))
            {
                GameObject box = other.gameObject;
                if (_caughtBoxes.Add(box))
                {
                    DisableAllColliders();
                    HandleSecondBoxImpact(box);
                }
            }
            else if (other.CompareTag(PLAYER_TAG))
            {
                DisableAllColliders();
                HandlePlayerSecondImpact();
            }
        }
    }

    private void HandleFirstBoxCatch(GameObject box)
    {
        if (box.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        bossActor.NotifyPreDie();
        
        // 1. Movemos el collider original para esperar la SEGUNDA CAJA
        SetColliderCenter(secondHitOffset);
        
        // 2. Creamos un nuevo collider específicamente para el PLAYER
        CreatePlayerCollider();
        
        impactFirstBox.Play();
        StartCoroutine(MoveAndDeactivateBox(box));
    }

    private void CreatePlayerCollider()
    {
        // Duplicamos el tipo de collider que ya estés usando y lo posicionamos para el Player
        if (_triggerCollider is BoxCollider box)
        {
            var pCol = gameObject.AddComponent<BoxCollider>();
            pCol.isTrigger = true;
            pCol.size = box.size;
            pCol.center = playerHitOffset;
            _playerTriggerCollider = pCol;
        }
        else if (_triggerCollider is SphereCollider sphere)
        {
            var pCol = gameObject.AddComponent<SphereCollider>();
            pCol.isTrigger = true;
            pCol.radius = sphere.radius;
            pCol.center = playerHitOffset;
            _playerTriggerCollider = pCol;
        }
        else if (_triggerCollider is CapsuleCollider capsule)
        {
            var pCol = gameObject.AddComponent<CapsuleCollider>();
            pCol.isTrigger = true;
            pCol.radius = capsule.radius;
            pCol.height = capsule.height;
            pCol.direction = capsule.direction;
            pCol.center = playerHitOffset;
            _playerTriggerCollider = pCol;
        }
    }

    private void DisableAllColliders()
    {
        if (_triggerCollider != null) _triggerCollider.enabled = false;
        if (_playerTriggerCollider != null) _playerTriggerCollider.enabled = false;
    }

    private IEnumerator MoveAndDeactivateBox(GameObject box)
    {
        Vector3 startPos = box.transform.position;
        Quaternion startRot = box.transform.rotation;

        float time = 0f;
        while (time < catchDuration)
        {
            time += Time.deltaTime;
            float t01 = Mathf.Clamp01(time / catchDuration);
            float curveValue = catchCurve.Evaluate(t01);

            box.transform.position = Vector3.LerpUnclamped(startPos, socket.position, curveValue);
            box.transform.rotation = Quaternion.LerpUnclamped(startRot, socket.rotation, curveValue);

            yield return null;
        }

        box.SetActive(false);
        fakeBox.SetActive(true);
    }

    private void HandleSecondBoxImpact(GameObject box)
    {
        if (box.TryGetComponent<Rigidbody>(out var boxRb))
        {
            boxRb.freezeRotation = false;
        }
        
        GameEventManager.Instance.bossEvents.OnDeath.Raise(BossDeathType.BoxImpact);
    }

    private void HandlePlayerSecondImpact()
    {
        GameEventManager.Instance.bossEvents.OnDeath.Raise(BossDeathType.PlayerImpact);
    }

    private void SetColliderCenter(Vector3 offset)
    {
        if (_triggerCollider is BoxCollider box) box.center = offset;
        else if (_triggerCollider is SphereCollider sphere) sphere.center = offset;
        else if (_triggerCollider is CapsuleCollider capsule) capsule.center = offset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 size = box != null ? box.size : Vector3.one;

        // Primer hit (Verde)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(firstHitOffset, size);

        // Segundo hit CAJA (Rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(secondHitOffset, size);

        // Segundo hit PLAYER (Cian)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(playerHitOffset, size);

        // Línea de trayectoria del collider de la caja
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(firstHitOffset, secondHitOffset);
    }
}
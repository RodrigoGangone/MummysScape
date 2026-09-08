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
    [Tooltip("El 'Center' local del collider esperando el 2do hit")]
    [SerializeField] private Vector3 secondHitOffset;

    [Header("Catch Settings")] 
    [SerializeField] private float catchDuration = 0.5f; 
    [SerializeField] private AnimationCurve catchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private HashSet<GameObject> _caughtBoxes = new();
    private Collider _triggerCollider;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        SetColliderCenter(firstHitOffset);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HEAVY_BOX_TAG))
        {
            GameObject box = other.gameObject;

            if (_caughtBoxes.Add(box))
            {
                if (_caughtBoxes.Count == 1)
                {
                    HandleFirstBoxCatch(box);
                }
                else
                {
                    HandleSecondBoxImpact(box);
                }
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
        
        // Modificamos únicamente la posición central del collider
        SetColliderCenter(secondHitOffset);
        impactFirstBox.Play();
        StartCoroutine(MoveAndDeactivateBox(box));
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
        var boxRb = box.GetComponent<Rigidbody>();
        boxRb.freezeRotation = false;
        
        GameEventManager.Instance.bossEvents.OnDeath.Raise();
    }

    private void SetColliderCenter(Vector3 offset)
    {
        if (_triggerCollider is BoxCollider box) box.center = offset;
        else if (_triggerCollider is SphereCollider sphere) sphere.center = offset;
        else if (_triggerCollider is CapsuleCollider capsule) capsule.center = offset;
    }

    private void OnDrawGizmosSelected()
    {
        // Aplicamos la matriz del transform para que los gizmos respeten rotación y escala
        Gizmos.matrix = transform.localToWorldMatrix;

        // Intentamos sacar el tamaño si es un BoxCollider (lo más común para este tipo de catch)
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 size = box != null ? box.size : Vector3.one;

        // Primer hit (Verde)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(firstHitOffset, size);

        // Segundo hit (Rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(secondHitOffset, size);

        // Línea de trayectoria
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(firstHitOffset, secondHitOffset);
    }
}
using System;
using System.Collections;
using UnityEngine;

public class SelectorMovement : MonoBehaviour
{
    [Header("Actor")]
    [Tooltip("Transform raíz que se desplaza entre los nodos.")]
    [SerializeField] private Transform actor;

    [Tooltip("Transform visual que rota hacia la dirección de movimiento. Puede ser el mismo Actor.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Animator del modelo del personaje.")]
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.6f;

    [SerializeField]
    private AnimationCurve moveCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Rotation")]
    [Tooltip("Hace que el personaje mire hacia el nodo al que se está desplazando.")]
    [SerializeField] private bool faceMovementDirection = true;

    [Tooltip("Rotación extra si el modelo no mira hacia +Z.")]
    [SerializeField] private float visualYawOffset = 0f;

    [Tooltip("Al llegar, toma la rotación del SelectionPoint.")]
    [SerializeField] private bool useTargetRotationAtEnd = false;

    [Header("Animation Triggers")]
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string walkTrigger = "Walk";

    private Coroutine _moveRoutine;

    private int _idleTriggerHash;
    private int _walkTriggerHash;

    public bool IsMoving => _moveRoutine != null;

    public Transform Actor => actor;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = actor;

        _idleTriggerHash =
            Animator.StringToHash(idleTrigger);

        _walkTriggerHash =
            Animator.StringToHash(walkTrigger);
    }

    private void Start()
    {
        PlayIdle();
    }

    // ============================================================
    // POSITION
    // ============================================================

    public void SnapTo(Transform target)
    {
        if (actor == null || target == null)
            return;

        StopCurrentMovement();

        actor.position = target.position;

        if (useTargetRotationAtEnd)
            ApplyTargetRotation(target);

        PlayIdle();
    }

    public void MoveTo(
        Transform target,
        Action onComplete = null)
    {
        MoveTo(
            target,
            moveDuration,
            onComplete
        );
    }
    
    public void MoveTo(
        Transform target,
        float duration,
        Action onComplete = null)
    {
        if (actor == null || target == null)
        {
            onComplete?.Invoke();
            return;
        }

        StopCurrentMovement();

        _moveRoutine = StartCoroutine(
            MoveRoutine(
                target,
                duration,
                onComplete
            )
        );
    }

    public void StopCurrentMovement()
    {
        if (_moveRoutine == null)
            return;

        StopCoroutine(_moveRoutine);
        _moveRoutine = null;

        PlayIdle();
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    private IEnumerator MoveRoutine(
        Transform target,
        float duration,
        Action onComplete)
    {
        Vector3 startPosition =
            actor.position;

        Vector3 targetPosition =
            target.position;

        FaceTarget(
            startPosition,
            targetPosition
        );

        PlayWalk();
        
        duration =
            Mathf.Max(
                0.01f,
                duration
            );
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float t =
                moveCurve != null
                    ? moveCurve.Evaluate(normalized)
                    : normalized;

            actor.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            yield return null;
        }

        actor.position =
            targetPosition;

        if (useTargetRotationAtEnd)
            ApplyTargetRotation(target);

        _moveRoutine = null;

        PlayIdle();

        onComplete?.Invoke();
    }

    // ============================================================
    // ROTATION
    // ============================================================

    private void FaceTarget(
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        if (!faceMovementDirection ||
            visualRoot == null)
        {
            return;
        }

        Vector3 direction =
            targetPosition - startPosition;

        // Sólo nos interesa la orientación horizontal.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion lookRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        Quaternion yawOffset =
            Quaternion.Euler(
                0f,
                visualYawOffset,
                0f
            );

        visualRoot.rotation =
            lookRotation * yawOffset;
    }

    private void ApplyTargetRotation(
        Transform target)
    {
        if (visualRoot == null ||
            target == null)
        {
            return;
        }

        Quaternion yawOffset =
            Quaternion.Euler(
                0f,
                visualYawOffset,
                0f
            );

        visualRoot.rotation =
            target.rotation * yawOffset;
    }

    // ============================================================
    // ANIMATION
    // ============================================================

    private void PlayWalk()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(
            _idleTriggerHash
        );

        animator.SetTrigger(
            _walkTriggerHash
        );
    }

    private void PlayIdle()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(
            _walkTriggerHash
        );

        animator.SetTrigger(
            _idleTriggerHash
        );
    }

    // ============================================================

    private void OnDisable()
    {
        if (_moveRoutine != null)
        {
            StopCoroutine(
                _moveRoutine
            );

            _moveRoutine = null;
        }
    }
}
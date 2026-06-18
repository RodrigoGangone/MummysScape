using UnityEngine;

public class FlameLagController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private Transform lagTarget;

    [Header("Lag Settings")]
    [SerializeField] private float maxAngle = 18f;
    [SerializeField] private float speedMultiplier = 8f;
    [SerializeField] private float smooth = 8f;

    private Vector3 previousPosition;
    private Quaternion initialLocalRotation;

    private void Start()
    {
        previousPosition = characterRoot.position;
        initialLocalRotation = lagTarget.localRotation;
    }

    private void LateUpdate()
    {
        Vector3 velocity = (characterRoot.position - previousPosition) / Time.deltaTime;
        previousPosition = characterRoot.position;

        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        localVelocity.y = 0f;

        float xTilt = Mathf.Clamp(localVelocity.z * speedMultiplier, -maxAngle, maxAngle);
        float zTilt = Mathf.Clamp(-localVelocity.x * speedMultiplier, -maxAngle, maxAngle);

        Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(xTilt, 0f, zTilt);

        lagTarget.localRotation = Quaternion.Slerp(
            lagTarget.localRotation,
            targetRotation,
            Time.deltaTime * smooth
        );
    }
}
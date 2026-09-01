using UnityEngine;

public class HeadFireWindController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private ParticleSystem fireParticles;

    [Header("Wind Settings")]
    [SerializeField] private float windMultiplier = 1.0f;
    [SerializeField] private float verticalForce = 0.75f;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float maxWind = 2.0f;

    private Vector3 _lastPosition;
    private Vector3 _currentWind;

    private void Awake()
    {
        _lastPosition = player.position;
    }

    private void Update()
    {
        Vector3 velocity = (player.position - _lastPosition) / Time.deltaTime;
        _lastPosition = player.position;

        Vector3 targetWind = -velocity * windMultiplier;
        targetWind = Vector3.ClampMagnitude(targetWind, maxWind);

        _currentWind = Vector3.Lerp(
            _currentWind,
            targetWind,
            Time.deltaTime * smoothSpeed
        );

        ApplyWind();
    }

    private void ApplyWind()
    {
        var velocityModule = fireParticles.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World;

        velocityModule.x = _currentWind.x;
        velocityModule.y = verticalForce;
        velocityModule.z = _currentWind.z;
    }
}
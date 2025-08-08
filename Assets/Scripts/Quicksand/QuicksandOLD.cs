using System;
using UnityEngine;
using UnityEngine.Serialization;

public class QuicksandOLD : MonoBehaviour
{
    [Header("Platform Settings")] private BoxCollider invisiblePlatform; // Reference to the BoxCollider
    private Vector3 startPosition; // Starting position of the invisible platform's center
    private Vector3 endPosition; // Ending position of the invisible platform's center

    [Header("Movement Settings")] [Range(0.05f, 0.5f)]
    public float moveSpeedDown = 0.1f; // Speed at which the platform moves down for Normal size

    [Range(0f, 0f)] public float moveSpeedDownHead = 0f; // Speed at which the platform moves down for Head size
    [Range(0.05f, 1f)] public float moveSpeedUp = 0.5f; // Speed at which the platform moves up (twice as fast)
    [SerializeField] float yOffset = -3f; // The amount to offset the end position in the Y axis

    [Header("FX")] [SerializeField] private GameObject _sinkFX;

    private bool isMoving; // Flag to check if the platform is moving
    private Vector3 targetPosition; // Current target position
    private float currentMoveSpeed; // Current movement speed

    private Player _player;
    private LevelManager _levelManager;

    [SerializeField] private float _currentTime;
    [SerializeField] private float _timeDeath;
    private bool _isPlayerOnPlatform; // Flag to track if player is currently on the platform
    private bool _activeTimeToDeath;

    void Start()
    {
        _player = FindObjectOfType<Player>();
        _levelManager = FindObjectOfType<LevelManager>();

        // Initialize platform position
        invisiblePlatform = GetComponent<BoxCollider>();
        startPosition = invisiblePlatform.center;
        endPosition = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);

        // Subscribe to player size modification event
        _player.SizeModify += UpdateMovementSpeedForPlayerSize;
        _levelManager.OnPlayerDeath += SinkPlayer;
    }

    void Update()
    {
        if (isMoving)
        {
            MovePlatform();
        }

        if (_activeTimeToDeath)
        {
            _currentTime += Time.deltaTime;
            currentMoveSpeed = moveSpeedDown;

            if (_currentTime >= _timeDeath)
            {
                _levelManager.OnPlayerDeath?.Invoke();
                enabled = false; //dehabilito el script para no llamar multiples veces a OnPlayerDeath
            }
        }
    }

    private void MovePlatform()
    {
        Vector3 currentPosition = invisiblePlatform.center;
        Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPosition, currentMoveSpeed * Time.deltaTime);
        invisiblePlatform.center = new Vector3(currentPosition.x, newPosition.y, currentPosition.z);

        if (invisiblePlatform.center == targetPosition)
        {
            isMoving = false;
        }
    }

    private void UpdateMovementSpeedForPlayerSize()
    {
        if (_isPlayerOnPlatform)
        {
            if (_player.CurrentPlayerSize == PlayerSize.Head)
            {
                _activeTimeToDeath = false;
                _currentTime = 0;
                currentMoveSpeed = moveSpeedUp;

                SetTargetPosition(startPosition, currentMoveSpeed);
            }
            else
            {
                _activeTimeToDeath = true;

                SetTargetPosition(endPosition, currentMoveSpeed);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("PlayerFather")) return;

        _player.WalkingSand = true;

        _isPlayerOnPlatform = true;
        UpdateMovementSpeedForPlayerSize();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag("PlayerFather")) return;

        _player.WalkingSand = false;

        _activeTimeToDeath = false;
        _isPlayerOnPlatform = false;
        _currentTime = 0;
        SetTargetPosition(startPosition, moveSpeedUp);
    }

    private void SetTargetPosition(Vector3 newTargetPosition, float speed)
    {
        targetPosition = newTargetPosition;
        currentMoveSpeed = speed;
        isMoving = true;
    }

    private void SinkPlayer()
    {
        _levelManager.OnPlayerDeath -= SinkPlayer;

        Instantiate(_sinkFX, _player.transform.position, _player.transform.rotation);
    }
}
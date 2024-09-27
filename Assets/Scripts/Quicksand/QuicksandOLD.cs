using UnityEngine;
using static Utils;

public class QuicksandOLD : MonoBehaviour
{
    [Header("Platform Settings")]
    private BoxCollider invisiblePlatform; // Reference to the BoxCollider
    private Vector3 startPosition; // Starting position of the invisible platform's center
    private Vector3 endPosition; // Ending position of the invisible platform's center

    [Header("Movement Settings")]
    [Range(0.05f, 0.5f)] public float moveSpeedDownNormal = 0.2f; // Speed at which the platform moves down for Normal size
    [Range(0.05f, 0.5f)] public float moveSpeedDownSmall = 0.1f; // Speed at which the platform moves down for Small size
    [Range(0f, 0f)] public float moveSpeedDownHead = 0f; // Speed at which the platform moves down for Head size
    [Range(0.05f, 1f)] public float moveSpeedUp = 0.5f; // Speed at which the platform moves up (twice as fast)
    public float yOffset = -3f; // The amount to offset the end position in the Y axis

    private bool isMoving; // Flag to check if the platform is moving
    private Vector3 targetPosition; // Current target position
    private float currentMoveSpeed; // Current movement speed

    private Player player;
    private bool isPlayerOnPlatform; // Flag to track if player is currently on the platform

    void Start()
    {
        player = FindObjectOfType<Player>();
        
        // Initialize platform position
        invisiblePlatform = GetComponent<BoxCollider>();
        startPosition = invisiblePlatform.center;
        endPosition = new Vector3(startPosition.x, startPosition.y + yOffset, startPosition.z);
        
        // Subscribe to player size modification event
        player._modelPlayer.SizeModify += UpdateMovementSpeedForPlayerSize;
        
        // Set initial movement speed based on player's current size
        UpdateMovementSpeedForPlayerSize();
    }

    void Update()
    {
        if (isMoving)
        {
            MovePlatform();
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
        switch (player.CurrentPlayerSize)
        {
            case PlayerSize.Small:
                currentMoveSpeed = moveSpeedDownSmall;
                break;
            case PlayerSize.Head:
                if (isPlayerOnPlatform)
                {
                    currentMoveSpeed = moveSpeedUp;
                    SetTargetPosition(startPosition, currentMoveSpeed);
                }
                else
                {
                    currentMoveSpeed = moveSpeedDownHead;
                }
                break;
            case PlayerSize.Normal:
            default:
                currentMoveSpeed = moveSpeedDownNormal;
                break;
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(PLAYER_TAG))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                isPlayerOnPlatform = true;
                UpdateMovementSpeedForPlayerSize();
                if (player.CurrentPlayerSize == PlayerSize.Head)
                {
                    SetTargetPosition(startPosition, currentMoveSpeed);
                }
                else
                {
                    SetTargetPosition(endPosition, currentMoveSpeed);
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(PLAYER_TAG))
        {
            isPlayerOnPlatform = false;
            SetTargetPosition(startPosition, moveSpeedUp);
        }
    }

    private void SetTargetPosition(Vector3 newTargetPosition, float speed)
    {
        targetPosition = newTargetPosition;
        currentMoveSpeed = speed;
        isMoving = true;
    }
}

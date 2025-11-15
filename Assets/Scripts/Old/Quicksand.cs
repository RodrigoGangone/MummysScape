using System.Collections;
using UnityEngine;
using static PlayerEnum;

public class Quicksand : MonoBehaviour
{
    [Header("Platform Settings")] 
    
    private Vector3 _startPosition;
    private Vector3 _endPosition;

    [Header("Movement Settings")] 
    
    [SerializeField, Range(0.05f, 0.5f)] private float moveSpeed = 0.1f;
    [SerializeField] private float yOffset = -3f;

    [Header("Timer Settings")] 
    
    [SerializeField] private float timeToDrown = 3.0f;

    private Coroutine _drownCoroutine;
    private Coroutine _resetCoroutine;

    [Header("FX")] 
    
    [SerializeField] private GameObject sinkFx;
    [SerializeField] private GameObject drowningFx;

    private bool _isMoving;
    private bool _isPlayerOnPlatform;
    private float _currentMoveSpeed;

    private Vector3 _targetPosition;
    private PlayerContext _player;
    private GameObject _currentDrowningFxInstance;

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnDeath.Register(Drowned);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Register<PlayerSize>(Apply);
    }


    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Drowned);
        GameEventManager.Instance.playerEvents.OnSizeChanged.Unregister<PlayerSize>(Apply);
    }

    private void Drowned()
    {
        if (_player != null && sinkFx != null)
            Instantiate(sinkFx, _player.Tf.position, _player.Tf.rotation);

        if (_currentDrowningFxInstance != null)
        {
            Destroy(_currentDrowningFxInstance);
            _currentDrowningFxInstance = null;
        }
    }

    private void Start()
    {
        _startPosition = transform.position;
        _endPosition = new Vector3(_startPosition.x, _startPosition.y + yOffset, _startPosition.z);
    }

    private void Update()
    {
        if (_isMoving)
            MovePlatform();

        if (_currentDrowningFxInstance != null && _player != null)
            _currentDrowningFxInstance.transform.position = _player.Tf.position;
    }

    private void MovePlatform()
    {
        Vector3 currentPosition = transform.position;
        Vector3 newPosition = Vector3.MoveTowards(currentPosition, _targetPosition, _currentMoveSpeed * Time.deltaTime);
        transform.position = new Vector3(currentPosition.x, newPosition.y, currentPosition.z);
        if (transform.position == _targetPosition)
            _isMoving = false;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("PlayerFather")) return;

        _player ??= col.gameObject.GetComponent<PlayerController>().Ctx;

        if (_player != null && _player.Model.Size == PlayerSize.Head)
            return;

        _isPlayerOnPlatform = true;

        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }

        SetTargetPosition(_endPosition, moveSpeed);

        if (_drownCoroutine != null)
            StopCoroutine(_drownCoroutine);
        
        _drownCoroutine = StartCoroutine(DrownTimer());

        if (drowningFx != null && _currentDrowningFxInstance == null)
            _currentDrowningFxInstance = Instantiate(drowningFx, _player.Tf.position, Quaternion.identity);
    }

    private void OnCollisionExit(Collision col)
    {
        if (!col.gameObject.CompareTag("PlayerFather")) return;

        _isPlayerOnPlatform = false;

        _resetCoroutine ??= StartCoroutine(ResetPlatformTimer());
    }

    private void SetTargetPosition(Vector3 newTargetPosition, float speed)
    {
        _targetPosition = newTargetPosition;
        _currentMoveSpeed = speed;
        _isMoving = true;
    }

    private IEnumerator DrownTimer()
    {
        yield return new WaitForSeconds(timeToDrown);

        if (_isPlayerOnPlatform)
        {
            if (_currentDrowningFxInstance != null)
            {
                Destroy(_currentDrowningFxInstance);
                _currentDrowningFxInstance = null;
            }

            GameEventManager.Instance.levelEvents.OnDeath.Raise();
        }

        _drownCoroutine = null;
    }

    private IEnumerator ResetPlatformTimer()
    {
        yield return new WaitForSeconds(0.1f);

        if (_isPlayerOnPlatform) yield break;

        SetTargetPosition(_startPosition, moveSpeed);

        if (_drownCoroutine != null)
        {
            StopCoroutine(_drownCoroutine);
            _drownCoroutine = null;
        }

        if (_currentDrowningFxInstance != null)
        {
            Destroy(_currentDrowningFxInstance);
            _currentDrowningFxInstance = null;
        }
        
        _resetCoroutine = null;
    }

    private void Apply(PlayerSize newSize)
    {
        if (newSize != PlayerSize.Head || !_isPlayerOnPlatform) return;
        
        _isPlayerOnPlatform = false;
            
        if (_drownCoroutine != null)
        {
            StopCoroutine(_drownCoroutine);
            _drownCoroutine = null;
        }
            
        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }
            
        if (_currentDrowningFxInstance != null)
        {
            Destroy(_currentDrowningFxInstance);
            _currentDrowningFxInstance = null;
        }
            
        SetTargetPosition(_startPosition, moveSpeed);
    }
}
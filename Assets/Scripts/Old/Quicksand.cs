using System.Collections;
using UnityEngine;
using static Tags; 
using static Layers; 

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
    
    private float _drowningFxFixedY;

    private void OnEnable()
    { 
        GameEventManager.Instance.levelEvents.OnDeath.Register(Drowned);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnDeath.Unregister(Drowned);
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
        {
            Vector3 p = _player.Tf.position;
            _currentDrowningFxInstance.transform.position = 
                new Vector3(p.x, _drowningFxFixedY, p.z);
        }
        
        if (_isPlayerOnPlatform && _player != null)
        {
            CheckPlayerBandagesState();
        }
    }

    private void MovePlatform()
    {
        Vector3 currentPosition = transform.position;
        Vector3 newPosition = Vector3.MoveTowards(currentPosition, _targetPosition, _currentMoveSpeed * Time.deltaTime);
        transform.position = new Vector3(currentPosition.x, newPosition.y, currentPosition.z);
        if (transform.position == _targetPosition)
            _isMoving = false;
    }

    private void CheckPlayerBandagesState()
    {
        int currentBandages = _player.Model.Bandages;
        
        if (currentBandages == 0)
        {
            EscapeQuicksand();
        }
    }
    
    private void EscapeQuicksand()
    {
        if (!_isPlayerOnPlatform) return; 

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

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(BANDAGE_MOUND_LAYER))
        {
            Instantiate(sinkFx, other.transform.position, other.transform.rotation);
            Destroy(other.gameObject);
        }
        
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        _player ??= other.gameObject.GetComponent<PlayerController>().Ctx;

        if (_player == null || _player.Model.Bandages == 0)
            return; 

        if (_isPlayerOnPlatform)
            return; 

        _isPlayerOnPlatform = true;

        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }

        SetTargetPosition(_endPosition, moveSpeed);

        StartDrownTimer();

        if (drowningFx != null && _currentDrowningFxInstance == null)
        {
            _currentDrowningFxInstance = Instantiate(drowningFx, _player.Tf.position, Quaternion.identity);
            _drowningFxFixedY = _currentDrowningFxInstance.transform.position.y;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag(PLAYER_TAG)) return;

        _isPlayerOnPlatform = false;

        _resetCoroutine ??= StartCoroutine(ResetPlatformTimer());
    }

    private void SetTargetPosition(Vector3 newTargetPosition, float speed)
    {
        _targetPosition = newTargetPosition;
        _currentMoveSpeed = speed;
        _isMoving = true;
    }

    private void StartDrownTimer()
    {
        if (_drownCoroutine != null)
            StopCoroutine(_drownCoroutine);
            
        _drownCoroutine = StartCoroutine(DrownTimer());
    }

    private IEnumerator DrownTimer()
    {
        yield return new WaitForSeconds(timeToDrown);

        if (_isPlayerOnPlatform && _player != null && _player.Model.Bandages > 0)
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
    
    private void Apply(int bandagesCount)
    {
        // Vacío
    }
}
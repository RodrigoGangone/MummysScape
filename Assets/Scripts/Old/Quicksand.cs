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
            _currentDrowningFxInstance.transform.position = _player.Tf.position;
        
        // LÓGICA DE VERIFICACIÓN EN UPDATE: Siempre se chequea el estado de vendajes mientras esté en la plataforma.
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
        
        // Si tiene 0 vendajes, escapa (la condición de escape está en Update)
        if (currentBandages == 0)
        {
            EscapeQuicksand();
        }
        // Nota: Si los vendajes cambian de 1 a 2 o viceversa, la lógica no reinicia el timer 
        // aquí, ya que el DrownTimer sigue corriendo normalmente. 
        // Si el jugador entra con 1 y gana 1 más, no hay "ventana de gracia" aquí.
    }
    
    private void EscapeQuicksand()
    {
        // Solo ejecuta la lógica de escape si actualmente está en la plataforma.
        if (!_isPlayerOnPlatform) return; 

        // Cancelar el estado de hundimiento
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
            
        // La plataforma vuelve a su posición inicial
        SetTargetPosition(_startPosition, moveSpeed);
    }

    // *** CAMBIO CLAVE: Usamos OnTriggerStay para iniciar y mantener la detección ***
    private void OnTriggerStay(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

        _player ??= other.gameObject.GetComponent<PlayerController>().Ctx;

        // Si el jugador no existe o está en estado Head (0 vendajes), NO hacemos nada.
        if (_player == null || _player.Model.Bandages == 0)
            return; 

        // Si el jugador ya está marcado como en la plataforma, la lógica ya está activa 
        // y el Update está comprobando el estado. No necesitamos re-iniciar todo.
        if (_isPlayerOnPlatform)
            return; 

        // INICIAR EL PROCESO (Solo la primera vez que se detecta y tiene vendajes)

        _isPlayerOnPlatform = true;

        if (_resetCoroutine != null)
        {
            StopCoroutine(_resetCoroutine);
            _resetCoroutine = null;
        }

        SetTargetPosition(_endPosition, moveSpeed);

        // Inicia el temporizador de ahogamiento
        StartDrownTimer();

        if (drowningFx != null && _currentDrowningFxInstance == null)
            _currentDrowningFxInstance = Instantiate(drowningFx, _player.Tf.position, Quaternion.identity);
    }

    // Usamos OnTriggerExit para detectar al jugador cuando sale del volumen
    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather")) return;

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

        // Solo ahogar si el jugador sigue en la plataforma y aún tiene vendajes (> 0)
        // La comprobación final es crucial para evitar ahogar al jugador que escapó en el último frame.
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
    
    // Método Apply - Mantenido como stub.
    private void Apply(int bandagesCount)
    {
        // Vacío
    }
}
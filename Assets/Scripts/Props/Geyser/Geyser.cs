using System.Collections;
using UnityEngine;
using static PlayerEnum;
using static PauseUtils;

/// <summary> 
/// Controlador central del Geyser.
/// Extrae los tiempos del ParticleSystem y eleva la plataforma SOLO si el jugador está dentro durante la erupción.
/// </summary>
public class Geyser : MonoBehaviour, IPausable
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem _mainGeyserVFX;      
    [SerializeField] private ParticleSystem _eruptionParticle;   

    [Header("TIMING (Cooldown Manual)")]
    [SerializeField] private float _cooldownTime = 3.0f;     
    
    private float _anticipationTime; 
    private float _eruptionTime;     

    [Header("MOVIMIENTO")]
    [SerializeField] private Transform _platform; 
    [SerializeField] private Transform _pos1;     
    [SerializeField] private Transform _pos2;     
    [SerializeField] private float _platformRiseSpeed = 15f; 

    private PlayerContext _currentPlayerContext; 
    private Transform _playerTransform;

    private bool _paused;
    private bool _isErupting;
    private bool _playerInTrigger;
    private bool _penaltyApplied;

    private void Awake()
    {
        var mainModule = _eruptionParticle.main;
        _anticipationTime = mainModule.startDelay.constant; 
        _eruptionTime = mainModule.startLifetime.constant;                
    }

    private void Start()
    {
        _platform.position = _pos1.position;
        _platform.gameObject.SetActive(false); 
        StartCoroutine(GeyserCycle());
    }

    private void FixedUpdate()
    {
        if (_paused) return;

        // ⚡ NUEVA LÓGICA: Sube SOLO si está escupiendo arena Y el jugador está sobre ella.
        if (_isErupting && _playerInTrigger)
        {
            _platform.position = Vector3.MoveTowards(
                _platform.position, 
                _pos2.position, 
                _platformRiseSpeed * Time.fixedDeltaTime
            );
        }
        else
        {
            // Si no hay erupción, o el jugador no está tocando el geyser, se queda en la base.
            _platform.position = _pos1.position;
        }
    }

    private IEnumerator GeyserCycle()
    {
        while (true)
        {
            // 1. REPOSO
            _isErupting = false;
            _penaltyApplied = false;
            ReleasePlayer();
            _platform.gameObject.SetActive(false); 
            
            if (_mainGeyserVFX != null && _mainGeyserVFX.isPlaying) 
                _mainGeyserVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            
            yield return WaitForSecondsPausable(_cooldownTime, () => _paused);

            // 2. ANTICIPACIÓN
            if (_mainGeyserVFX != null) 
                _mainGeyserVFX.Play(true);
            
            yield return WaitForSecondsPausable(_anticipationTime, () => _paused);

            // 3. ERUPCIÓN
            _isErupting = true;
            _platform.gameObject.SetActive(true); // Se activa en la base esperando al jugador
            
            yield return WaitForSecondsPausable(_eruptionTime, () => _paused);
        }
    }

    public void OnPlayerEnterTrigger(Collider player)
    {
        _playerInTrigger = true;
        _playerTransform = player.transform;
        _playerTransform.SetParent(_platform);
        
        PlayerController pController = player.GetComponentInParent<PlayerController>();
        if (pController != null)
        {
            _currentPlayerContext = pController.Ctx;
            CheckAndApplyPenalty();
        }
    }

    public void OnPlayerExitTrigger(Collider player)
    {
        if (player.transform == _playerTransform)
        {
            ReleasePlayer();
        }
    }

    private void ReleasePlayer()
    {
        if (_playerInTrigger && _playerTransform != null)
        {
            _playerTransform.SetParent(null);
        }
        
        _playerInTrigger = false;
        _playerTransform = null;
        _currentPlayerContext = null;
    }

    private void CheckAndApplyPenalty()
    {
        if (_isErupting && _playerInTrigger && !_penaltyApplied && _currentPlayerContext != null)
        {
            if (_currentPlayerContext.Model.Size != PlayerSize.Head)
            {
                _currentPlayerContext.Model.TryConsumeBandage(_currentPlayerContext.Model.Bandages);
            }
            _penaltyApplied = true; 
        }
    }

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;

        if (_mainGeyserVFX != null)
        {
            if (paused && _mainGeyserVFX.isPlaying) 
                _mainGeyserVFX.Pause(true);
            else if (!paused && _mainGeyserVFX.isPaused) 
                _mainGeyserVFX.Play(true);
        }
    }
    
    private void OnEnable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    private void OnDisable() => GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
}
using UnityEngine;

public class Audio3DEmitter : MonoBehaviour
{
    [Header("Bank")]
    [SerializeField] private FxBank _bank;
    [SerializeField] private string _key;          // ej: "Torch_Loop", "Trap_Open"

    [Header("Modo")]
    [SerializeField] private bool _loop = true;    // true = loop local, false = one-shot 3D
    [SerializeField] private bool _playOnStart = true;

    [Header("Gizmos")]
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private float _extraRadius = 0f;  // por si querés inflar un poco el círculo
    [SerializeField] private Color _gizmoColor = new Color(1f, 0.6f, 0f, 0.25f);

    private AudioSource _src;

    private void Awake()
    {
        if (_loop)
            SetupSourceForLoop();
    }

    private void Start()
    {
        if (_playOnStart)
            Play();
        
    }

    // ---------- API pública ----------

    public void Play()
    {
        if (_bank == null || string.IsNullOrEmpty(_key))
            return;

        if (_loop)
        {
            if (_src == null)
                SetupSourceForLoop();

            if (_src != null && _src.clip != null && !_src.isPlaying)
                _src.Play();
        }
        else
        {
            // one-shot 3D usando tu sistema central
            _bank.Play3D(_key, transform.position);
        }
    }

    public void Stop()
    {
        if (_loop && _src != null && _src.isPlaying)
            _src.Stop();
    }

    // ---------- Interno ----------

    private void SetupSourceForLoop()
    {
        var entry = _bank != null ? _bank.Get(_key) : null;
        if (entry == null || entry.clip == null)
        {
            Debug.LogWarning($"Audio3DEmitter ({name}): key '{_key}' no encontrada en bank '{_bank?.name}'.");
            return;
        }

        _src = GetComponent<AudioSource>();
        if (_src == null)
            _src = gameObject.AddComponent<AudioSource>();

        var group = AudioManager.Instance.GetMixerGroup(AudioBus.Sfx);
        
        _src.outputAudioMixerGroup = group;
        _src.clip         = entry.clip;
        _src.loop         = true;
        _src.playOnAwake  = false;
        _src.volume       = entry.volume;
        _src.pitch        = entry.pitch;
        _src.spatialBlend = entry.is3D ? entry.spatialBlend : 0f;
        _src.maxDistance  = entry.maxDistance;
        _src.rolloffMode  = AudioRolloffMode.Linear;
        // Output: se la seteás en el inspector (SFX / Ambience / etc.)
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos || _bank == null || string.IsNullOrEmpty(_key))
            return;

        // Pedimos al Bank el FxEntry y usamos su maxDistance
        var entry = _bank.Get(_key);
        if (entry == null)
            return;

        float radius = entry.maxDistance + _extraRadius;
        if (radius <= 0f)
            return;

        Gizmos.color = _gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

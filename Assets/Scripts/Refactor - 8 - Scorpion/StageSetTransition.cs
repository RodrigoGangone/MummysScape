using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla el swap de tandas/escenas por "stages".
/// - Baja el grupo actual hasta downOffsetY, lo apaga.
/// - Activa el siguiente grupo en esa altura y lo sube hasta su altura base.
/// - Puede funcionar solo (llamando Next/TransitionTo) o suscripto a BossActor.OnStageChanged.
/// 
/// Requisitos:
/// - "stageRoots": lista de empties (uno por stage) que agrupan todos los assets del stage.
/// - Cada empty debe estar en su altura base (por ej. y = 0) al iniciar.
/// </summary>
[DisallowMultipleComponent]
public class StageSetTransition : MonoBehaviour
{
    [Header("Stage Roots (empties contenedores)")]
    [Tooltip("Un Transform por stage, en orden. Cada uno agrupa los assets de ese stage.")]
    [SerializeField] private List<Transform> stageRoots = new();

    [Header("Enganche opcional con el Boss")]
    [Tooltip("Si lo asignás, se suscribe a OnStageChanged / OnDeath del BossActor.")]
    [SerializeField] private BossActor bossActor; // opcional

    [Header("Animación")]
    [Tooltip("Altura hacia donde baja el grupo saliente (relativa a su posición base).")]
    [SerializeField] private float downOffsetY = -10f;

    [Tooltip("Duración de la bajada del grupo saliente (segundos).")]
    [SerializeField] private float downDuration = 0.6f;

    [Tooltip("Duración de la subida del grupo entrante (segundos).")]
    [SerializeField] private float upDuration = 0.8f;

    [Tooltip("Curva de easing para la bajada.")]
    [SerializeField] private AnimationCurve easeDown = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Curva de easing para la subida.")]
    [SerializeField] private AnimationCurve easeUp = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Si es true, se anima la 'localPosition'. Si es false, se usa 'position' (mundo).")]
    [SerializeField] private bool useLocalPosition = true;

    [Header("Estado")]
    [SerializeField] private int currentStageIndex = 0;

    // Cache de posiciones base de cada root
    private Dictionary<Transform, Vector3> _basePos = new();
    private Coroutine _transitionCR;

    private void Awake()
    {
        // Cachear posiciones base y normalizar activos
        _basePos.Clear();
        for (int i = 0; i < stageRoots.Count; i++)
        {
            var t = stageRoots[i];
            if (t == null) continue;

            _basePos[t] = useLocalPosition ? t.localPosition : t.position;

            // Activar solo el stage inicial
            bool active = (i == currentStageIndex);
            if (t.gameObject.activeSelf != active)
                t.gameObject.SetActive(active);

            // Asegurar que el activo esté en su base
            if (active) SetPos(t, _basePos[t]);
        }
    }

    private void OnEnable()
    {
        if (bossActor != null)
        {
            bossActor.OnStageChanged += OnBossStageChanged;
            bossActor.OnDeath += OnBossDeath;
        }
    }

    private void OnDisable()
    {
        if (bossActor != null)
        {
            bossActor.OnStageChanged -= OnBossStageChanged;
            bossActor.OnDeath -= OnBossDeath;
        }
    }

    /// <summary>
    /// Llamado por BossActor.OnStageChanged (si está asignado).
    /// </summary>
    private void OnBossStageChanged(int nextIndex)
    {
        TransitionTo(nextIndex);
    }

    /// <summary>
    /// Si el boss muere, opcionalmente bajamos y apagamos el actual.
    /// </summary>
    private void OnBossDeath()
    {
        // Podés dejarlo así o hacer una última bajada y apagado:
        if (IsValidIndex(currentStageIndex))
            StartTransition(currentStageIndex, -1); // -1 => sin entrante, solo bajar y apagar.
    }

    /// <summary>
    /// Cambia al siguiente stage (current + 1).
    /// </summary>
    [ContextMenu("Next Stage")]
    public void Next()
    {
        TransitionTo(currentStageIndex + 1);
    }

    /// <summary>
    /// Transiciona al índice pedido.
    /// </summary>
    public void TransitionTo(int nextIndex)
    {
        if (nextIndex == currentStageIndex) return;
        if (!IsValidIndex(currentStageIndex))
        {
            // Si no hay actual válido, forzamos inicio en next
            ForceSetInitialStage(nextIndex);
            return;
        }

        // Si nextIndex es inválido, solo bajar y apagar el actual (por si querés un “fin de secuencia”)
        if (!IsValidIndex(nextIndex))
        {
            StartTransition(currentStageIndex, -1);
            return;
        }

        StartTransition(currentStageIndex, nextIndex);
    }

    /// <summary>
    /// Fija un stage como inicial (activa y posiciona), apagando el resto.
    /// </summary>
    public void ForceSetInitialStage(int index)
    {
        if (!IsValidIndex(index)) return;

        // Apagar todos menos el index
        for (int i = 0; i < stageRoots.Count; i++)
        {
            var t = stageRoots[i];
            if (t == null) continue;

            bool active = (i == index);
            t.gameObject.SetActive(active);
            if (active && _basePos.TryGetValue(t, out var p))
                SetPos(t, p);
        }

        currentStageIndex = index;
    }

    private bool IsValidIndex(int idx) => idx >= 0 && idx < stageRoots.Count;

    private void StartTransition(int fromIndex, int toIndex)
    {
        if (_transitionCR != null) StopCoroutine(_transitionCR);
        _transitionCR = StartCoroutine(TransitionSequence(fromIndex, toIndex));
    }

    private IEnumerator TransitionSequence(int fromIndex, int toIndex)
    {
        var fromRoot = IsValidIndex(fromIndex) ? stageRoots[fromIndex] : null;
        var toRoot   = IsValidIndex(toIndex)   ? stageRoots[toIndex]   : null;

        // 1) BAJAR y APAGAR el saliente
        if (fromRoot != null && _basePos.TryGetValue(fromRoot, out var fromBase))
        {
            Vector3 fromStart = GetPos(fromRoot);
            Vector3 fromEnd   = new Vector3(fromBase.x, fromBase.y + downOffsetY, fromBase.z);

            // Si por alguna razón el start no es la base, animamos desde donde esté hacia el end.
            yield return AnimatePos(fromRoot, fromStart, fromEnd, downDuration, easeDown);

            // Apagar
            fromRoot.gameObject.SetActive(false);
        }

        // 2) PREPARAR el entrante en la altura baja y SUBIRLO a su base
        if (toRoot != null && _basePos.TryGetValue(toRoot, out var toBase))
        {
            // Colocar en “debajo” y activar
            Vector3 toStart = new Vector3(toBase.x, toBase.y + downOffsetY, toBase.z);
            SetPos(toRoot, toStart);
            toRoot.gameObject.SetActive(true);

            // Subir a su base
            yield return AnimatePos(toRoot, toStart, toBase, upDuration, easeUp);

            currentStageIndex = toIndex;
        }

        _transitionCR = null;
    }

    private IEnumerator AnimatePos(Transform t, Vector3 start, Vector3 end, float duration, AnimationCurve curve)
    {
        duration = Mathf.Max(0.0001f, duration);
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t01 = Mathf.Clamp01(time / duration);
            float k = curve != null ? curve.Evaluate(t01) : t01;

            Vector3 pos = Vector3.LerpUnclamped(start, end, k);
            SetPos(t, pos);

            yield return null;
        }

        SetPos(t, end);
    }

    private Vector3 GetPos(Transform t) => useLocalPosition ? t.localPosition : t.position;

    private void SetPos(Transform t, Vector3 p)
    {
        if (useLocalPosition) t.localPosition = p;
        else t.position = p;
    }

    // Gizmos simples para visualizar posiciones base y “down”
    private void OnDrawGizmosSelected()
    {
        if (stageRoots == null) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        foreach (var t in stageRoots)
        {
            if (t == null) continue;

            var baseP = Application.isPlaying && _basePos.ContainsKey(t) ? _basePos[t] : (useLocalPosition ? t.localPosition : t.position);
            var worldBase = useLocalPosition ? t.parent != null ? t.parent.TransformPoint(baseP) : baseP : baseP;

            // Base
            Gizmos.DrawCube(worldBase, Vector3.one * 0.4f);

            // Down
            var downP = worldBase + Vector3.up * downOffsetY;
            Gizmos.DrawWireCube(downP, Vector3.one * 0.35f);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor de Entorno: Coordina el intercambio físico y visual de las diferentes áreas o "sets" 
/// del escenario mediante animaciones de desplazamiento al avanzar de fase en el combate.
/// </summary>
[DisallowMultipleComponent]
public class StageSetTransition : MonoBehaviour, IPausable
{
    [Header("Boss Handler")] [SerializeField]
    private BossActor boss;

    [SerializeField] private float delayToReactivateBoss = 1.5f;
    [SerializeField] private bool activateBossOnStart = true;

    [Header("Stage Roots (empties contenedores)")]
    [Tooltip("Un Transform por stage, en orden. Cada uno agrupa los assets de ese stage.")]
    [SerializeField]
    private List<Transform> stageRoots = new();

    [Header("Animación")] [SerializeField] private float delayBeforeStart = 2.0f;
    [SerializeField] private float downOffsetY = -10f;
    [SerializeField] private float downDuration = 0.6f;
    [SerializeField] private float upDuration = 0.8f;
    [SerializeField] private AnimationCurve easeDown = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve easeUp = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool useLocalPosition = true;

    [Header("Estado")] [SerializeField] private int currentStageIndex = 0;

    private Dictionary<Transform, Vector3> _basePos = new();
    private Coroutine _transitionCR;
    private bool _paused;

    private void Awake()
    {
        _basePos.Clear();
        for (int i = 0; i < stageRoots.Count; i++)
        {
            var t = stageRoots[i];
            if (t == null) continue;

            _basePos[t] = useLocalPosition ? t.localPosition : t.position;

            bool active = (i == currentStageIndex);
            t.gameObject.SetActive(active);

            if (active) SetPos(t, _basePos[t]);
        }

        if (boss != null && activateBossOnStart)
        {
            boss.gameObject.SetActive(false);
            StartCoroutine(InitialBossActivation());
        }
    }

    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
        GameEventManager.Instance.bossEvents.OnStageCompleted.Register<int>(OnBossStageChanged);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        GameEventManager.Instance.bossEvents.OnStageCompleted.Unregister<int>(OnBossStageChanged);
    }

    private IEnumerator InitialBossActivation()
    {
        yield return new WaitForSeconds(delayToReactivateBoss);

        if (boss != null)
        {
            boss.gameObject.SetActive(true);
            Debug.Log("[StageHandler] Boss activado por primera vez.");
        }
    }

    private void OnBossStageChanged(int nextIndex) => TransitionTo(nextIndex);

    public void TransitionTo(int nextIndex)
    {
        if (nextIndex == currentStageIndex) return;

        if (!IsValidIndex(currentStageIndex))
        {
            ForceSetInitialStage(nextIndex);
            return;
        }

        if (!IsValidIndex(nextIndex))
        {
            Debug.LogWarning("[StageSetTransition] Índice de destino inválido: " + nextIndex);
            return;
        }

        if (_transitionCR != null) StopCoroutine(_transitionCR);
        _transitionCR = StartCoroutine(TransitionSequence(currentStageIndex, nextIndex));
    }

    private IEnumerator TransitionSequence(int fromIndex, int toIndex)
    {
        float timer = 0;
        while (timer < delayBeforeStart)
        {
            if (!_paused) timer += Time.deltaTime;
            yield return null;
        }

        var fromRoot = IsValidIndex(fromIndex) ? stageRoots[fromIndex] : null;
        var toRoot = IsValidIndex(toIndex) ? stageRoots[toIndex] : null;

        if (fromRoot != null && _basePos.TryGetValue(fromRoot, out var fromBase))
        {
            Vector3 fromStart = GetPos(fromRoot);
            Vector3 fromEnd = fromBase + Vector3.up * downOffsetY;
            yield return AnimatePos(fromRoot, fromStart, fromEnd, downDuration, easeDown);
            fromRoot.gameObject.SetActive(false);
        }

        if (toRoot != null && _basePos.TryGetValue(toRoot, out var toBase))
        {
            Vector3 toStart = toBase + Vector3.up * downOffsetY;
            SetPos(toRoot, toStart);
            toRoot.gameObject.SetActive(true);

            var focus = toRoot.GetComponent<FocusOnActivation>();
            if (focus != null) focus.Activate();

            yield return AnimatePos(toRoot, toStart, toBase, upDuration, easeUp);
            currentStageIndex = toIndex;
        }

        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Boss", false);
        _transitionCR = null;
    }

    private IEnumerator AnimatePos(Transform t, Vector3 start, Vector3 end, float duration, AnimationCurve curve)
    {
        float time = 0f;
        while (time < duration)
        {
            if (_paused)
            {
                yield return null;
                continue;
            }

            time += Time.deltaTime;
            float t01 = Mathf.Clamp01(time / duration);
            float k = curve != null ? curve.Evaluate(t01) : t01;
            SetPos(t, Vector3.LerpUnclamped(start, end, k));
            yield return null;
        }

        SetPos(t, end);
    }

    private bool IsValidIndex(int idx) => idx >= 0 && idx < stageRoots.Count;
    private Vector3 GetPos(Transform t) => useLocalPosition ? t.localPosition : t.position;

    private void SetPos(Transform t, Vector3 p)
    {
        if (useLocalPosition) t.localPosition = p;
        else t.position = p;
    }

    public void OnPauseChanged(bool paused) => _paused = paused;

    public void ForceSetInitialStage(int index)
    {
        if (!IsValidIndex(index)) return;
        for (int i = 0; i < stageRoots.Count; i++)
        {
            bool active = (i == index);
            stageRoots[i].gameObject.SetActive(active);
            if (active && _basePos.TryGetValue(stageRoots[i], out var p)) SetPos(stageRoots[i], p);
        }

        currentStageIndex = index;
    }
}
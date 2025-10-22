using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameEventManager;

/// <summary>
/// Controla la transición visual entre stages de forma secuencial y minimalista.
/// Baja el stage actual, espera, y luego sube el siguiente.
/// </summary>
[DisallowMultipleComponent]
public class StageSetTransition : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private List<Transform> stageRoots = new();
    [SerializeField] private int initialStageIndex = 0;

    [Header("Animación Secuencial")]
    [SerializeField] private float downDuration = 0.6f;
    [SerializeField] private float delayBetweenStages = 0.2f;
    [SerializeField] private float upDuration = 0.8f;
    
    [SerializeField] private CameraPathManager cameraPathManager;
    
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool useLocalPosition = true;
    
    [SerializeField] private float downOffsetY = -10f;

    private int _currentStageIndex = -1;
    private Dictionary<Transform, Vector3> _basePositions = new();
    private Coroutine _transitionCoroutine;

    private void Start()
    {
        // Cachear posiciones y configurar el estado inicial.
        for (int i = 0; i < stageRoots.Count; i++)
        {
            var root = stageRoots[i];
            if (root == null) continue;

            _basePositions[root] = useLocalPosition ? root.localPosition : root.position;
            bool isActive = (i == initialStageIndex);
            root.gameObject.SetActive(isActive);
            if (isActive) SetPosition(root, _basePositions[root]);
        }
        _currentStageIndex = initialStageIndex;
    }

    private void OnEnable()
    {
        Instance.bossEvents.OnStageChanged.Register<int>(TransitionTo);
        Instance.bossEvents.OnDeath.Register(OnBossDeath);
    }

    private void OnDisable()
    {
        Instance.bossEvents.OnStageChanged.Unregister<int>(TransitionTo);
        Instance.bossEvents.OnDeath.Unregister(OnBossDeath);
    }

    private void OnBossDeath() => TransitionTo(_currentStageIndex + 1);

    private void TransitionTo(int nextIndex)
    {
        if (nextIndex < 0 || nextIndex >= stageRoots.Count || nextIndex == _currentStageIndex) return;

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionSequence(nextIndex));
    }

    private IEnumerator TransitionSequence(int toIndex)
    {
        // --- 1. BAJAR PLATAFORMA ACTUAL ---
        Transform fromRoot = stageRoots[_currentStageIndex];
        Vector3 fromBasePos = _basePositions[fromRoot];
        Vector3 fromEndPos = fromBasePos + Vector3.up * downOffsetY;

        float elapsedTime = 0f;
        while (elapsedTime < downDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsedTime / downDuration));
            SetPosition(fromRoot, Vector3.LerpUnclamped(fromBasePos, fromEndPos, t));
            yield return null;
        }
        SetPosition(fromRoot, fromEndPos);
        fromRoot.gameObject.SetActive(false);

        // --- 2. ESPERAR ---
        if (delayBetweenStages > 0)
        {
            yield return new WaitForSeconds(delayBetweenStages);
        }

        // --- 3. SUBIR PLATAFORMA NUEVA ---
        cameraPathManager.ShakeCamera(upDuration, 2f);
        
        Transform toRoot = stageRoots[toIndex];
        Vector3 toBasePos = _basePositions[toRoot];
        Vector3 toStartPos = toBasePos + Vector3.up * downOffsetY;
        
        toRoot.gameObject.SetActive(true);
        SetPosition(toRoot, toStartPos);
        
        elapsedTime = 0f;
        while (elapsedTime < upDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsedTime / upDuration));
            SetPosition(toRoot, Vector3.LerpUnclamped(toStartPos, toBasePos, t));
            yield return null;
        }
        SetPosition(toRoot, toBasePos);

        // --- 4. FINALIZAR ---
        _currentStageIndex = toIndex;
        _transitionCoroutine = null;
    }

    private void SetPosition(Transform t, Vector3 p)
    {
        if (useLocalPosition) t.localPosition = p;
        else t.position = p;
    }
}
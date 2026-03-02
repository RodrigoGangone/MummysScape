using System.Collections;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class GemCounterAnimator : MonoBehaviour
{
    [Header("Referencias")] 
    public Camera mainCamera;
    public RectTransform canvasRect;
    public RectTransform targetGemUI; 
    public TextMeshProUGUI totalGemsText;
    public Transform playerTransform;

    [Header("Prefab")] 
    public GameObject gemUIPrefab;

    [Header("Configuración")] 
    public float travelDuration = 0.8f;
    public float curveHeight = 100f; 
    public float delayBetweenGems = 0.15f;
    public float initialWaitTime = 2.0f; // Tiempo de espera tras los focos
    public Vector3 iconPunchScale = new Vector3(1.3f, 1.3f, 1.3f);

    IEnumerator Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 1. Esperamos a que todos los Tiles manden sus peticiones en su Start()
        yield return new WaitForEndOfFrame();

        // 2. Si el FocusManager está ocupado con revelaciones, esperamos
        if (FocusManager.Instance != null && FocusManager.Instance.IsBusy)
        {
            while (FocusManager.Instance.IsBusy)
            {
                yield return null;
            }

            // Buffer extra tras las cámaras para que el jugador se ubique
            yield return new WaitForSeconds(initialWaitTime);
        }

        // 3. Lógica de gemas usando tu sistema Save
        int actualTotal = Save.GetGlobalGemCount(); 
        int lastSeenGems = Save.GetSeenGemsCount();

        if (actualTotal > lastSeenGems)
        {
            int gemsToAnimate = actualTotal - lastSeenGems;
            totalGemsText.text = lastSeenGems.ToString();
            yield return StartCoroutine(SequenceRoutine(lastSeenGems, gemsToAnimate, actualTotal));
        }
        else
        {
            totalGemsText.text = actualTotal.ToString();
        }
    }

    private IEnumerator SequenceRoutine(int startCount, int amount, int finalTotal)
    {
        // Pequeño delay inicial antes de que salgan las gemas
        yield return new WaitForSeconds(0.3f);

        Vector3 pPos = (playerTransform != null) ? playerTransform.position : Vector3.zero;
        Vector2 spawnBasePos = WorldToCanvasPosition(pPos + Vector3.up * 1.5f);
        Vector2 destinationPos = GetCanvasPosition(targetGemUI);

        int currentCount = startCount;

        for (int i = 0; i < amount; i++)
        {
            GameObject go = Instantiate(gemUIPrefab, canvasRect);
            RectTransform gemRect = go.GetComponent<RectTransform>();

            gemRect.anchoredPosition = spawnBasePos + (Random.insideUnitCircle * 40f);

            StartCoroutine(AnimateSingleGem(gemRect, destinationPos, () =>
            {
                currentCount++;
                totalGemsText.text = currentCount.ToString();
                StopCoroutine(nameof(PunchIcon));
                StartCoroutine(PunchIcon());
            }));

            yield return new WaitForSeconds(delayBetweenGems);
        }

        // Guardamos el progreso visto
        Save.UpdateSeenGemsCount(finalTotal);
    }

    private IEnumerator AnimateSingleGem(RectTransform gem, Vector2 targetPos, System.Action onComplete)
    {
        float elapsed = 0;
        Vector2 startPos = gem.anchoredPosition;
        Vector2 midPoint = Vector2.Lerp(startPos, targetPos, 0.5f) + Vector2.up * curveHeight;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelDuration;
            float tStep = t * t * (3f - 2f * t); // Ease Out

            Vector2 m1 = Vector2.Lerp(startPos, midPoint, tStep);
            Vector2 m2 = Vector2.Lerp(midPoint, targetPos, tStep);
            gem.anchoredPosition = Vector2.Lerp(m1, m2, tStep);

            yield return null;
        }

        onComplete?.Invoke();
        Destroy(gem.gameObject);
    }

    private IEnumerator PunchIcon()
    {
        targetGemUI.localScale = iconPunchScale;
        yield return new WaitForSeconds(0.1f);
        targetGemUI.localScale = Vector3.one;
    }

    private Vector2 WorldToCanvasPosition(Vector3 worldPos)
    {
        Vector2 screenPoint = mainCamera.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private Vector2 GetCanvasPosition(RectTransform element)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, element.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Random = UnityEngine.Random;

public class GemCounterAnimator : MonoBehaviour
{
    [Header("Referencias")] 
    public Camera mainCamera;
    public RectTransform canvasRect;
    public RectTransform targetGemUI; // El icono de la gema en la esquina
    public TextMeshProUGUI totalGemsText;
    public Transform playerTransform;

    [Header("Prefab")] 
    public GameObject gemUIPrefab;

    [Header("Configuración")] 
    public float travelDuration = 0.8f;
    public float curveHeight = 100f; // Altura del arco para que no sea línea recta
    public float delayBetweenGems = 0.15f;
    public Vector3 iconPunchScale = new Vector3(1.3f, 1.3f, 1.3f);

    private const string GEMS_KEY = "LastSeenGemsCount";

    void Start()
    {
        // El sistema de guardado de Mummy's Escape
        int actualTotal = Save.GetGlobalGemCount();
        int lastSeenGems = PlayerPrefs.GetInt(GEMS_KEY, 0);

        if (actualTotal > lastSeenGems)
        {
            int gemsToAnimate = actualTotal - lastSeenGems;
            totalGemsText.text = lastSeenGems.ToString();
            StartCoroutine(SequenceRoutine(lastSeenGems, gemsToAnimate, actualTotal));
        }
        else
        {
            totalGemsText.text = actualTotal.ToString();
        }
    }

    private IEnumerator SequenceRoutine(int startCount, int amount, int finalTotal)
    {
        yield return new WaitForSeconds(0.5f);

        // Convertimos la posición de la momia (mundo) a la UI una sola vez al inicio
        Vector2 spawnBasePos = WorldToCanvasPosition(playerTransform.position + Vector3.up * 1.5f);
        
        // Calculamos la posición real del icono de destino dentro del Canvas
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
                StartCoroutine(PunchIcon());
            }));

            yield return new WaitForSeconds(delayBetweenGems);
        }

        PlayerPrefs.SetInt(GEMS_KEY, finalTotal);
        PlayerPrefs.Save();
    }

    private IEnumerator AnimateSingleGem(RectTransform gem, Vector2 targetPos, System.Action onComplete)
    {
        float elapsed = 0;
        Vector2 startPos = gem.anchoredPosition;

        // Calculamos un punto medio para crear un arco
        Vector2 midPoint = Vector2.Lerp(startPos, targetPos, 0.5f) + Vector2.up * curveHeight;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelDuration;

            // Suavizado (Ease Out)
            float tStep = t * t * (3f - 2f * t);

            // Trayectoria curva (Curva de Bezier simple)
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

    // --- FUNCIONES DE CONVERSIÓN ---

    private Vector2 WorldToCanvasPosition(Vector3 worldPos)
    {
        Vector2 screenPoint = mainCamera.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private Vector2 GetCanvasPosition(RectTransform element)
    {
        // Esta función asegura que el destino sea exacto sin importar los anchors
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, element.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }
}
using System.Collections;
using UnityEngine;

public class CinematicUIManager : MonoBehaviour
{
    [Header("UI Cinematic Bars")]
    [SerializeField] private RectTransform _topBar;
    [SerializeField] private RectTransform _bottomBar;
    
    [Tooltip("Elemento de UI (HUD, botones) que se ocultará durante la cinemática.")]
    [SerializeField] private RectTransform _uiElementToHide;
    
    [Header("Config")]
    [SerializeField] private float _targetBarHeight = 150f;
    [SerializeField] private float _transitionDuration = 2f;

    private Coroutine _animationRoutine;

    private void OnEnable()
    {
        // Te suscribís al evento global igual que en tu UIManager
        GameEventManager.Instance.levelEvents.OnCinematicToggled.Register<bool>(HandleCinematicToggled);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnCinematicToggled.Unregister<bool>(HandleCinematicToggled);
    }

    public void HandleCinematic(bool value) => GameEventManager.Instance.levelEvents.OnCinematicToggled.Raise(value);
    
    private void HandleCinematicToggled(bool isCinematic)
    {
        if (_animationRoutine != null)
            StopCoroutine(_animationRoutine);

        _animationRoutine = StartCoroutine(AnimateLetterbox(isCinematic));
    }

    private IEnumerator AnimateLetterbox(bool show)
    {
        // Altura de las barras: Si es cine van a target, si no a 0
        float targetHeight = show ? _targetBarHeight : 0f;
        float currentHeight = _topBar.sizeDelta.y;
        
        // Escala de la UI: Si es cine va a 0 (oculto), si no a 1 (visible)
        float targetScale = show ? 0f : 1f;
        float currentScale = _uiElementToHide != null ? _uiElementToHide.localScale.x : targetScale;

        float elapsed = 0f;

        while (elapsed < _transitionDuration)
        {
            // Usamos unscaledDeltaTime siguiendo la misma lógica de tus otros menús
            elapsed += Time.unscaledDeltaTime; 
            
            float t = Mathf.SmoothStep(0, 1, elapsed / _transitionDuration);
            
            // Interpolamos altura
            float h = Mathf.Lerp(currentHeight, targetHeight, t);
            _topBar.sizeDelta = new Vector2(_topBar.sizeDelta.x, h);
            _bottomBar.sizeDelta = new Vector2(_bottomBar.sizeDelta.x, h);

            // Interpolamos escala simultáneamente (si el elemento fue asignado)
            if (_uiElementToHide != null)
            {
                float s = Mathf.Lerp(currentScale, targetScale, t);
                _uiElementToHide.localScale = new Vector3(s, s, s);
            }

            yield return null;
        }

        // Aseguramos valores finales exactos
        _topBar.sizeDelta = new Vector2(_topBar.sizeDelta.x, targetHeight);
        _bottomBar.sizeDelta = new Vector2(_bottomBar.sizeDelta.x, targetHeight);
        
        if (_uiElementToHide != null)
        {
            _uiElementToHide.localScale = new Vector3(targetScale, targetScale, targetScale);
        }
    }
}
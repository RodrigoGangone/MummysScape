/// <summary>
/// SubmitEditableSlider
/// Slider que requiere pulsar Submit (X / A / Enter) para entrar en modo edición.
/// - Modo navegación: el movimiento sólo navega (usa la Navigation del Selectable).
/// - Modo edición: Left / Right modifican el valor; Up / Down se ignoran.
/// Sale del modo edición al perder selección.
/// </summary>
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Submit Editable Slider")]
public class SubmitEditableSlider : Slider, ISubmitHandler
{
    [Header("Edit Step (0 = usar 5% del rango)")]
    [Range(0f, 1f)]
    [SerializeField] public float _normalizedStep = 0.0f;

    [Header("Visual")]
    [SerializeField] public Graphic _handleGraphic;
    [SerializeField] public Color _editingColor = new (1f, 0f, 0.843f, 1f);

    private bool _isEditing;
    private Color _handleDefaultColor;

    protected override void Awake()
    {
        base.Awake();

        if (_handleGraphic == null && handleRect != null)
            _handleGraphic = handleRect.GetComponent<Graphic>();

        if (_handleGraphic != null)
            _handleDefaultColor = _handleGraphic.color;
    }

    #region ISubmitHandler

    public void OnSubmit(BaseEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
            return;

        _isEditing = !_isEditing;

        if (_handleGraphic != null)
            _handleGraphic.color = _isEditing ? _editingColor : _handleDefaultColor;
    }

    #endregion

    #region Navegación / edición

    public override void OnMove(AxisEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
        {
            base.OnMove(eventData);
            return;
        }

        if (_isEditing)
        {
            HandleMoveWhileEditing(eventData);
        }
        else
        {
            // FUERA de modo edición: dejamos toda la navegación
            // al comportamiento estándar de Selectable/Slider,
            // que respeta tu Navigation = Explicit.
            base.OnMove(eventData);
        }
    }

    private void HandleMoveWhileEditing(AxisEventData eventData)
    {
        switch (eventData.moveDir)
        {
            case MoveDirection.Left:
                StepValue(-1f);
                eventData.Use();       // consumimos el input, NO navegamos
                break;

            case MoveDirection.Right:
                StepValue(+1f);
                eventData.Use();
                break;

            case MoveDirection.Up:
            case MoveDirection.Down:
                // Ignoramos navegación vertical mientras editamos
                eventData.Use();
                break;

            case MoveDirection.None:
            default:
                break;
        }
    }

    private void StepValue(float directionSign)
    {
        float range = maxValue - minValue;

        float step;
        if (_normalizedStep <= 0f)
        {
            // por defecto: 5% del rango
            step = range * 0.05f;
        }
        else
        {
            step = range * _normalizedStep;
        }

        float newValue = value + step * directionSign;
        newValue = Mathf.Clamp(newValue, minValue, maxValue);

        value = newValue; // dispara onValueChanged como siempre
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);

        _isEditing = false;

        if (_handleGraphic != null)
            _handleGraphic.color = _handleDefaultColor;
    }

    #endregion
}
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(BoxCollider))]
public class Spears : MonoBehaviour
{
    public enum SpearState { Up, Down, Failed }
    
    [Header("State")]
    [SerializeField] private SpearState _currentState = SpearState.Up;

    private Animator _animatorController;
    private BoxCollider _collider;
    
    private void Start()
    {
        _animatorController = GetComponent<Animator>();
        _collider = GetComponent<BoxCollider>();
        
        // Forzamos el estado y la animación inicial
        _animatorController.SetTrigger("Up");
        _collider.enabled = true;
    }

    /// <summary>
    /// Vincular al evento OnActivated del ActionPressureButton.
    /// </summary>
    public void Interact()
    {
        if (_currentState == SpearState.Up)
        {
            Down();
        }
        else if (_currentState == SpearState.Failed)
        {
            DownFromHalf();
        }
    }

    /// <summary>
    /// Vincular al evento OnFailedActivate del ActionPressureButton.
    /// </summary>
    public void Failed()
    {
        // Solo entra en estado Failed si estaba arriba. 
        if (_currentState == SpearState.Up)
        {
            _currentState = SpearState.Failed;
            _animatorController.SetTrigger("Failed");
            
            // Nota: El BoxCollider se mantiene activado para que 
            // las rejas a la mitad sigan bloqueando el paso del jugador.
        }
    }

    /// <summary>
    /// Vincular al evento OnDeactivated del ActionPressureButton.
    /// </summary>
    public void ReturnUp()
    {
        // Solo permitimos que suba si actualmente está abajo.
        // Esto evita que al bajarse de un botón fallido, las rejas
        // intenten subir y pisen el estado Failed.
        if (_currentState == SpearState.Down)
        {
            _currentState = SpearState.Up;
            _collider.enabled = true;
            _animatorController.SetTrigger("Up");
        }
    }

    private void Down()
    {
        _currentState = SpearState.Down;
        _collider.enabled = false;
        _animatorController.SetTrigger("Down");
    }

    private void DownFromHalf()
    {
        _currentState = SpearState.Down;
        _collider.enabled = false;
        _animatorController.SetTrigger("DownFromHalf");
    }
}
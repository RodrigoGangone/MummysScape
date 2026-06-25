using System;
using UnityEngine;
using static PauseUtils;
using static Tags;
using static PlayerEnum.PlayerSize;

public abstract class BasePressureButton : MonoBehaviour, IPausable
{
    // 1. Definimos los estados posibles del botón
    protected enum ButtonState
    {
        Empty,
        ValidPress,
        InvalidPress
    }

    [Header("Base Detection Settings")] [SerializeField]
    protected LayerMask detectionLayer;

    [SerializeField] protected Vector3 boxSize = new(0.8f, 0.2f, 0.8f);
    [SerializeField] protected float checkDistance = 0.5f;
    [SerializeField] protected float timer;

    // 2. Reemplazamos el booleano por el estado actual
    protected ButtonState currentState = ButtonState.Empty;
    protected bool _paused;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    protected virtual void FixedUpdate()
    {
        ButtonState newState = CheckOccupancyState();

        // Si el estado no cambió, no hacemos nada
        if (newState == currentState) return;

        // 3. Ejecutamos el método correspondiente según el nuevo estado
        switch (newState)
        {
            case ButtonState.ValidPress:
                OnPress();
                break;
            case ButtonState.InvalidPress:
                OnFailedPress();
                break;
            case ButtonState.Empty:
                OnRelease();
                break;
        }

        currentState = newState;
    }

    private ButtonState CheckOccupancyState()
    {
        RaycastHit[] hits = Physics.BoxCastAll(transform.position, boxSize / 2, Vector3.up, Quaternion.identity,
            checkDistance, detectionLayer);

        bool foundInvalidMummy = false;

        foreach (var hit in hits)
        {
            // 1. Es una caja -> Presión válida inmediata
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer(BOX_TAG))
                return ButtonState.ValidPress;

            // 2. Es la Momia
            if (hit.collider.CompareTag(PLAYER_TAG))
            {
                var mummy = hit.collider.GetComponent<PlayerController>();
                if (mummy != null)
                {
                    // Si el tamaño es Normal, es válido y salimos del loop
                    if (mummy.Ctx.Model.Size == Normal)
                    {
                        return ButtonState.ValidPress;
                    }
                    else if (mummy.Ctx.Model.Size == Small)
                    {
                        // Registramos el fallo pero seguimos buscando 
                        // por si hay una caja también en el botón
                        foundInvalidMummy = true;
                    }
                }
            }
        }

        // Si encontramos una momia de tamaño incorrecto y NO había nada válido
        if (foundInvalidMummy) return ButtonState.InvalidPress;

        // Si no detectamos nada relevante
        return ButtonState.Empty;
    }

    protected virtual void OnPress()
    {
        if (_animator != null) _animator.SetBool("Pressed", true);
    }

    protected virtual void OnRelease()
    {
        if (_animator != null) _animator.SetBool("Pressed", false);
    }

    // 4. NUEVO MÉTODO VIRTUAL
    protected virtual void OnFailedPress()
    {
        if (_animator != null) _animator.SetTrigger("Failed");
    }
    
    public void OnPauseChanged(bool paused)
    {
        _paused = paused;

        if (_animator) _animator.enabled = !paused;
    }

    private void OnDrawGizmos()
    {
        // Actualizamos los Gizmos para reflejar los 3 estados
        Gizmos.color = currentState == ButtonState.ValidPress
            ? Color.green
            : (currentState == ButtonState.InvalidPress ? Color.yellow : Color.red);
        Gizmos.DrawWireCube(transform.position + Vector3.up * checkDistance, boxSize);
    }
    
    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
    }
}
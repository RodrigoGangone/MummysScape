using System;
using System.Collections.Generic;
using UnityEngine;
using static PauseUtils;
using static Layers;
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

        if (newState == currentState) return;

        if (currentState == ButtonState.ValidPress && newState == ButtonState.InvalidPress)
            OnRelease();

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
        Vector3 boxCenter = transform.position + (Vector3.up * checkDistance);
        Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize / 2f, Quaternion.identity, detectionLayer);

        int totalWeight = 0;
        
        HashSet<Transform> processedObjects = new HashSet<Transform>();

        foreach (var col in colliders)
        {
            Transform rootObj = col.transform.root;

            if (processedObjects.Contains(rootObj)) continue;
            
            processedObjects.Add(rootObj);

            int hitLayer = rootObj.gameObject.layer;

            // 1. Es una caja -> Peso pesado (2)
            if (hitLayer == LayerMask.NameToLayer(INTERACTABLE_LAYER))
            {
                totalWeight += 2;
            }
            // 2. Es una venda -> Peso ligero (1)
            else if (hitLayer == LayerMask.NameToLayer(BANDAGE_MOUND_LAYER))
            {
                totalWeight += 1;
            }
            // 3. Es la Momia
            else if (hitLayer == LayerMask.NameToLayer(PLAYER_LAYER))
            {
                // Buscamos en el root (o en los hijos por si el script está en otro lado)
                var mummy = rootObj.GetComponentInChildren<PlayerController>();
                if (mummy != null)
                {
                    if (mummy.Ctx.Model.Size == Normal)
                    {
                        totalWeight += 2;
                    }
                    else if (mummy.Ctx.Model.Size == Small)
                    {
                        totalWeight += 1;
                    }
                }
            }

            // Optimización: Si ya llegamos al peso requerido (2), salimos del loop al toque
            if (totalWeight >= 2)
            {
                return ButtonState.ValidPress;
            }
        }

        // Si salimos del loop y el peso quedó en 1
        if (totalWeight == 1) return ButtonState.InvalidPress;

        // Si no detectamos nada
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
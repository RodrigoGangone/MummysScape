using System;
using UnityEngine;

public class LevelTile : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string displayName;
    [SerializeField] private string levelId;
    [SerializeField] private int buildIndex;

    [Header("Navigation")]
    [SerializeField] private Transform selectionPoint;

    [Tooltip("Anchor donde se posiciona el EntryRig genérico.")]
    [SerializeField] private Transform entryPoint;

    [Tooltip("Lugar hacia el que camina el actor durante la entrada.")]
    [SerializeField] private Transform actorEntryTarget;

    [SerializeField] private Transform focusPoint;
    
    [Header("Components")]
    [SerializeField] private SelectorNodeVisuals visuals;
    [SerializeField] private LevelGemView gemView;

    private LevelTileState _state;
    private bool _isSelected;

    // Se utiliza únicamente durante una Timeline de reveal.
    private int _currentGemRevealIndex;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? levelId
            : displayName;
    public string LevelId => levelId;
    public int BuildIndex => buildIndex;

    public Transform SelectionPoint =>
        selectionPoint != null
            ? selectionPoint
            : transform;
    public Transform ActorEntryTarget =>
        actorEntryTarget != null
            ? actorEntryTarget
            : EntryPoint;
    public Transform FocusPoint =>
        focusPoint != null
            ? focusPoint
            : transform;
    public Transform EntryPoint =>
        entryPoint != null
            ? entryPoint
            : SelectionPoint;
    public LevelTileState State => _state;

    public bool IsSelected => _isSelected;
    public bool IsPlayable => _state.IsPlayable;

    // Alias temporal por compatibilidad con cualquier script externo
    // que todavía estuviera utilizando PlayerPos.
    public Transform PlayerPos => SelectionPoint;

    private void Awake()
    {
        if (visuals == null)
            visuals = GetComponent<SelectorNodeVisuals>();

        if (gemView == null)
            gemView = GetComponent<LevelGemView>();
    }

    /// <summary>
    /// Recibe el estado ya calculado por SelectorProgression.
    /// LevelTile NO consulta Save.
    /// </summary>
    public void ApplyState(LevelTileState state)
    {
        _state = state;

        if (visuals != null)
            visuals.ApplyLevelState(state);

        if (gemView != null)
            gemView.ApplyState(state);
    }

    /// <summary>
    /// Cambia solamente la representación de selección.
    /// No modifica progreso.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (_isSelected == selected)
            return;

        _isSelected = selected;

        if (visuals != null)
            visuals.SetSelected(selected);
    }

    // ============================================================
    // REVEAL API
    //
    // Estos métodos quedan deliberadamente en LevelTile porque
    // pueden ser invocados por Signals de Timeline.
    //
    // La implementación real está delegada a componentes visuales.
    // ============================================================

    public void PrepareReveal()
    {
        _currentGemRevealIndex = 0;

        if (gemView != null)
            gemView.HideAll();
    }

    public void AnimateGlowBlink()
    {
        if (visuals != null)
            visuals.AnimateGlowBlink();
    }

    public void AnimateCutOffReveal()
    {
        if (visuals != null)
            visuals.AnimateCutOffReveal();
    }

    /// <summary>
    /// Compatible con Signals existentes de Timeline.
    /// Cada Signal avanza un slot.
    /// </summary>
    public void AnimateNextGemReveal()
    {
        if (_state.Gems == null)
            return;

        if (_currentGemRevealIndex >= _state.Gems.Length)
            return;

        int index = _currentGemRevealIndex;
        _currentGemRevealIndex++;

        if (!_state.Gems[index])
            return;

        if (gemView != null)
            gemView.RevealCollectedGem(index);
    }

    public void CompleteRevealVisuals()
    {
        LevelTileState finalState = _state;
        finalState.RevealPending = false;

        ApplyState(finalState);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(levelId))
            levelId = gameObject.name;
    }
#endif
}
using System;
using UnityEngine;

public class ZoneTile : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string zoneId = "zone_02";
    [SerializeField] private string displayName = "SIGUIENTE ZONA";
    [Header("Navigation")]
    [Tooltip("Build Index del Selector de la siguiente zona.")]
    [SerializeField] private int targetSelectorBuildIndex;

    [Tooltip("Cantidad de gemas necesarias para habilitar este portal.")]
    [SerializeField] private int requiredGems;

    [Header("Points")]
    [Tooltip("Lugar donde se posiciona el actor al seleccionar el Portal Final.")]
    [SerializeField] private Transform selectionPoint;

    [Tooltip("Punto futuro para FocusManager / reveals.")]
    [SerializeField] private Transform focusPoint;

    [Header("Components")]
    [SerializeField] private SelectorNodeVisuals visuals;
    [SerializeField] private SelectorEntrySequence entrySequence;

    private ZoneTileState _state;
    private bool _isSelected;
    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? zoneId
            : displayName;
    public string ZoneId => zoneId;

    public int TargetSelectorBuildIndex =>
        targetSelectorBuildIndex;

    public int RequiredGems =>
        requiredGems;

    public Transform SelectionPoint =>
        selectionPoint != null
            ? selectionPoint
            : transform;

    public Transform FocusPoint =>
        focusPoint != null
            ? focusPoint
            : transform;

    public ZoneTileState State =>
        _state;

    /// <summary>
    /// Podemos movernos hasta el portal una vez derrotado el Boss,
    /// aunque todavía falten gemas.
    /// </summary>
    public bool CanBeReached =>
        _state.BossCompleted;

    /// <summary>
    /// Para entrar deben cumplirse Boss + Gems.
    /// </summary>
    public bool IsUnlocked =>
        _state.IsUnlocked;

    public bool IsSelected =>
        _isSelected;

    private void Awake()
    {
        if (visuals == null)
            visuals = GetComponent<SelectorNodeVisuals>();

        if (entrySequence == null)
            entrySequence = GetComponent<SelectorEntrySequence>();
    }

    public void ApplyState(ZoneTileState state)
    {
        _state = state;

        if (visuals != null)
            visuals.ApplyZoneState(state);
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected)
            return;

        _isSelected = selected;

        if (visuals != null)
            visuals.SetSelected(selected);
    }

    public void PlayEntrySequence(Action onComplete)
    {
        if (!IsUnlocked)
        {
            Debug.LogWarning(
                $"[ZoneTile] El portal '{zoneId}' todavía está bloqueado."
            );

            return;
        }

        if (entrySequence != null)
        {
            entrySequence.Play(onComplete);
            return;
        }

        onComplete?.Invoke();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        requiredGems = Mathf.Max(0, requiredGems);
    }
#endif
}
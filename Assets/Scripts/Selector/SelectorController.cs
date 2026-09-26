using System;
using UnityEngine;

public class SelectorController : MonoBehaviour
{
    [Header("Levels")]
    [Tooltip("Levels EN ORDEN visual: izquierda -> derecha. El último debe ser el Boss.")]
    [SerializeField]
    private LevelTile[] levels;

    [Header("Final Portal")]
    [Tooltip("Portal ubicado inmediatamente después del Boss.")]
    [SerializeField]
    private ZoneTile finalPortal;

    [Header("Systems")]
    [SerializeField]
    private SelectorProgression progression;

    [SerializeField]
    private SelectorMovement movement;

    [SerializeField]
    private SelectorNavigation navigation;
    [SerializeField]
    private SelectorEntryController entryController;
    [Header("Input")]
    [SerializeField]
    private string horizontalAxis = "Horizontal";

    [SerializeField]
    private string acceptButton = "Accept";

    [SerializeField]
    [Range(0.1f, 1f)]
    private float horizontalThreshold = 0.5f;

    [Header("Startup")]
    [SerializeField]
    private bool startAtLastPlayedLevel = true;

    [SerializeField]
    private bool startAtFirstLevelForDebug = false;

    private int _currentIndex;

    private SelectorMode _mode =
        SelectorMode.Initializing;

    private bool _horizontalReady = true;

    // ============================================================
    // PUBLIC STATE
    // ============================================================
    public event Action OnSelectionChanged;
    
    public int CurrentIndex =>
        _currentIndex;

    public SelectorMode Mode =>
        _mode;

    public int FinalPortalIndex =>
        levels != null
            ? levels.Length
            : 0;

    public bool IsFinalPortalSelected =>
        finalPortal != null &&
        _currentIndex == FinalPortalIndex;

    public LevelTile CurrentLevel
    {
        get
        {
            if (IsFinalPortalSelected)
                return null;

            if (!IsLevelIndexValid(
                _currentIndex))
            {
                return null;
            }

            return levels[_currentIndex];
        }
    }

    public ZoneTile CurrentPortal =>
        IsFinalPortalSelected
            ? finalPortal
            : null;

    // ============================================================
    // UNITY
    // ============================================================

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (_mode !=
            SelectorMode.Navigating)
        {
            return;
        }

        HandleHorizontalInput();
        HandleAcceptInput();
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private void Initialize()
    {
        _mode =
            SelectorMode.Initializing;

        if (!ValidateReferences())
            return;

        RefreshProgression();

        _currentIndex =
            ResolveInitialIndex();

        ApplySelectionInstant();

        _mode =
            SelectorMode.Navigating;

        NotifySelectionChanged();
    }

    public void RefreshProgression()
    {
        LevelTile previousLevel = null;

        for (int i = 0;
             i < levels.Length;
             i++)
        {
            LevelTile level =
                levels[i];

            if (level == null)
                continue;

            LevelTileState state =
                progression.EvaluateLevel(
                    level,
                    previousLevel
                );

            level.ApplyState(state);

            previousLevel = level;
        }

        // El último LevelTile es el Boss.
        LevelTile boss =
            GetBossLevel();

        if (finalPortal != null &&
            boss != null)
        {
            ZoneTileState portalState =
                progression.EvaluateFinalPortal(
                    finalPortal,
                    boss
                );

            finalPortal.ApplyState(
                portalState
            );
        }
    }

    private LevelTile GetBossLevel()
    {
        if (levels == null ||
            levels.Length == 0)
        {
            return null;
        }

        return levels[
            levels.Length - 1
        ];
    }

    // ============================================================
    // START NODE
    // ============================================================

    private int ResolveInitialIndex()
    {
        if (levels == null ||
            levels.Length == 0)
        {
            return 0;
        }

        if (startAtFirstLevelForDebug)
            return 0;

        if (startAtLastPlayedLevel)
        {
            int lastBuildIndex =
                Save.GetLastLevelPlayed();

            int savedIndex =
                FindLevelIndexByBuildIndex(
                    lastBuildIndex
                );

            if (savedIndex >= 0 &&
                levels[savedIndex].IsPlayable)
            {
                return savedIndex;
            }
        }

        for (int i = levels.Length - 1;
             i >= 0;
             i--)
        {
            if (levels[i] != null &&
                levels[i].IsPlayable)
            {
                return i;
            }
        }

        return 0;
    }

    // ============================================================
    // INPUT
    // ============================================================

    private void HandleHorizontalInput()
    {
        float horizontal =
            Input.GetAxisRaw(
                horizontalAxis
            );

        if (!_horizontalReady)
        {
            if (Mathf.Abs(horizontal) < 0.2f)
                _horizontalReady = true;

            return;
        }

        if (horizontal >
            horizontalThreshold)
        {
            _horizontalReady = false;
            TryMove(1);

            return;
        }

        if (horizontal <
            -horizontalThreshold)
        {
            _horizontalReady = false;
            TryMove(-1);
        }
    }

    private void HandleAcceptInput()
    {
        if (Input.GetButtonDown(
            acceptButton))
        {
            TryEnterCurrentNode();
        }
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    private void TryMove(int direction)
    {
        if (_mode !=
            SelectorMode.Navigating)
        {
            return;
        }

        int targetIndex =
            _currentIndex +
            direction;

        if (!IsNodeIndexValid(
            targetIndex))
        {
            return;
        }

        // --------------------------------------------
        // TARGET = FINAL PORTAL
        // --------------------------------------------

        if (IsPortalIndex(targetIndex))
        {
            if (finalPortal == null)
                return;

            // Boss todavía no completado:
            // no dejamos avanzar hasta el Portal.
            if (!finalPortal.CanBeReached)
            {
                Debug.Log(
                    "[Selector] Portal Final inaccesible: Boss no completado."
                );

                return;
            }

            SelectNode(targetIndex);
            return;
        }

        // --------------------------------------------
        // TARGET = LEVEL
        // --------------------------------------------

        LevelTile target =
            levels[targetIndex];

        if (target == null)
            return;

        if (!target.IsPlayable)
        {
            Debug.Log(
                $"[Selector] '{target.LevelId}' está bloqueado."
            );

            return;
        }

        SelectNode(targetIndex);
    }

    private void SelectNode(
        int targetIndex)
    {
        if (targetIndex ==
            _currentIndex)
        {
            return;
        }

        SetNodeSelected(
            _currentIndex,
            false
        );

        _currentIndex =
            targetIndex;

        SetNodeSelected(
            _currentIndex,
            true
        );

        Transform targetPoint =
            GetNodeSelectionPoint(
                _currentIndex
            );

        if (targetPoint == null)
        {
            _mode =
                SelectorMode.Navigating;

            return;
        }

        _mode =
            SelectorMode.Moving;

        movement.MoveTo(
            targetPoint,
            HandleMovementCompleted
        );
    }

    private void HandleMovementCompleted()
    {
        if (_mode !=
            SelectorMode.Moving)
        {
            return;
        }

        _mode =
            SelectorMode.Navigating;

        NotifySelectionChanged();
    }
    
    private void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke();
    }

    // ============================================================
    // ACCEPT
    // ============================================================

    private void TryEnterCurrentNode()
    {
        if (_mode !=
            SelectorMode.Navigating)
        {
            return;
        }

        if (IsFinalPortalSelected)
        {
            TryEnterFinalPortal();
            return;
        }

        TryEnterCurrentLevel();
    }

    private void TryEnterCurrentLevel()
    {
        LevelTile level =
            CurrentLevel;

        if (level == null ||
            !level.IsPlayable)
        {
            return;
        }

        _mode =
            SelectorMode.Entering;

        if (entryController != null)
        {
            entryController.Play(
                level,
                () =>
                {
                    if (navigation != null)
                    {
                        navigation.EnterLevel(
                            level
                        );
                    }
                    else
                    {
                        Debug.LogError(
                            "[SelectorController] Falta SelectorNavigation."
                        );

                        _mode =
                            SelectorMode.Navigating;
                    }
                }
            );

            return;
        }

        // Fallback por seguridad.
        if (navigation != null)
        {
            navigation.EnterLevel(level);
        }
        else
        {
            _mode =
                SelectorMode.Navigating;
        }
    }

    private void TryEnterFinalPortal()
    {
        if (finalPortal == null)
            return;

        if (!finalPortal.IsUnlocked)
        {
            ZoneTileState state =
                finalPortal.State;

            Debug.Log(
                $"[Selector] Portal bloqueado. Boss: {state.BossCompleted} | Gems: {state.CurrentGems}/{state.RequiredGems}"
            );

            return;
        }

        _mode =
            SelectorMode.Entering;

        finalPortal.PlayEntrySequence(
            () =>
            {
                if (navigation != null)
                {
                    navigation.EnterNextZone(
                        finalPortal
                    );
                }
                else
                {
                    _mode =
                        SelectorMode.Navigating;
                }
            }
        );
    }

    // ============================================================
    // SELECTION VISUALS
    // ============================================================

    private void ApplySelectionInstant()
    {
        for (int i = 0;
             i < levels.Length;
             i++)
        {
            if (levels[i] != null)
                levels[i].SetSelected(false);
        }

        if (finalPortal != null)
            finalPortal.SetSelected(false);

        SetNodeSelected(
            _currentIndex,
            true
        );

        Transform point =
            GetNodeSelectionPoint(
                _currentIndex
            );

        if (point != null)
        {
            movement.SnapTo(
                point
            );
        }
    }

    private void SetNodeSelected(
        int index,
        bool selected)
    {
        if (IsPortalIndex(index))
        {
            if (finalPortal != null)
            {
                finalPortal.SetSelected(
                    selected
                );
            }

            return;
        }

        if (!IsLevelIndexValid(index))
            return;

        if (levels[index] != null)
        {
            levels[index].SetSelected(
                selected
            );
        }
    }

    private Transform GetNodeSelectionPoint(
        int index)
    {
        if (IsPortalIndex(index))
        {
            return finalPortal != null
                ? finalPortal.SelectionPoint
                : null;
        }

        if (!IsLevelIndexValid(index))
            return null;

        return levels[index] != null
            ? levels[index].SelectionPoint
            : null;
    }

    // ============================================================
    // INDEX HELPERS
    // ============================================================

    private bool IsPortalIndex(
        int index)
    {
        return finalPortal != null &&
               index == FinalPortalIndex;
    }

    private bool IsLevelIndexValid(
        int index)
    {
        return levels != null &&
               index >= 0 &&
               index < levels.Length;
    }

    private bool IsNodeIndexValid(
        int index)
    {
        if (index < 0)
            return false;

        int maxIndex =
            finalPortal != null
                ? FinalPortalIndex
                : levels.Length - 1;

        return index <= maxIndex;
    }

    private int FindLevelIndexByBuildIndex(
        int buildIndex)
    {
        if (buildIndex < 0 ||
            levels == null)
        {
            return -1;
        }

        for (int i = 0;
             i < levels.Length;
             i++)
        {
            if (levels[i] != null &&
                levels[i].BuildIndex ==
                buildIndex)
            {
                return i;
            }
        }

        return -1;
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    private bool ValidateReferences()
    {
        if (levels == null ||
            levels.Length == 0)
        {
            Debug.LogError(
                "[SelectorController] No hay LevelTiles."
            );

            return false;
        }

        if (progression == null)
        {
            Debug.LogError(
                "[SelectorController] Falta SelectorProgression."
            );

            return false;
        }

        if (movement == null)
        {
            Debug.LogError(
                "[SelectorController] Falta SelectorMovement."
            );

            return false;
        }

        if (navigation == null)
        {
            Debug.LogError(
                "[SelectorController] Falta SelectorNavigation."
            );

            return false;
        }

        return true;
    }
}
using static PlayerEnum;

/// <summary> 
/// Núcleo de Datos: Gestiona el inventario de vendas y deriva automáticamente el tamaño (Size) del jugador, 
/// disparando eventos globales cada vez que cambian las estadísticas o la forma del personaje. 
/// </summary>
public sealed class PlayerModel
{
    private const int MinBandages = 0;
    private const int MaxBandages = 2;
    
    private const int MinSpecialBandages = 0;
    private const int MaxSpecialBandages = 1;
    
    private readonly GameEvent _onBandagesCountChanged;
    private readonly GameEvent _onSizeChanged;

    public int MinBandagesValue => MinBandages;
    public int MaxBandagesValue => MaxBandages;
    public int Bandages { get; private set; }
    public int SpecialBandages { get; private set; }
    
    // 1. Ahora el Size depende de ambos valores
    public PlayerSize Size => MapSize(Bandages, SpecialBandages); 
    
    private readonly ProgressionSettings _progression;
    
    public PlayerModel(GameEvent onBandagesCountChanged, GameEvent onSizeChanged, ProgressionSettings progression)
    {
        _onBandagesCountChanged = onBandagesCountChanged;
        _onSizeChanged          = onSizeChanged;
        _progression            = progression;
        
        GameEventManager.Instance.playerEvents.OnShoot.Register((() => { TryConsumeBandage(); }));
        GameEventManager.Instance.playerEvents.OnHit.Register((() => { TryConsumeBandage(Bandages); }));
        GameEventManager.Instance.playerEvents.OnEmpowered.Register((() => { TryConsumeBandage(Bandages); }));
        
        Bandages = Clamp(MaxBandages, MinBandages, MaxBandages);
    }
    
    public bool CanUseAbility(PlayerStateId state) => _progression.IsUnlocked(state);

    public bool TryConsumeBandage(int amount = 1)
    {
        if (amount <= 0 || Bandages < amount) return false;
        SetBandages(Bandages - amount);
        return true;
    }

    public void AddBandages(int amount = 1)
    {
        if (amount <= 0) return;
        SetBandages(Bandages + amount);
    }
    
    // 2. Modificamos esto para que pase por un setter propio
    public void AddSpecialBandage(int value) => SetSpecialBandages(SpecialBandages + value);

    // 3. Nuevo setter para SpecialBandages que verifica si el tamaño (Size) cambió al obtenerla
    private void SetSpecialBandages(int target)
    {
        int clamped = Clamp(target, MinSpecialBandages, MaxSpecialBandages);
        if (clamped == SpecialBandages) return;

        var oldSize = Size; // Lee el estado actual

        SpecialBandages = clamped;
        var newSize = Size; // Lee el nuevo estado

        // Si conseguir (o perder) la venda especial cambia el Size a Empowered, disparamos el evento
        if (newSize != oldSize)
            _onSizeChanged?.Raise(newSize);
    }

    private void SetBandages(int target)
    {
        int clamped = Clamp(target, MinBandages, MaxBandages);
        if (clamped == Bandages) return;

        var oldSize = Size;

        Bandages = clamped;
        var newSize = Size;

        _onBandagesCountChanged?.Raise(Bandages);

        if (newSize != oldSize)
            _onSizeChanged?.Raise(newSize);
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    private static PlayerSize MapSize(int bandages, int specialBandages) => bandages switch
    {
        2 => specialBandages > 0 ? PlayerSize.Empowered : PlayerSize.Normal,
        1 => PlayerSize.Small,
        _ => PlayerSize.Head
    };
}
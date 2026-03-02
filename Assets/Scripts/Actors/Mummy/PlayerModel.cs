using static PlayerEnum;

/// <summary> 
/// Núcleo de Datos: Gestiona el inventario de vendas y deriva automáticamente el tamaño (Size) del jugador, 
/// disparando eventos globales cada vez que cambian las estadísticas o la forma del personaje. 
/// </summary>

public sealed class PlayerModel
{
    private const int MinBandages = 0;
    private const int MaxBandages = 2;

    private readonly GameEvent _onBandagesCountChanged;
    private readonly GameEvent _onSizeChanged;

    public int MinBandagesValue => MinBandages;
    public int MaxBandagesValue => MaxBandages;
    public int Bandages { get; private set; }
    public PlayerSize Size => MapSize(Bandages);
    public PlayerModel(GameEvent onBandagesCountChanged, GameEvent onSizeChanged)
    {
        _onBandagesCountChanged = onBandagesCountChanged;
        _onSizeChanged          = onSizeChanged;
        
        GameEventManager.Instance.playerEvents.OnShoot.Register((() => { TryConsumeBandage(); }));
        GameEventManager.Instance.playerEvents.OnHit.Register((() => { TryConsumeBandage(Bandages); }));
        
        Bandages = Clamp(MaxBandages, MinBandages, MaxBandages);
    }

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

    private void SetBandages(int target)
    {
        int clamped = Clamp(target, MinBandages, MaxBandages);
        if (clamped == Bandages) return;

        int oldBand = Bandages;
        var oldSize = MapSize(oldBand);

        Bandages = clamped;
        var newSize = MapSize(Bandages);

        _onBandagesCountChanged?.Raise(Bandages);

        if (newSize != oldSize)
            _onSizeChanged?.Raise(newSize);
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    private static PlayerSize MapSize(int bandages) => bandages switch
    {
        2 => PlayerSize.Normal,
        1 => PlayerSize.Small,
        _ => PlayerSize.Head
    };
}
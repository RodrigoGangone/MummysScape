/// <summary>
/// PlayerModel
/// Vida = cantidad de vendas (0..2). El tamaño (Size) se deriva:
/// 2 => Normal, 1 => Small, 0 => Head.
/// Emite OnBandagesChanged(old,current) y OnSizeChanged(newSize).
/// </summary>

using System;
using static PlayerEnum;
public sealed class PlayerModel
{
    private const int MinBandages = 0;
    private const int MaxBandages = 2 ;

    public int Bandages { get; private set; }
    public PlayerSize Size => MapSize(Bandages);

    public event Action<int> OnBandagesChanged;
    public event Action<PlayerSize> OnSizeChanged;

    public PlayerModel()
    {
        Bandages = Clamp(MaxBandages, MinBandages, MaxBandages);
        GameEventManager.Instance.playerEvents.OnBandageCount.Raise(Bandages);
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
        
        GameEventManager.Instance.playerEvents.OnBandageCount.Raise(Bandages);
        if (newSize != oldSize) OnSizeChanged?.Invoke(newSize);
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    private static PlayerSize MapSize(int bandages) => bandages switch
    {
        2 => PlayerSize.Normal,
        1 => PlayerSize.Small,
        _ => PlayerSize.Head
    };
}
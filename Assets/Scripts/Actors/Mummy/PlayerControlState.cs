public static class PlayerControlState
{
    public static bool Paused { get; private set; }
    public static bool Locked { get; private set; }

    public static bool AnyBlocked => Paused || Locked;

    public static void SetPause(bool v) => Paused = v;
    public static void SetLock(bool v) => Locked = v;
}
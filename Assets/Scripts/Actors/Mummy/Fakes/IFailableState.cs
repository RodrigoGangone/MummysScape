using static PlayerEnum;

public interface IFailableState
{
    void OnTransitionDenied(PlayerSize currentSize);
}
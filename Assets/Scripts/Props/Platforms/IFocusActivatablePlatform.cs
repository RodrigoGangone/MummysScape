/// <summary>Permite que un Eagle active varias plataformas usando un único foco.</summary>
public interface IFocusActivatablePlatform
{
    bool IsPreparingActivation { get; }
    void StartActionWithoutFocus(bool allowMovementDuringFocus);
    void EndActivationFocus();
}

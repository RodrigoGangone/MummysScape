using UnityEngine;

public class GroupPressureButton : BasePressureButton
{
    public GroupPressureManager manager;

    protected override void OnPress() => manager.NotifyChange();
    protected override void OnRelease() => manager.NotifyChange();

    public bool IsActive => isOccupied;
}
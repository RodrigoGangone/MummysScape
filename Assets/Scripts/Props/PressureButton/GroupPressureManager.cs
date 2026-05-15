using System.Linq;
using UnityEngine;

public class GroupPressureManager : MonoBehaviour
{
    public GroupPressureButton[] buttons;
    public UnityEngine.Events.UnityEvent OnAllActive;

    public void NotifyChange()
    {
        if (buttons.All(b => b.IsActive)) OnAllActive.Invoke();
    }
}
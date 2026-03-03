using UnityEngine;
using UnityEngine.InputSystem;

public class JoystickRumble : MonoBehaviour
{
    private void OnEnable()
    {
        GameEventManager.Instance.levelEvents.OnRumbleLow.Register<float, float>(HighVibrate);
        GameEventManager.Instance.levelEvents.OnRumbleHigh.Register<float, float>(LowVibrate);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnRumbleLow.Unregister<float, float>(HighVibrate);
        GameEventManager.Instance.levelEvents.OnRumbleHigh.Unregister<float, float>(LowVibrate);
    }

    private void HighVibrate(float highFrequency, float duration)
    {
        if (Gamepad.current == null)
            return;

        Gamepad.current.SetMotorSpeeds(0, highFrequency);
        Invoke(nameof(StopVibration), duration);
    }

    private void LowVibrate(float lowFrequency, float duration)
    {
        if (Gamepad.current == null)
            return;

        Gamepad.current.SetMotorSpeeds(lowFrequency, 0);
        Invoke(nameof(StopVibration), duration);
    }

    private void StopVibration()
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}
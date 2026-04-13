using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class JoystickRumble : MonoBehaviour
{
    private Coroutine rumbleCoroutine;

    private void OnEnable()
    {
        // Corregido el registro invertido
        GameEventManager.Instance.levelEvents.OnRumbleLow.Register<float, float>(LowVibrate);
        GameEventManager.Instance.levelEvents.OnRumbleHigh.Register<float, float>(HighVibrate);
    }

    private void OnDisable()
    {
        GameEventManager.Instance.levelEvents.OnRumbleLow.Unregister<float, float>(LowVibrate);
        GameEventManager.Instance.levelEvents.OnRumbleHigh.Unregister<float, float>(HighVibrate);
        StopVibration(); // Seguridad: dejar de vibrar si se destruye el objeto
    }

    private void HighVibrate(float highFrequency, float duration)
    {
        StartRumble(0, highFrequency, duration);
    }

    private void LowVibrate(float lowFrequency, float duration)
    {
        StartRumble(lowFrequency, 0, duration);
    }

    private void StartRumble(float lowFreq, float highFreq, float duration)
    {
        if (Gamepad.current == null) return;

        // Si ya hay una vibración en curso, la detenemos para empezar la nueva
        if (rumbleCoroutine != null)
            StopCoroutine(rumbleCoroutine);

        rumbleCoroutine = StartCoroutine(RumbleRoutine(lowFreq, highFreq, duration));
    }

    private IEnumerator RumbleRoutine(float lowFreq, float highFreq, float duration)
    {
        Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);

        yield return new WaitForSeconds(duration);

        Gamepad.current.SetMotorSpeeds(0f, 0f);
        rumbleCoroutine = null;
    }

    public void StopVibration()
    {
        if (rumbleCoroutine != null) StopCoroutine(rumbleCoroutine);
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    public void HighRumbleBasic() => GameEventManager.Instance.levelEvents.OnRumbleHigh.Raise(0.9f, 1f);
    public void LowRumbleBasic() => GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(0.5f, 0.25f);
}
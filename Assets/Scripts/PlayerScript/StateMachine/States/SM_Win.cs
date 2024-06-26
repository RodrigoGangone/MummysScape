using UnityEngine;

public class SM_Win : State
{
    private Player _player;
    private float _materialTransitionDuration = 6f; // Duración en segundos para cambiar el material
    private float _rotationSpeed = 90f; // Velocidad de rotación en grados por segundo
    private float _timerToChangeLvl = 0f; // Timer para cambiar de nivel

    private float _startTime;
    private bool _materialChanged;
    private bool _rotationStarted;

    public SM_Win(Player player)
    {
        _player = player;
    }

    public override void OnEnter()
    {
        _startTime = Time.timeSinceLevelLoad;
        _materialChanged = false;
        _rotationStarted = false;

        // Ejecutar el trigger de animación "Win"
        _player._viewPlayer.PLAY_ANIM_TRIGGER("Win");
    }

    public override void OnExit()
    {
        // Restaurar cualquier cambio necesario al salir del estado Win
    }

    public override void OnUpdate()
    {
        // Cambiar progresivamente la variable del material
        if (!_materialChanged)
        {
            float t = Mathf.Clamp01((Time.timeSinceLevelLoad - _startTime) / _materialTransitionDuration);
            float value = Mathf.Lerp(1f, 0f, t);
            _player._viewPlayer.playerMat.SetFloat("_CutoffLight", value);

            if (t >= 1f)
            {
                _materialChanged = true;
            }
        }

        // Rotar progresivamente hacia la cámara
        if (!_rotationStarted)
        {
            RotateTowardsCamera();
        }
        
        // Aquí podrías agregar lógica adicional después de que se complete la transición y rotación
        if (_materialChanged && _rotationStarted)
        {
            _timerToChangeLvl += Time.deltaTime;

            if (_timerToChangeLvl >= 2f)
                GameManager.Instance.ChangeScene();
        }
    }

    public override void OnFixedUpdate()
    {
    }

    private void RotateTowardsCamera()
    {
        Vector3 direction = (_player._cameraTransform.position - _player.transform.position).normalized;
        direction.y = 0; // Solo rotar en el plano XZ (horizontal)

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Interpolar la rotación de manera suave
        _player.transform.rotation = Quaternion.RotateTowards(_player.transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

        // Verificar si hemos alcanzado la rotación deseada
        if (Quaternion.Angle(_player.transform.rotation, targetRotation) < 0.1f)
        {
            _rotationStarted = true;
        }
    }
}

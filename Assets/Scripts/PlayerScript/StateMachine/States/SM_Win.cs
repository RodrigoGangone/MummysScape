using UnityEngine;

public class SM_Win : State
{
    private Player _player;
    private float _materialTransitionDuration = 2f; // Duración en segundos para cambiar el material
    private float _rotationSpeed = 90f; // Velocidad de rotación en grados por segundo

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
    }

    public override void OnUpdate()
    {
        if (!_materialChanged)
        {
            float elapsedTime = Time.timeSinceLevelLoad - _startTime;
            float t1 = Mathf.Clamp01(elapsedTime / _materialTransitionDuration);
            float body = Mathf.Lerp(1, 0, t1);

            _player._viewPlayer.SetValueMaterial(body, 1);

            if (body == 0)
            {
                float t2 = Mathf.Clamp01((elapsedTime - _materialTransitionDuration) / _materialTransitionDuration);
                _player._viewPlayer.SetValueMaterial(0, Mathf.Lerp(1, 0, t2));

                if (t2 >= 1f)
                {
                    _materialChanged = true;
                }
            }
        }

        // Rotar progresivamente hacia la cámara
        if (!_rotationStarted)
        {
            RotateTowardsCamera();
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
        _player.transform.rotation =
            Quaternion.RotateTowards(_player.transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

        // Verificar si hemos alcanzado la rotación deseada
        if (Quaternion.Angle(_player.transform.rotation, targetRotation) < 0.1f)
        {
            _rotationStarted = true;
        }
    }
}
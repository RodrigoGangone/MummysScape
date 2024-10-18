using System.Collections;
using UnityEngine;

public class SM_Win : State
{
    private Player _player;
    private float _materialTransitionDuration = 2f; // Duración en segundos para cambiar el material
    private float _rotationSpeed = 90f; // Velocidad de rotación en grados por segundo

    private bool _materialChanged;
    private bool _rotationStarted;

    private float _elapsedTime;
    
    private Coroutine _disappearCoroutine;

    public SM_Win(Player player)
    {
        _player = player;
    }

    public override void OnEnter()
    {
        _player._viewPlayer.PLAY_ANIM_TRIGGER("Win");
        
        _materialChanged = false;
        _rotationStarted = false;   
        
        _disappearCoroutine = _player.StartCoroutine(MaterialTransitionCoroutine());
    }

    public override void OnExit()
    {
        _player.StopCoroutine(_disappearCoroutine);
    }

    public override void OnUpdate()
    {
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
    
    private IEnumerator MaterialTransitionCoroutine()
    {
        while (!_materialChanged)
        {
            _elapsedTime += Time.deltaTime;
            float t1 = Mathf.Clamp01(_elapsedTime / _materialTransitionDuration);
            float body = Mathf.Lerp(1, -0.5f, t1);

            _player._viewPlayer.SetValueMaterial(body, 1);

            if (body == -0.5f)
            {
                float t2 = Mathf.Clamp01((_elapsedTime - _materialTransitionDuration) / _materialTransitionDuration);
                _player._viewPlayer.SetValueMaterial(-0.5f, Mathf.Lerp(1, -0.5f, t2));

                if (t2 >= 1f)
                {
                    _materialChanged = true;
                }
            }
            yield return null;
        }
    }
}
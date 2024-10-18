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
        
        _disappearCoroutine = _player.StartCoroutine(_player._viewPlayer.MaterialTransitionCoroutine(true));
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
    
    //private IEnumerator HandleMaterialTransition()
    //{
    //    // Desvanecer al jugador (fadeOut = true)
    //    yield return _player.StartCoroutine(_player._viewPlayer.MaterialTransitionCoroutine(true));
    //
    //    // Esperar un breve momento si lo deseas
    //    yield return new WaitForSeconds(1.5f);
    //
    //    // Aparecer al jugador (fadeOut = false)
    //    yield return _player.StartCoroutine(_player._viewPlayer.MaterialTransitionCoroutine(false));
    //}
}
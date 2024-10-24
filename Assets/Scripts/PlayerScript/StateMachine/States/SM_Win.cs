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
    private Coroutine _dissolveFlameCoroutine;

    private Material _flameMaterial;

    public SM_Win(Player player)
    {
        _player = player;
    }

    public override void OnEnter()
    {
        _player._viewPlayer.PLAY_ANIM_TRIGGER("Win");

        _materialChanged = false;
        _rotationStarted = false;

        _flameMaterial = _player.flame.GetComponent<Renderer>().material;

        _player.StartCoroutine(_player._viewPlayer.MaterialTransitionCoroutine(true));
        _player.StartCoroutine(DissolveFlame());
    }

    public override void OnExit()
    {
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

    IEnumerator DissolveFlame()
    {
        float startValue = 1f;
        float endValue = 0;
        float elapsedTime = 0.0f;

        while (elapsedTime < 2f)
        {
            elapsedTime += Time.deltaTime;

            _flameMaterial.SetFloat("_Dissolve_Distortion",
                Mathf.Lerp(startValue, endValue, elapsedTime / 2f));

            yield return null;
        }

        _player.flame.SetActive(false);
    }
}
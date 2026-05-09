using System.Collections;
using UnityEngine;
using static SfxIDs;

/// <summary> 
/// Puente de Animación: Traduce eventos visuales de los clips de animación en acciones lógicas, 
/// como la ejecución de áreas de impacto (Smash) o la activación de bloqueos de control. 
/// </summary>
public class PlayerAnimHandler : MonoBehaviour
{
    private PlayerContext _ctx;

    private void Start()
    {
        _ctx = GetComponentInParent<PlayerController>().Ctx;
    }

    public void Smash()
    {
        _ctx.View._smashFX.Play();
        _ctx.View.PlaySfx(Mummy___Head.SmashExit);

        Collider[] hits = Physics.OverlapSphere(transform.position, _ctx.SmashRange, _ctx.SmashLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out SmashObject smashObj))
            {
                smashObj.DoBreak();
            }
        }
    }
    public void Shoot() => GameEventManager.Instance.playerEvents.OnShoot.Raise();
    public void WrapFakeSwing()
    {
        Vector3 targetPoint = _ctx.TryGetSwingTarget(out Rigidbody hook)
            ? hook.worldCenterOfMass
            : _ctx.Rb.position + (_ctx.Rb.transform.forward * 4f) + Vector3.up;

        _ctx.View.StartBandage(_ctx.Tf, targetPoint, 0.15f);
    }
    public void UnWrap() => _ctx.View.StopBandage();
    public void CutSwing()
    {
        _ctx.View.CutBandage();
        _ctx.View.StopBandage();
    }
    private IEnumerator KinematicStumble(float duration, float distance)
    {
        float elapsed = 0f;
        Vector3 startPos = _ctx.Rb.position;
        Vector3 forward = _ctx.Rb.transform.forward;

        // RAYCAST DE SEGURIDAD
        // Importante: Usar un radio de SphereCast es mejor aquí para simular el grosor de la momia
        float safeDistance = distance;
        float mummyRadius = 0.4f;

        // Tiramos el rayo para ver si hay una pared antes de movernos
        if (Physics.Raycast(startPos + Vector3.up, forward, out RaycastHit hit, distance + mummyRadius,
                LayerMask.GetMask("Wall", "Interactable")))
        {
            // Restamos el radio para que la momia no quede "pegada" o dentro del muro
            safeDistance = Mathf.Max(0, hit.distance - mummyRadius);
        }

        Vector3 targetPos = startPos + (forward * safeDistance);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Movimiento con suavizado (Deceleración)
            float curve = t * (2 - t);

            // Movemos la posición directamente (al ser kinematic, ignoramos fuerzas)
            _ctx.Rb.position = Vector3.Lerp(startPos, targetPos, curve);

            if (!_ctx.IsGrounded())
            {
                _ctx.Rb.linearVelocity = Vector3.zero;
                break;
            }
            
            yield return null;
        }
    }
    public void KinematicStumble() => StartCoroutine(KinematicStumble(2f, 1f));
    public void Locked() => GameEventManager.Instance.playerEvents.OnLockRequested.Raise("AnimationEvent", true);
    public void UnLocked() => GameEventManager.Instance.playerEvents.OnLockRequested.Raise("AnimationEvent", false);
}
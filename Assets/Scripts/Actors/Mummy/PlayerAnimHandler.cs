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
    
    public void UnLocked()
    {
        GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Smash", false);
        _ctx.View.Animator.SetBool("Smash", false);
    }

    public void Locked() => GameEventManager.Instance.playerEvents.OnLockRequested.Raise("Smash", true);
}
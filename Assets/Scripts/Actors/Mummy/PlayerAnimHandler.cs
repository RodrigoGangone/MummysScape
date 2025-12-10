using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        Collider[] hits = Physics.OverlapSphere(transform.position, _ctx.SmashRange, _ctx.SmashLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out SmashObject smashObj))
            {
                smashObj.DoBreak();
            }
        }
    }

    public void UnLocked()
    {
        GameEventManager.Instance.playerEvents.OnLocked.Raise(false);
        _ctx.View.Animator.SetBool("Smash", false);
    }

    public void Locked() => GameEventManager.Instance.playerEvents.OnLocked.Raise(true);
}
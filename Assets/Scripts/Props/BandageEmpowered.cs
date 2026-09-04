using System;
using UnityEngine;
using static Tags;

public class BandageEmpowered : MonoBehaviour
{
    private const int AMOUNT = 1;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_TAG)) return;

        var ctrl = other.GetComponentInParent<PlayerController>();
        if (ctrl == null) return;

        bool wasCollected = ctrl.TryCollectSpecialBandage(AMOUNT);
        if (!wasCollected) return;

        if (ctrl.Ctx.Model.Size != PlayerEnum.PlayerSize.Normal)
            ctrl.TryCollectBandage(ctrl.Ctx.Model.MaxBandagesValue);

        GameEventManager.Instance.playerEvents.OnEmpoweredBegin.Raise();
        
        Destroy(gameObject);
    }
}
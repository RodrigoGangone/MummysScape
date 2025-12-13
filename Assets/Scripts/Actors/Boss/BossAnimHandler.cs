using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAnimHandler : MonoBehaviour
{
    [SerializeField] private BossActor _bossActor;

    [Header("FX")] 
    
    [SerializeField] private GameObject primaryChargePrefab;
    [SerializeField] private Transform primaryChargeSocket;
    
    public void AE_Entry_End() => _bossActor.NotifyEntryEnded();
    public void AE_Damaged_Recovery() => _bossActor.NotifyRecovery();

    public void AE_Primary_FX()
    {
        if (primaryChargePrefab == null || primaryChargeSocket == null) return;
        
        GameObject go = Instantiate(primaryChargePrefab, primaryChargeSocket.position, primaryChargeSocket.rotation);
            
        go.transform.SetParent(primaryChargeSocket);
            
        var proj = go.GetComponent<ChargeableProjectile>();
        
        if (proj != null)
            proj.Initialize(_bossActor);
        else
            Debug.LogError("El prefab 'primaryChargePrefab' no tiene el script ChargeableProjectile.");
    }
    
    public void AE_Primary_Launch()
    {
        if (_bossActor?.OnPrimarySkill?.Invoke() != true)
            _bossActor?.NotifySkillEnded();
    }
    
    public void AE_Secondary_Launch()
    {
        if (_bossActor?.OnSecondarySkill?.Invoke() != true)
            _bossActor?.NotifySkillEnded();
    }
    
    public void AE_Skill_Ended() => _bossActor.NotifySkillEnded();

    public void AE_Die() => Destroy(_bossActor.gameObject);
}
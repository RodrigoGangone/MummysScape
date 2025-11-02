using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAnimHandler : MonoBehaviour
{
    [SerializeField] private BossActor _bossActor;

    public void AE_Entry_End() => _bossActor.NotifyEntryEnded();
    public void AE_Damaged_Recovery() => _bossActor.NotifyRecovery();

    //Skill Primary

    public void AE_Primary_Launch()
    {
        if (_bossActor?.OnPrimarySkill?.Invoke() != true)
            _bossActor?.NotifySkillEnded();
    }
    
    //Skill Secondary

    public void AE_Secondary_Launch()
    {
        if (_bossActor?.OnSecondarySkill?.Invoke() != true)
            _bossActor?.NotifySkillEnded();
    }
    
    public void AE_Skill_Ended() => _bossActor.NotifySkillEnded();

    public void AE_Die() => Destroy(_bossActor.gameObject);
}

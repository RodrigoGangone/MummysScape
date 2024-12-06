using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static Utils;

public class DeathBossScorpion : State
{
    private Scorpion _scorpion;

    public DeathBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
    //    _scorpion.anim.SetTrigger(DEATH_ANIM_SCORPION);
//
    //    //_scorpion.viewScorpion.SetActive(false);
//
    //    _scorpion.stageProperties.winPlatform.gameObject.SetActive(true);
//
    //    MoveWinPlatform();
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
    }

  //  private void MoveWinPlatform()
  //  {
  //      _scorpion.StartCoroutine(MovePlatformCoroutine());
  //  }

   // // Coroutine que mueve la plataforma
   // private IEnumerator MovePlatformCoroutine()
   // {
   //     Vector3 startPos = _scorpion._winPlatform.position;
   //     Vector3 targetPos = new Vector3(6, 8, 35); // La posición a donde quieres mover la plataforma
//
   //     float distance = Vector3.Distance(startPos, targetPos);
   //     float startTime = Time.time;
//
   //     while (Vector3.Distance(_scorpion._winPlatform.position, targetPos) > 0.1f)
   //     {
   //         // Calcular el tiempo transcurrido y mover la plataforma de forma gradual
   //         float journeyLength = Vector3.Distance(startPos, targetPos);
   //         float distanceCovered = (Time.time - startTime) * 5f;
   //         float fractionOfJourney = distanceCovered / journeyLength;
//
   //         _scorpion._winPlatform.position = Vector3.Lerp(startPos, targetPos, fractionOfJourney);
//
   //         yield return null; // Espera hasta el siguiente frame
   //     }
//
   //     // Asegurarse de que la plataforma llegue exactamente a la posición final
   //     _scorpion._winPlatform.position = targetPos;
   // }
}
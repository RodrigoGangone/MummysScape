using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bandage : MonoBehaviour
{
    private Bullet _bulletInstance;

    /*public void setInstantiator(Bullet bulletIntance)
    {
        _bulletInstance = bulletIntance;
    }*/

    private void OnTriggerEnter(Collider collision)
    {
        //Verifica que pueda tomar la venda
        if (!collision.gameObject.CompareTag("PlayerFather")) return;
        
        //buscar componente en el padre del padre
        var playerRef = collision.gameObject.GetComponentInParent<Player>().GetComponentInParent<Player>(); 
        if (playerRef.CurrentBandageStock >= playerRef.MinBandageStock &&
            playerRef.CurrentBandageStock < playerRef.MaxBandageStock)
        {
            playerRef._modelPlayer.CountBandage(1);
            Destroy(gameObject);
        }
    }
}
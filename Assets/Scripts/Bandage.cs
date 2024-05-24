using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bandage : MonoBehaviour
{
    private Bullet _bulletInstance;

    public void setInstantiator(Bullet bulletIntance)
    {
        _bulletInstance = bulletIntance;
    }

    private void OnTriggerEnter(Collider collision)
    {
        //Verifica que pueda tomar la venda
        if (!collision.gameObject.CompareTag("Player")) return;
        var playerRef =
            collision.gameObject.GetComponentInParent<Player>()
                .GetComponentInParent<Player>(); //buscar componente en el padre del padre

        if (playerRef.CurrentNumOfShoot <= playerRef.MaxNumOfShoot && playerRef.CurrentNumOfShoot > 0)
        {
            playerRef.CurrentNumOfShoot--;
            playerRef._modelPlayer.SizeHandler();

            if (_bulletInstance !=
                null) //Puede que la venda no venga de un disparo, sino que se spawnee al romper jarrones
            {
                BulletFactory.Instance.ReturnObjectToPool(_bulletInstance);
            }

            Destroy(gameObject);
        }
    }
}
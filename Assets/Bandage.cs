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
        var playerRef = collision.gameObject.GetComponent<Player>();

        if (playerRef.CurrentNumOfShoot <= playerRef.MaxNumOfShoot)
        {
            playerRef.CurrentNumOfShoot--;
            playerRef.transform.localScale += new Vector3(0.25f, 0.25f, 0.25f); //TODO: hacer metodo para aumentar dimensiones
            BulletFactory.Instance.ReturnObjectToPool(_bulletInstance); 
            Destroy(gameObject);
        }
    }
}
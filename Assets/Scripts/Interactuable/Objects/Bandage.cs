using UnityEngine;

public class Bandage : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        if (!collision.gameObject.CompareTag("PlayerFather")) return;
        
        var playerRef = collision.gameObject.GetComponentInParent<Player>().GetComponentInParent<Player>(); 
        if (playerRef.CurrentBandageStock >= playerRef.MinBandageStock &&
            playerRef.CurrentBandageStock < playerRef.MaxBandageStock)
        {
            playerRef._modelPlayer.CountBandage(1);
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class Bandage : MonoBehaviour
{
    private void OnTriggerEnter(Collider collision)
    {
        if (!collision.gameObject.CompareTag("PlayerFather")) return;

        var playerModel = collision.gameObject.GetComponentInParent<PlayerController>().Ctx.Model;
        if (playerModel.Bandages >= playerModel.MinBandagesValue &&
            playerModel.Bandages < playerModel.MaxBandagesValue)
        {
            playerModel.AddBandages();
            Destroy(gameObject);
        }
    }
}
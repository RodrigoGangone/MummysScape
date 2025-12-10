using System;
using System.Collections;
using UnityEngine;
using static PauseUtils;

public class Bandage : MonoBehaviour, IPausable
{
    private Collider _collider;
    private bool _paused;

    private void Awake() => _collider = GetComponent<Collider>();

    private void OnEnable()
    {
        _collider.enabled = false;

        StartCoroutine(EnableColliderRoutine());
    }

    private IEnumerator EnableColliderRoutine()
    {
        yield return WaitForSecondsPausable(2f, () => _paused);

        _collider.enabled = true;
    }
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

    public void OnPauseChanged(bool paused) => _paused = paused;
}
using UnityEngine;
using System.Collections;
using static Tags;
using System.Collections.Generic;

public class BossBoxCatchTrigger : MonoBehaviour
{
    [SerializeField] private BossActor bossActor;
    [SerializeField] private Transform socket;
    [SerializeField] private GameObject fakeBox;

    [Header("Catch Settings")] [SerializeField]
    private float catchDuration = 0.5f; // Tiempo que tarda en llegar a la pinza

    [SerializeField] private AnimationCurve catchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private HashSet<GameObject> _caughtBoxes = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HEAVY_BOX_TAG))
        {
            GameObject box = other.gameObject;

            if (_caughtBoxes.Add(box))
            {
                if (_caughtBoxes.Count == 1)
                {
                    HandleFirstBoxCatch(box);
                }
                else
                {
                    HandleSecondBoxImpact();
                }
            }
        }
    }

    private void HandleFirstBoxCatch(GameObject box)
    {
        // 1. Apagamos físicas de inmediato para evitar colisiones erráticas durante el traslado
        if (box.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 2. Notificamos al Boss para que pase a PreDie e inicie su animación en loop
        bossActor.NotifyPreDie();

        // 3. Iniciamos el traslado visual
        StartCoroutine(MoveAndDeactivateBox(box));
    }

    private IEnumerator MoveAndDeactivateBox(GameObject box)
    {
        Vector3 startPos = box.transform.position;
        Quaternion startRot = box.transform.rotation;

        float time = 0f;
        while (time < catchDuration)
        {
            time += Time.deltaTime;
            float t01 = Mathf.Clamp01(time / catchDuration);
            float curveValue = catchCurve.Evaluate(t01);

            // Interpolación hacia el socket
            box.transform.position = Vector3.LerpUnclamped(startPos, socket.position, curveValue);
            box.transform.rotation = Quaternion.LerpUnclamped(startRot, socket.rotation, curveValue);

            yield return null;
        }

        // 4. Apagamos la caja al llegar a destino
        box.SetActive(false);
        fakeBox.SetActive(true);
    }

    private void HandleSecondBoxImpact() => GameEventManager.Instance.bossEvents.OnDeath.Raise();
}
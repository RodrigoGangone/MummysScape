using System.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Puente de Animación: Traduce los eventos de los clips de Animator (Animation Events) en acciones 
/// de código, como notificar el fin de un estado, instanciar efectos o disparar proyectiles.
/// </summary>
public class BossAnimHandler : MonoBehaviour
{
    [SerializeField] private BossActor _bossActor;

    [Header("FX")] [SerializeField] private GameObject primaryChargePrefab;
    [SerializeField] private Transform primaryChargeSocket;
    [SerializeField] private FxBank bank;
    [SerializeField] private GameObject brokenBox;

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

    public void AE_Die() => StartCoroutine(SinkAndDieRoutine());

    private IEnumerator SinkAndDieRoutine()
    {
        // 1. Configuración de variables
        float duration = 2.0f; // Tiempo que tarda en bajar (ajústalo a tu gusto)
        float distance = 3.0f;
        Vector3 startPos = _bossActor.transform.position;
        Vector3 endPos = startPos + Vector3.down * distance;
        float elapsed = 0;

        // 2. Bucle de interpolación (Movimiento gradual)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            // Aplicamos el movimiento al transform del BossActor
            _bossActor.transform.position = Vector3.Lerp(startPos, endPos, percent);

            yield return null; // Espera al siguiente frame
        }

        // 3. Destrucción final
        Destroy(_bossActor.gameObject);
    }

    public void Play_Entry()
    {
        bank.Play2D("Entry");
        GameEventManager.Instance.levelEvents.OnRumbleHigh.Raise(1f, 2f);
        GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(1f, 2f);
    }

    public void Play_Charge() => bank.Play3D("Charge", transform.position);
    public void Play_Launch() => bank.Play3D("Launch", transform.position);
    public void Play_Dig() => bank.Play3D("Dig", transform.position);

    public void Play_Death()
    {
        bank.Play3D("Death", transform.position);
        GameEventManager.Instance.levelEvents.OnRumbleHigh.Raise(1f, 2f);
        GameEventManager.Instance.levelEvents.OnRumbleLow.Raise(1f, 2f);
    }

    public void Play_BrokenBox() => brokenBox.SetActive(true);
}
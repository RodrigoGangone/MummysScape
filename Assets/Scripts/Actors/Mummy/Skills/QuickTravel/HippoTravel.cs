using System.Collections;
using UnityEngine;

public class HippoTravel : MonoBehaviour, IPausable
{
    [SerializeField] private HippoTravel teleportDestination;
    [SerializeField] private Transform posHeadOpen;
    [SerializeField] private Transform posHeadClose;
    [SerializeField, Min(0.01f)] private float approachDuration = 0.35f;

    private Animator HippoStartAnim => GetComponentInChildren<Animator>();
    private Animator HippoDestAnim => teleportDestination.GetComponentInChildren<Animator>();

    public HippoLink Link => GetComponentInParent<HippoLink>();

    private bool _paused;

    public PlayerSizeVisual playerSizeVisual;

    public void OnPauseChanged(bool paused)
    {
        _paused = paused;

        if (HippoStartAnim) HippoStartAnim.enabled = !paused;
        if (HippoDestAnim) HippoDestAnim.enabled = !paused;
    }

    public void BeginTravel(Transform player)
    {
        if (_paused || !Link.CanStartTravel()) return;
        
        StartCoroutine(Co_Travel(player));
    }

    private IEnumerator Co_Travel(Transform player)
    {
        Link.StartTravel();
        
        playerSizeVisual = player.GetComponent<PlayerSizeVisual>();
        teleportDestination.AssignPlayerToDestination(playerSizeVisual);

        // 1) Acercar al jugador a posHeadOpen (con pausa y rotación siempre aplicada)
        Vector3 p0 = player.position, p1 = posHeadOpen.position;
        Quaternion r0 = player.rotation, r1 = posHeadOpen.rotation;

        for (float t = 0f; t < approachDuration;)
        {
            if (_paused)
            {
                yield return PauseUtils.WaitWhilePaused(() => _paused);
                continue;
            }

            t += Time.deltaTime;
            var k = Mathf.Clamp01(t / approachDuration);

            player.position = Vector3.Lerp(p0, p1, k);
            player.rotation = Quaternion.Slerp(r0, r1, k);

            yield return null;
        }

        player.position = p1;
        player.rotation = r1;

        // 2) Abrir boca (start) + esperar 0.5s pausable
        if (HippoStartAnim) HippoStartAnim.SetTrigger("isOpen");
        yield return PauseUtils.WaitForSecondsPausable(3f, () => _paused);

        // 3) Mover a salida destino + cerrar boca (dest)
        var exit = teleportDestination.posHeadClose;
        player.position = exit.position;
        player.rotation = exit.rotation;

        if (HippoDestAnim) HippoDestAnim.SetTrigger("isClose");
    }


    public void PlayerMeshOff()
    {
        playerSizeVisual.MeshTurn(false);
    }

    public void PlayerMeshOn()
    {
        playerSizeVisual.MeshTurn(true);
        
        Link.EndTravel();
    }

    private void AssignPlayerToDestination(PlayerSizeVisual player) => playerSizeVisual = player;
}
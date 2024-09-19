using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimationEventReceiver : MonoBehaviour
{
    private Player _player;

    private void Start()
    {
        _player = GetComponentInParent<Player>();
    }

    public void EVENT_ANIM_SHOOT() //Se usa en animacion : Shoot
    {
        _player._modelPlayer.Shoot();
    }

    public void EVENT_ANIM_GO_IDLE() //Se usa en animacion : Shoot
    {
        _player._stateMachinePlayer.ChangeState(PlayerState.Idle);
    }

    public void EVENT_ANIM_HOOK()
    {
        _player._modelPlayer.Hook();
    }

    public void EVENT_ANIM_PULL()
    {
        _player._modelPlayer.isPulling = true;
    }

    public void EVENT_ANIM_DRAW_PULL()
    {
        _player._viewPlayer.drawPull = true;
    }

    public void EVENT_ANIM_NEXT_SCENE()
    {
        GameManager.Instance.ChangeScene();
    }

    public void EVENT_ANIM_DEATH()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
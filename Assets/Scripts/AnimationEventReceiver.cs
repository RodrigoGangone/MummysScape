using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private Player _player;

    private void Start()
    {
        _player = GameObject.Find("Mummy").GetComponent<Player>();
    }

    public void EVENT_ANIM_SHOOT() //Se usa en animacion : Shoot
    {
        _player._modelPlayer.Shoot();
    }
    
    public void EVENT_ANIM_GO_IDLE() //Se usa en animacion : Shoot
    {
        _player._stateMachinePlayer.ChangeState(PlayerState.Idle);
    }
}
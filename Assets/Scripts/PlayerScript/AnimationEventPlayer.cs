using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimationEventPlayer : MonoBehaviour
{
    private Player _player;
    private UIManager _uiManager;

    private void Start()
    {
        _player = GetComponentInParent<Player>();
        _uiManager = FindObjectOfType<UIManager>();
    }

    public void EVENT_ANIM_SHOOT() //Se usa en animacion : Shoot
    {
        _player._modelPlayer.Shoot();
    }

    public void EVENT_ANIM_GO_IDLE() //Se usa en animacion : Shoot
    {
        _player._stateMachinePlayer.ChangeState(PlayerState.Idle);
    }
    
    public void EVENT_ANIM_WIN()
    {
        _uiManager.Win();
    }

    public void EVENT_ANIM_LOSE()
    {
        _uiManager.Lose();
    }
}
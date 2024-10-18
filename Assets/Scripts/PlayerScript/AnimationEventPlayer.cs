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

    public void EVENT_ANIM_PULL()
    {
        _player._modelPlayer.isPulling = true;
    }

    public void EVENT_ANIM_DRAW_PULL()
    {
        _player._viewPlayer.drawPull = true;
    }

    public void EVENT_ANIM_HIT_TACKLE()
    {
        if (_player.smashFX.isPlaying)
        {
            _player.smashFX.Stop();
            _player.smashFX.Clear();
        }

        _player.smashFX.Play();
        
        _player._modelPlayer.tackleSphereCollider.enabled = true;
    }
    
    public void EVENT_ANIM_FINISH_HIT_TACKLE()
    {
        _player._modelPlayer.tackleSphereCollider.enabled = false;
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
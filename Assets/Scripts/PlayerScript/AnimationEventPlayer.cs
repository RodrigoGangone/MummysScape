using TMPro.Examples;
using Unity.VisualScripting;
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

    public void EVENT_ANIM_GO_IDLE() //Se usa en animacion : Smash
    {
        _player._stateMachinePlayer.ChangeState(PlayerState.Idle);
    }

    public void EVENT_ANIM_HIT_SMASH(int value)
    {
        var activeSmash = value == 1;
        
        if (activeSmash)
        {
            //TODO: Volver a agregar esto
            //FindObjectOfType<CameraPos>().TriggerShake(); //Shake de camara casero xd
            
            if (_player.smashFX.isPlaying)
            {
                _player.smashFX.Stop();
                _player.smashFX.Clear();
            }
            _player.smashFX.Play();
        }
        _player._modelPlayer.tackleSphereCollider.enabled = activeSmash;
    }

    public void EVENT_UI_BREAK_HOURGLASS()
    {
        AudioManager.Instance.PlaySFX(NameSounds.SFX_BreakHourglass);
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
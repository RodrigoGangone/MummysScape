using UnityEngine;

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
            Camera.main?.GetComponent<CameraPathManager>().ShakeCamera(0.25f, 0.25f);
            
            if (_player.smashFX.isPlaying)
            {
                _player.smashFX.Stop();
                _player.smashFX.Clear();
            }
            AudioManager.Instance.PlaySFX(RandomSmashSound());
            _player.smashFX.Play();
        }
        _player._modelPlayer.tackleSphereCollider.enabled = activeSmash;
    }

    private NameSounds RandomSmashSound()
    {
        int randomValue = Random.Range(0, 4);

        switch (randomValue)
        {
            case 0:
                return NameSounds.SFX_MummySmash_1;
            case 1:
                return NameSounds.SFX_MummySmash_2;
            case 2:
                return NameSounds.SFX_MummySmash_3; 
            case 3:
                return NameSounds.SFX_MummySmash_4;
            default:
                return NameSounds.SFX_MummySmash_1; 
        }
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
using UnityEngine;

public class SM_Walk : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Walk(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;

        _player.SizeModify += CheckWalkSound;
    }

    private void CheckWalkSound()
    {
        if (!_player._stateMachinePlayer.getCurrentState().Equals(Utils.STATE_WALK)) return;
        
        switch (_player.CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkNormal);
                break;
            case PlayerSize.Small:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkSmall);
                break;
            case PlayerSize.Head:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkHead);
                break;
            default:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkNormal);
                break;
        }
    }

    public override void OnEnter()
    {

        _view.PLAY_ANIM("Walk", true);
        _view.PLAY_WALK(true);

        CheckWalkSound();
    }

    public override void OnExit()
    {
        _model.ClampMovement();
        _view.PLAY_ANIM("Walk", false);
        _view.PLAY_WALK(false);

        AudioManager.Instance.StopSFX(NameSounds.SFX_MummyWalkNormal);
        AudioManager.Instance.StopSFX(NameSounds.SFX_MummyWalkSmall);
        AudioManager.Instance.StopSFX(NameSounds.SFX_MummyWalkHead);
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnFixedUpdate()
    {

        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"),
            _player.Speed,
            _player.SpeedRotation);
        
    }
}
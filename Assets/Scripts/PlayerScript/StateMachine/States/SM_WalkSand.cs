using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_WalkSand : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_WalkSand(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;
    }

    public override void OnEnter()
    {
        Debug.Log("ON ENTER STATE WALKSAND");
        _view.PLAY_ANIM("WalkSand", true);
        _view.PLAY_WALK_SAND(true);
        
        switch (_player.CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkSandNormal);
                break;
            case PlayerSize.Small:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkSandSmall);
                break;
            case PlayerSize.Head:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkSandHead);
                break;
            default:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyWalkNormal);
                break;
        }
    }

    public override void OnExit()
    {
        _model.ClampMovement();
        _view.PLAY_WALK_SAND(false);
        _view.PLAY_ANIM("WalkSand", false);
        
        switch (_player.CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                AudioManager.Instance.StopSFX(NameSounds.SFX_MummyWalkSandNormal);
                break;
            case PlayerSize.Small:
                AudioManager.Instance.StopSFX(NameSounds.SFX_MummyWalkSandSmall);
                break;
            case PlayerSize.Head:
                AudioManager.Instance.StopSFX(NameSounds.SFX_MummyWalkSandHead);
                break;
            default:
                AudioManager.Instance.StopSFX(NameSounds.SFX_MummyWalkNormal);
                break;
        }
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
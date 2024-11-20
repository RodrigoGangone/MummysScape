
public class SM_Dead : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Dead(Player player)
    {
        _player = player;
        _model = _player._modelPlayer;
        _view = _player._viewPlayer;
    }

    public override void OnEnter()
    {
        _view.PLAY_ANIM_TRIGGER("Death");

        switch (_player.CurrentPlayerSize)
        {
            case PlayerSize.Normal:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyDeathNormal);
                break;
            case PlayerSize.Small:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyDeathSmall);
                break;
            default:
                AudioManager.Instance.PlaySFX(NameSounds.SFX_MummyDeathSkull);
                break;
        }
    }

    public override void OnExit()
    {
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }
}

public class SM_Smash : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Smash(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }
    
    public override void OnEnter()
    {
        _view.PLAY_ANIM("Smash", true);
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Smash", false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
    }
}
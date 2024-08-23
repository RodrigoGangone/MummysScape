public class SM_Idle : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Idle(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }
    public override void OnEnter()
    {
        _view.PLAY_ANIM("Idle", true);
    }
    
    public override void OnExit()
    {
        _view.PLAY_ANIM("Idle", false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        _model.ActivateParticleButtonInView();
    }
}

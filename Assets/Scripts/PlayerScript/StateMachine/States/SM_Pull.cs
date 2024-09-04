

public class SM_Pull : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;
    
    public SM_Pull(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        //TODO: Agregar animacion de pull
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

using UnityEngine;

public class SM_Walk : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    public SM_Walk(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        _view.PLAY_ANIM("Walk", true);
        _view.PLAY_WALK(true);
    }

    public override void OnExit()
    {
        _model.ClampMovement();
        _view.PLAY_ANIM("Walk", false);
        _view.PLAY_WALK(false);
    }

    public override void OnUpdate()
    {
    }

    public override void OnFixedUpdate()
    {
        _model.Move(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
        
        _model.ActivateParticleButtonInView();
    }
}
using UnityEngine;

public class SM_Pull : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;
    private float _time;
    bool isPullDestiny;

    public SM_Pull(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        Debug.Log("ON ENTER PULL ");
        _view.bandageHook.enabled = true;
        _view.PLAY_ANIM("Pull", true);

        //TODO: Agregar animacion de pull
        Debug.Log("Pull");
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Pull", false);
        Debug.Log("ON EXIT PULL ");
        isPullDestiny = false;

        _view.bandageHook.enabled = false;

        _view.hookMaterial.SetFloat("_rightThreshold", 1.5f);

        _time = 0;
        _model.isPulling = false;
        _view.drawPull = false;
    }

    public override void OnUpdate()
    {
        if (!Input.GetKey(KeyCode.Space) || _model.CurrentBox == null)
            StateMachine.ChangeState(PlayerState.Idle);

        if (!_view.drawPull) return;

        if (!isPullDestiny)
        {
            _time += Time.deltaTime;
            var newValue = Mathf.Lerp(1.5f, -1.5f, _time / 0.5f);
            _view.hookMaterial.SetFloat("_rightThreshold", newValue);

            if (_view.hookMaterial.GetFloat("_rightThreshold") == -1.5f)
                isPullDestiny = true;
        }
    }

    public override void OnFixedUpdate()
    {
        if (_model.CurrentBox == null) return;

        if (!_model.IsBoxCloseToPlayer() &&
            _model.CurrentBox.GetComponent<PushPullObject>().BoxInFloor() &&
            _model.isPulling)
            _model.CurrentBox.transform.position += _model.DirToPull *  Time.deltaTime;

        _view.DrawBandage(_model.CurrentBox.position);
    }
}
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
        _view.bandageHook.enabled = true;

        //TODO: Agregar animacion de pull
        Debug.Log("Pull");
    }

    public override void OnExit()
    {
        isPullDestiny = false;
        _view.bandageHook.enabled = false;
    }

    public override void OnUpdate()
    {
        if (!isPullDestiny)
        {
            _time += Time.deltaTime;
            var newValue = Mathf.Lerp(1.5f, -1.5f, _time / 0.25f);
            _view.hookMaterial.SetFloat("_rightThreshold", newValue);

            if (_view.hookMaterial.GetFloat("_rightThreshold") == -1.5f)
                isPullDestiny = true;
        }

        _view.DrawBandagePull();
    }

    public override void OnFixedUpdate()
    {
        if (!_model.IsBoxCloseToPlayer())
            _model.MovePull();

        OnExit();
    }
}
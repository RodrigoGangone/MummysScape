using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Hook : State
{
    private ModelPlayer _model;
    private ViewPlayer _view;

    private bool _isHookDestiny;
    private float _time = 0;

    public SM_Hook(ModelPlayer model, ViewPlayer view)
    {
        _model = model;
        _view = view;
    }

    public override void OnEnter()
    {
        Debug.Log("ON ENTER HOOK");

        _view.StateRigBuilder(true);
        _view.PLAY_ANIM("Hook", true);
        _view.bandageHook.enabled = true;
    }

    public override void OnExit()
    {
        _view.PLAY_ANIM("Hook", false);
        _view.StateRigBuilder(false);
        ResetHook();
    }

    public override void OnUpdate()
    {
        if (!_model.isHooking) return;

        if (!_isHookDestiny)
        {
            _time += Time.deltaTime;
            var newValue = Mathf.Lerp(1.5f, -1.5f, _time / 0.25f);
            _view.hookMaterial.SetFloat("_rightThreshold", newValue);

            if (_view.hookMaterial.GetFloat("_rightThreshold") == -1.5f)
                _isHookDestiny = true;
        }

        if (!Input.GetKey(KeyCode.Space))
            StateMachine.ChangeState(PlayerState.Fall);
        else
        {
            _view.DrawBandageHOOK();
            IsSwinging();
        }
    }

    public override void OnFixedUpdate()
    {
        _model.LimitVelocityRB();

        if (!IsSwinging()) return;

        _model.MoveHooked(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
    }

    private bool IsSwinging()
    {
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }

    private void ResetHook()
    {
        _time = 0;

        _view.bandageHook.enabled = false;

        _view.rightHand.data.target = null;

        _view.hookMaterial.SetFloat("_rightThreshold", 1.5f);

        _isHookDestiny = false;

        _model.isHooking = false;

        Object.Destroy(_model.springJoint);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class SM_Hook : State
{
    private Player _player;

    private bool _isBandageDraw;
    private float _time;

    private Coroutine _drawBandageCoroutine;

    public SM_Hook(Player player)
    {
        _player = player;
    }

    public override void OnEnter()
    {
        _player.IsHooked = true;

        _player._modelPlayer.hookBeetle = _player._modelPlayer.detectionBeetle.currentHook;

        _player._viewPlayer.StateRigBuilder(true);
        _player._viewPlayer.PLAY_ANIM("Hook", true);
        _player._viewPlayer.bandageHook.enabled = true;

        _drawBandageCoroutine = _player.StartCoroutine(Bandage());
    }

    public override void OnExit()
    {
        _player.StopCoroutine(_drawBandageCoroutine);

        _time = 0;

        _player._viewPlayer.bandageHook.enabled = false;

        _player._viewPlayer.rightHand.data.target = null;

        _player.IsHooked = false;

        _isBandageDraw = false;

        _player._modelPlayer.hookBeetle = null;

        _player._viewPlayer.hookMaterial.SetFloat("_rightThreshold", 1.5f);

        Object.Destroy(_player._modelPlayer.springJoint);

        _player._viewPlayer.PLAY_ANIM("Hook", false);

        _player._viewPlayer.StateRigBuilder(false);
    }

    public override void OnUpdate()
    {
        if (!Input.GetKey(KeyCode.Space))
            StateMachine.ChangeState(PlayerState.Fall);
        else
        {
            _player._viewPlayer.DrawBandage(_player._modelPlayer.hookBeetle.transform.position);
            IsSwinging();
        }
    }

    private IEnumerator Bandage()
    {
        while (!_isBandageDraw)
        {
            _time += Time.deltaTime;
            var newValue = Mathf.Lerp(1.5f, -1.5f, _time / 0.5f);
            _player._viewPlayer.hookMaterial.SetFloat("_rightThreshold", newValue);

            if (_player._viewPlayer.hookMaterial.GetFloat("_rightThreshold") <= -1.5f)
            {
                _isBandageDraw = true;
            }

            yield return null;
        }
    }


    public override void OnFixedUpdate()
    {
        _player._modelPlayer.LimitVelocityRb();

        if (!IsSwinging()) return;

        _player._modelPlayer.MoveHooked(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
    }

    private bool IsSwinging()
    {
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }
}
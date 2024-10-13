using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SM_Hook : State
{
    private Player _player;

    private bool _isHookDestiny;
    private float _time = 0;

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

        _player.StartCoroutine(Bandage());
    }

    public override void OnExit()
    {
        _player._viewPlayer.PLAY_ANIM("Hook", false);
        _player._viewPlayer.StateRigBuilder(false);

        ResetHook();

        _player.IsHooked = false;
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
        _time = 0f; // Reseteamos el tiempo

        // Mientras no se haya llegado al destino del gancho
        while (!_isHookDestiny)
        {
            _time += Time.deltaTime;
            var newValue = Mathf.Lerp(1.5f, -1.5f, _time / 1f);
            _player._viewPlayer.hookMaterial.SetFloat("_rightThreshold", newValue);

            // Si el valor llega a -1.5f, marcamos que hemos alcanzado el destino
            if (_player._viewPlayer.hookMaterial.GetFloat("_rightThreshold") <= -1.5f)
            {
                _isHookDestiny = true;
            }

            yield return null; // Esperamos hasta el siguiente frame
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

    private void ResetHook()
    {
        _time = 0;

        _player._viewPlayer.bandageHook.enabled = false;

        _player._viewPlayer.rightHand.data.target = null;

        _player._viewPlayer.hookMaterial.SetFloat("_rightThreshold", 1.5f);

        _isHookDestiny = false;

        _player._modelPlayer.isHooking = false;

        _player._modelPlayer.hookBeetle = null;

        Object.Destroy(_player._modelPlayer.springJoint);
    }
}
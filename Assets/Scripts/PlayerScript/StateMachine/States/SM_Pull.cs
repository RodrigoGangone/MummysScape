using System.Collections;
using UnityEngine;
using static Utils;

public class SM_Pull : State
{
    private Player _player;
    
    private bool _isBandageDraw;
    private float _time;
    
    private Coroutine _drawBandageCoroutine;

    public SM_Pull(Player player)
    {
        _player = player;
    }

    public override void OnEnter()
    {
        Debug.Log("ON ENTER PULL ");
        
        _player._viewPlayer.PLAY_ANIM("Pull", true);
        _player._viewPlayer.bandageLineRenderer.enabled = true;
        
        _drawBandageCoroutine = _player.StartCoroutine(Bandage());
    }

    public override void OnExit()
    {
        Debug.Log("ON EXIT PULL ");
        
        _player.StopCoroutine(_drawBandageCoroutine);
        
        _time = 0;
        
        _player._viewPlayer.PLAY_ANIM("Pull", false);
        _player._viewPlayer.bandageLineRenderer.enabled = false;
        _player._viewPlayer.hookMaterial.SetFloat("_rightThreshold", 1.5f);
        
        UnwrapBox();

        _pullTime = 0;
    }

    public override void OnUpdate()
    {
        if (!Input.GetKey(KeyCode.Space) || _player._modelPlayer.CurrentBox == null ||
            !_player._modelPlayer.CurrentBox.GetComponent<PushPullObject>().BoxInFloor())
            StateMachine.ChangeState(PlayerState.Idle);

    }

    private float _pullTime;

    public override void OnFixedUpdate()
    {
        if (_player._modelPlayer.CurrentBox == null) return;

        if (_isBandageDraw &&
            !_player._modelPlayer.IsBoxCloseToPlayer() &&
            _player._modelPlayer.CurrentBox.GetComponent<PushPullObject>().BoxInFloor())
        {
            _pullTime += Time.fixedDeltaTime;
            float speed = _player.SpeedPull.Evaluate(_pullTime);
            _player._modelPlayer.CurrentBox.transform.position += _player._modelPlayer.DirToPull * (speed * Time.fixedDeltaTime);
        }

        _player._viewPlayer.DrawBandage(_player._modelPlayer.CurrentBox.position);
    }
    
    private IEnumerator Bandage()
    {
        while (!_isBandageDraw)
        {
            _time += Time.deltaTime;
            var newValue = Mathf.Lerp(1.5f, -1.5f, _time / 0.5f);
            _player._viewPlayer.hookMaterial.SetFloat(RIGHT_THRESHOLD, newValue);

            if (_player._viewPlayer.hookMaterial.GetFloat(RIGHT_THRESHOLD) <= -1.5f)
            {
                WrapBox();
            }

            yield return null;
        }
    }
    
    private void WrapBox()
    {
        if (_player._modelPlayer.CurrentBox == null) return;
        
        var boxScript = _player._modelPlayer.CurrentBox.GetComponent<PushPullObject>();
        if (boxScript != null)
        {
            _isBandageDraw = true;
            boxScript.StartWrap();
        }
    }

    private void UnwrapBox()
    {
        if (_player._modelPlayer.CurrentBox == null) return;
        
        var boxScript = _player._modelPlayer.CurrentBox.GetComponent<PushPullObject>();
        if (boxScript != null)
        {
            _isBandageDraw = false;
            boxScript.StartUnwrap();
        }
    }
}
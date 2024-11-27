using System.Collections;
using UnityEngine;
using static Utils;

public class SM_Pull : State
{
    private Player _player;
    
    private bool _isBandageDraw;
    private float _time;

    private Transform _lastCurrentBoxTrans;
    private PushPullObject _lastCurrentBoxScript;
    
    private Coroutine _drawBandageCoroutine;

    public SM_Pull(Player player)
    {
        _player = player;
    }

    public override void OnEnter()
    {
        _player._viewPlayer.PLAY_ANIM("Pull", true);
        _player._viewPlayer.bandageLineRenderer.enabled = true;

        _lastCurrentBoxTrans = _player._modelPlayer.CurrentBox;
        _lastCurrentBoxScript = _lastCurrentBoxTrans.GetComponent<PushPullObject>();
        
        _drawBandageCoroutine = _player.StartCoroutine(Bandage());
        
        AudioManager.Instance.PlaySFX(NameSounds.SFX_MovingBox);
    }

    public override void OnExit()
    {
        _player.StopCoroutine(_drawBandageCoroutine);
        
        _time = 0;
        
        _player._viewPlayer.PLAY_ANIM("Pull", false);
        _player._viewPlayer.bandageLineRenderer.enabled = false;
        _player._viewPlayer.hookMaterial.SetFloat("_rightThreshold", 1.5f);

        if (_lastCurrentBoxScript.CheckCollisionInDirections(_player._modelPlayer.DirToPull))
            ExplodeBox();
        else
            UnwrapBox();

        _pullTime = 0;
        
        AudioManager.Instance.StopSFX(NameSounds.SFX_MovingBox);
    }

    public override void OnUpdate()
    {
        if (!Input.GetKey(KeyCode.Space) || _player._modelPlayer.CurrentBox == null ||
            !_lastCurrentBoxScript.BoxInFloor())
            StateMachine.ChangeState(PlayerState.Idle);

    }

    private float _pullTime;

    public override void OnFixedUpdate()
    {
        if (_player._modelPlayer.CurrentBox == null) return;

        if (_isBandageDraw &&
            !_player._modelPlayer.IsBoxCloseToPlayer() &&
            _lastCurrentBoxScript.BoxInFloor())
        {
            _pullTime += Time.fixedDeltaTime;
            float speed = _player.SpeedPull.Evaluate(_pullTime);
            _lastCurrentBoxTrans.transform.position += _player._modelPlayer.DirToPull * (speed * Time.fixedDeltaTime);
        }

        _player._viewPlayer.DrawBandage(_lastCurrentBoxTrans.position);
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
        if (_lastCurrentBoxTrans == null) return;
        
        var boxScript = _lastCurrentBoxScript;
        if (boxScript != null)
        {
            _isBandageDraw = true;
            boxScript.StartWrap();
        }
    }

    private void UnwrapBox()
    {
        if (_lastCurrentBoxTrans == null) return;
        
        var boxScript = _lastCurrentBoxScript;
        if (boxScript != null)
        {
            _isBandageDraw = false;
            boxScript.StartUnwrap();
        }
    }
    
    private void ExplodeBox()
    {
        if (_lastCurrentBoxTrans == null) return;
        
        var boxScript = _lastCurrentBoxScript;
        if (boxScript != null)
        {
            _isBandageDraw = false;
            boxScript.StartExplode();
        }
    }
}
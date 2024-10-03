using UnityEngine;

public class SM_Pull : State
{
    private Player _player;
    private ModelPlayer _model;
    private ViewPlayer _view;
    private float _time;
    bool isPullDestiny;

    public SM_Pull(Player player)
    {
        _player = player;

        _model = _player._modelPlayer;
        _view = _player._viewPlayer;
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

        // Desenvuelvo la caja
        if (_model.CurrentBox != null)
        {
            var boxScript = _model.CurrentBox.GetComponent<PushPullObject>();
            if (boxScript != null)
            {
                //boxScript.SetExplode(!boxScript.BoxInFloor());
                boxScript.StartUnwrap();
            }
        }

        _time = 0;
        _model.isPulling = false;
        _view.drawPull = false;
        _pullTime = 0;
    }

    public override void OnUpdate()
    {
        if (!Input.GetKey(KeyCode.Space) || _model.CurrentBox == null ||
            !_model.CurrentBox.GetComponent<PushPullObject>().BoxInFloor())
            StateMachine.ChangeState(PlayerState.Idle);

        if (!_view.drawPull) return;

        if (!isPullDestiny)
        {
            _time += Time.deltaTime;
            var newValue = Mathf.Lerp(1.5f, -1.5f, _time / 0.5f);
            _view.hookMaterial.SetFloat("_rightThreshold", newValue);

            if (_view.hookMaterial.GetFloat("_rightThreshold") == -1.5f)
            {
                isPullDestiny = true;

                // Envolver la caja
                var boxScript = _model.CurrentBox.GetComponent<PushPullObject>();
                if (boxScript != null) boxScript.StartWrap();
            }
        }
    }

    private float _pullTime;

    public override void OnFixedUpdate()
    {
        if (_model.CurrentBox == null) return;

        if (isPullDestiny &&
            !_model.IsBoxCloseToPlayer() &&
            _model.CurrentBox.GetComponent<PushPullObject>().BoxInFloor() &&
            _model.isPulling)
        {
            _pullTime += Time.fixedDeltaTime;
            float speed = _player.SpeedPull.Evaluate(_pullTime);
            _model.CurrentBox.transform.position += _model.DirToPull * speed * Time.fixedDeltaTime;
        }

        _view.DrawBandage(_model.CurrentBox.position);
    }
}
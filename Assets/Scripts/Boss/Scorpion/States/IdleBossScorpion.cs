using UnityEngine;
using static Utils;

public class IdleBossScorpion : State
{
    private Scorpion _scorpion;
    private Player _player;

    private float _currentCoolDownFirst;
    private float _currentCoolDownSecond;

    private const string ANIM_ROTATION_RIGHT = "Right";
    private const string ANIM_ROTATION_LEFT = "Left";

    public IdleBossScorpion(Scorpion scorpion, Player player)
    {
        _scorpion = scorpion;
        _player = player;
    }

    public override void OnEnter()
    {
        _scorpion.anim.SetBool(IDLE_ANIM_SCORPION, true);
        Debug.Log("ENTER IDLE");
    }

    public override void OnUpdate()
    {
        if (_scorpion.LevelManager._currentLevelState != LevelState.Playing) return;

        SelectRotation();
        SelectCurrentAttack();
        UpdateCooldown();

        if (IsReadyForAttack(CurrentAttack.First, _currentCoolDownFirst, _scorpion.coolDownFirst))
            _scorpion.stateMachine.ChangeState(BossScorpionState.FirstAttackScorpion);

        if (IsReadyForAttack(CurrentAttack.Second, _currentCoolDownSecond, _scorpion.coolDownSecond))
            _scorpion.stateMachine.ChangeState(BossScorpionState.SecondAttackScorpion);
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _scorpion.anim.SetBool(IDLE_ANIM_SCORPION, false);

        _currentCoolDownFirst = 0;
        _currentCoolDownSecond = 0;
    }

    //TODO: Agregar validacion para modificar acciones de geyser en ciertos lados del stage
//Todo: Por ejemplo, cuando estoy en el final del stage 2 y tengo que cubrirme con una plataforma que se mueve
    private void SelectCurrentAttack()
    {
        var currentState = _scorpion.player._stateMachinePlayer.getCurrentState();

        _scorpion.currentAttack = currentState is STATE_HOOK or STATE_FALL
            ? CurrentAttack.Second
            : CurrentAttack.First;
    }

    private void SelectRotation()
    {
        Vector3 playerPos = _player.transform.position;
        Vector3 scorpionPos = _scorpion.transform.position;

        Vector3 dirToPlayer = new Vector3(playerPos.x - scorpionPos.x, 0, playerPos.z - scorpionPos.z).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);

        float currentYAngle = _scorpion.transform.rotation.eulerAngles.y;
        float targetYAngle = targetRot.eulerAngles.y;
        float deltaAngle = Mathf.DeltaAngle(currentYAngle, targetYAngle);

        if (Mathf.Abs(deltaAngle) > 1) // Umbral para evitar spam de triggers
        {
            _scorpion.anim.SetTrigger(deltaAngle > 0 ? ANIM_ROTATION_RIGHT : ANIM_ROTATION_LEFT);
            _scorpion.transform.rotation = Quaternion.Lerp(
                _scorpion.transform.rotation,
                Quaternion.Euler(0, targetYAngle, 0),
                Time.deltaTime * 2f // Controla la velocidad de rotación
            );
        }
    }

    private void UpdateCooldown()
    {
        _currentCoolDownFirst = Mathf.Min(_currentCoolDownFirst + Time.deltaTime, _scorpion.coolDownFirst);
        _currentCoolDownSecond = Mathf.Min(_currentCoolDownSecond + Time.deltaTime, _scorpion.coolDownSecond);
    }

    private bool IsReadyForAttack(CurrentAttack attackType, float currentCooldown, float requiredCooldown)
    {
        return currentCooldown >= requiredCooldown && _scorpion.currentAttack == attackType;
    }
}
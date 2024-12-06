using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class FirstAttackBossScorpion : State
{
    private Scorpion _scorpion;
    private FirstAttackProperties _firstAttackProperties;

    private Vector3 _initialPosPlayer;

    private List<List<Vector3>> _allPaths; // Lista de trayectorias

    private float _coneAngle = 20f;
    private float _arcBezierHeight = 5f;

    private List<GameObject> _stonesPool;
    private Dictionary<GameObject, List<Vector3>> _activeStones = new();
    private int _currentPathIndex;
    private float _speed = 30f;

    public FirstAttackBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;

        _firstAttackProperties = scorpion.FirstAttack;
    }


    public override void OnEnter()
    {
        _scorpion.anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, true);

        InitializeStonesPool();
        GenerateAndLaunchStones();
    }

    public override void OnUpdate()
    {
        UpdateStonesMovement();
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _scorpion.anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, false);
    }

    private bool _poolInitialized;

    private void InitializeStonesPool()
    {
        if (_poolInitialized) return;

        _stonesPool = new List<GameObject>();
        _activeStones = new Dictionary<GameObject, List<Vector3>>();

        for (int i = 0; i < _firstAttackProperties.countStone; i++)
        {
            GameObject stone = GameObject.Instantiate(_firstAttackProperties.stone);
            stone.SetActive(false);
            _stonesPool.Add(stone);
        }

        _poolInitialized = true;
    }


    private void GenerateAndLaunchStones()
    {
        _allPaths = new List<List<Vector3>>();
        
        float angleIncrement = _coneAngle / (_firstAttackProperties.countStone - 1);
        Vector3 targetShootPos = _firstAttackProperties.targetShoot.position; // Posición inicial

        for (int i = 0; i < _scorpion.FirstAttack.countStone; i++)
        {
            float angle = -_coneAngle / 2 + angleIncrement * i;

            Vector3 direction = Quaternion.Euler(0, angle, 0) *
                                (_scorpion.player.transform.position - targetShootPos).normalized;

            Vector3 targetPosition = targetShootPos +
                                     direction * Vector3.Distance(targetShootPos, _scorpion.player.transform.position);

            Vector3 start = targetShootPos;
            Vector3 mid = (start + targetPosition) / 2 + Vector3.up * _arcBezierHeight;
            List<Vector3> path = new List<Vector3>();

            for (int j = 0; j <= 3; j++) // Resolución fija en 3 puntos
            {
                float t = j / 3f;
                path.Add((1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * targetPosition);
            }

            _allPaths.Add(path);

            // Activa la piedra y le asigna la trayectoria
            if (i < _stonesPool.Count)
            {
                GameObject stone = _stonesPool[i];
                stone.SetActive(true);
                stone.transform.position = path[0];
                _activeStones[stone] = path;
            }
        }
    }


    private Dictionary<GameObject, int> _stonePathIndices = new();

    private void UpdateStonesMovement()
    {
        foreach (var stone in new List<GameObject>(_activeStones.Keys))
        {
            List<Vector3> path = _activeStones[stone];

            if (!_stonePathIndices.ContainsKey(stone))
                _stonePathIndices[stone] = 0; // Comienza desde el primer punto de la trayectoria

            int currentIndex = _stonePathIndices[stone];

            if (currentIndex < path.Count - 1)
            {
                stone.transform.position = Vector3.MoveTowards(
                    stone.transform.position,
                    path[currentIndex + 1],
                    _speed * Time.deltaTime
                );

                if (Vector3.Distance(stone.transform.position, path[currentIndex + 1]) < 0.1f)
                    _stonePathIndices[stone] = currentIndex + 1; // Actualiza el índice
            }
            else
            {
                stone.SetActive(false);
                _activeStones.Remove(stone);
                _stonePathIndices.Remove(stone); // Limpia el índice
                _scorpion.stateMachine.ChangeState(ScorpionState.IdleScorpion);
            }
        }
    }


    // public IEnumerator MovePlayer(Vector3 dir)
    // {
    //     float speed = 8f;
    //     Vector3 normalizedDir = dir.normalized;
    //     float rayLength = 1f; // Longitud del rayo para detectar la capa Sand
    //
    //     while (_player._modelPlayer.CheckGround() && !IsOnSand(rayLength))
    //     {
    //         _player.transform.position += normalizedDir * (speed * Time.deltaTime);
    //         yield return null;
    //     }
    //
    //     _player.GetComponent<Rigidbody>().AddForce(normalizedDir * (IsOnSand(rayLength) ? 10f : 5f), ForceMode.Impulse);
    //
    //     yield return new WaitForSeconds(0.5f);
    //
    //     _player._modelPlayer.IsDamage = false;
    // }
    //
    // private bool IsOnSand(float rayLength)
    // {
    //     Vector3 rayOrigin = _player.transform.position + Vector3.up * 0.1f; // Origen del rayo
    //     Ray ray = new Ray(rayOrigin, Vector3.down);
    //     RaycastHit hit;
    //
    //     if (Physics.Raycast(ray, out hit, rayLength))
    //     {
    //         // Comprueba si la capa del objeto detectado es "Sand"
    //         return hit.collider.gameObject.layer == LayerMask.NameToLayer("Sand");
    //     }
    //
    //     return false;
    // }
}
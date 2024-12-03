using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class FirstAttackBossScorpion : State
{
    private FirstAttackProperties _firstAttackProperties;
    private static Vector3 _playerPos;
    private static Vector3 _scorpionPos;

    private static Animator _animator;
    private static readonly string animationName = "FirstAttack";

    private Vector3 _initialPosPlayer;

    private List<List<Vector3>> _allPaths; // Lista de trayectorias

    private float _coneAngle = 45f;
    private float _arcBezierHeight = 5f;

    public FirstAttackBossScorpion(FirstAttackProperties firstAttackProperties)
    {
        _firstAttackProperties = firstAttackProperties;
    }

    private List<GameObject> _stonesPool; // Pool de piedras
    private Dictionary<GameObject, List<Vector3>> _activeStones = new(); // Piedras activas y sus trayectorias
    private int _currentPathIndex; // Índice de progreso en la trayectoria
    private float _speed = 5f; // Velocidad de las piedras

    public override void OnEnter()
    {
        // _animator.SetBool(animationName, true);

        InitializeStonesPool();
        GenerateAndLaunchStones();
    }

    public static void SetAnimator(Animator animator)
    {
        _animator = animator;
    }

    public static void SetPositions(Transform ScorpionPos, Transform PlayerPos)
    {
        _playerPos = PlayerPos.position;
        _scorpionPos = ScorpionPos.position;
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
        // _animator.SetBool(animationName, false);
    }


    private void InitializeStonesPool()
    {
        _stonesPool = new List<GameObject>();
        _activeStones = new Dictionary<GameObject, List<Vector3>>();

        for (int i = 0; i < _firstAttackProperties.countStone; i++)
        {
            // Crea una instancia única de la piedra
            GameObject stone = GameObject.Instantiate(_firstAttackProperties.stone);
            stone.SetActive(false); // Inicia inactiva
            _stonesPool.Add(stone);
        }
    }


    private void GenerateAndLaunchStones()
    {
        _allPaths = new List<List<Vector3>>();
        float angleIncrement = _coneAngle / (_firstAttackProperties.countStone - 1);
        Vector3 targetShootPos = _firstAttackProperties.targetShoot.position; // Posición inicial

        for (int i = 0; i < _firstAttackProperties.countStone; i++)
        {
            // Calcula el ángulo para dispersar las piedras
            float angle = -_coneAngle / 2 + angleIncrement * i;

            // Dirección desde el targetShoot hacia el player
            Vector3 direction = Quaternion.Euler(0, angle, 0) * (_playerPos - targetShootPos).normalized;

            // Define la posición final
            Vector3 targetPosition = targetShootPos + direction * Vector3.Distance(targetShootPos, _playerPos);

            // Generar la trayectoria Bezier
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


    private Dictionary<GameObject, int> _stonePathIndices = new(); // Índices de progreso por piedra

    private void UpdateStonesMovement()
    {
        foreach (var stone in new List<GameObject>(_activeStones.Keys))
        {
            List<Vector3> path = _activeStones[stone];

            // Asegúrate de tener un índice para la piedra
            if (!_stonePathIndices.ContainsKey(stone))
            {
                _stonePathIndices[stone] = 0; // Comienza desde el primer punto de la trayectoria
            }

            int currentIndex = _stonePathIndices[stone];

            if (currentIndex < path.Count - 1)
            {
                // Mueve la piedra hacia el siguiente punto en la trayectoria
                stone.transform.position = Vector3.MoveTowards(
                    stone.transform.position,
                    path[currentIndex + 1],
                    _speed * Time.deltaTime
                );

                // Verifica si ha llegado al siguiente punto de la trayectoria
                if (Vector3.Distance(stone.transform.position, path[currentIndex + 1]) < 0.1f)
                {
                    _stonePathIndices[stone] = currentIndex + 1; // Actualiza el índice
                }
            }
            else
            {
                // Finaliza el movimiento de la piedra
                stone.SetActive(false);
                _activeStones.Remove(stone);
                _stonePathIndices.Remove(stone); // Limpia el índice
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
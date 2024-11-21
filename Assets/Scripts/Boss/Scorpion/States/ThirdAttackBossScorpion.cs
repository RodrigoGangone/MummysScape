using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using static Utils;
using UnityEngine;

public class ThirdAttackBossScorpion : State
{
    private Scorpion _scorpion;
    private Vector3 _initialPosPlayer;
    private const int SPEED_PROJECTILE = 25;
    private float _lifeTimeStone;
    private const int MAX_LIFETIME_STONE = 5;
    private List<Vector3> _pathPoints; // Lista de puntos de la trayectoria
    private int _currentPointIndex; // Índice del punto actual en la lista

    public ThirdAttackBossScorpion(Scorpion scorpion)
    {
        _scorpion = scorpion;
    }

    public override void OnEnter()
    {
        _initialPosPlayer = _scorpion.player.transform.position + new Vector3(0, 1f, 0);
        _scorpion._anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, true);
        _scorpion._stoneView.SetActive(true);

        GeneratePath();

        _currentPointIndex = 0;
    }

    public override void OnUpdate()
    {
        MoveStone();

        _lifeTimeStone += Time.deltaTime;

        if (_lifeTimeStone > MAX_LIFETIME_STONE ||
            !_scorpion._stoneView.activeInHierarchy)
            _scorpion.stateMachine.ChangeState(BossScorpionState.IdleScorpion);
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        _scorpion._anim.SetBool(FIRST_ATTACK_ANIM_SCORPION, false);
        _scorpion._stoneView.SetActive(false);
        _scorpion._stonePrefab.transform.position = _scorpion._targetShoot.position;
        _lifeTimeStone = 0;
        _pathPoints = null;
    }

    void GeneratePath()
    {
        _pathPoints = new List<Vector3>();

        Vector3 start = _scorpion._stonePrefab.transform.position; // Posición inicial de la piedra
        Vector3 mid = (start + _initialPosPlayer) / 2 + Vector3.up * 1.5f; // Punto intermedio elevado
        Vector3 end = _initialPosPlayer; // Posición final (jugador)

        int resolution = 30;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 point = CalculateBezierPoint(t, start, mid, end);
            _pathPoints.Add(point);
        }
    }

    void MoveStone()
    {
        if (_currentPointIndex < _pathPoints.Count)
        {
            Vector3 targetPoint = _pathPoints[_currentPointIndex];
            _scorpion._stonePrefab.transform.position = Vector3.MoveTowards(
                _scorpion._stonePrefab.transform.position,
                targetPoint,
                SPEED_PROJECTILE * Time.deltaTime
            );

            if (Vector3.Distance(_scorpion._stonePrefab.transform.position, targetPoint) < 0.1f)
                _currentPointIndex++;
        }
        else
        {
            // Sigue moviéndose en línea recta después de la trayectoria
            Vector3 direction =
                (_pathPoints[^1] - _pathPoints[^2]).normalized; // Dirección de la última sección de la trayectoria
            _scorpion._stonePrefab.transform.position += direction * SPEED_PROJECTILE * Time.deltaTime;
        }
    }


    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }
}
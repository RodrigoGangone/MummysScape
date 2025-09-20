using UnityEngine;
using static PlayerEnum;
/// <summary>
/// AttractState
/// Busca un IAttractable al frente y lo atrae un corto lapso hasta acercarlo.
/// Prioridad de Space: si estás en Head -> Smash; si no, Attract.
/// </summary>
public sealed class AttractState : State
{
    public override void OnEnter()
    {
        Debug.Log("AttractState");
    }

    public override void OnUpdate() { }

    public override void OnFixedUpdate() { }

    public override void OnExit() { }
}

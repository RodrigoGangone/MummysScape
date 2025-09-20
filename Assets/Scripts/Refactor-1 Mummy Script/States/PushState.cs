using UnityEngine;
using static PlayerEnum;

/// <summary>
/// PushState
/// Mantiene el ciclo de empuje: proyecta el input al eje permitido, mueve la caja vía IPushable,
/// hace soft‑snap lateral del player al centro horizontal de la cara y lo alinea mirando a la caja.
/// No gestiona las transiciones (eso lo hace el Driver); asume que al entrar hay un target válido.
/// </summary>
public sealed class PushState : State
{
    public override void OnEnter()
    {
        throw new System.NotImplementedException();
    }

    public override void OnExit()
    {
        throw new System.NotImplementedException();
    }

    public override void OnUpdate()
    {
        throw new System.NotImplementedException();
    }

    public override void OnFixedUpdate()
    {
        throw new System.NotImplementedException();
    }
}
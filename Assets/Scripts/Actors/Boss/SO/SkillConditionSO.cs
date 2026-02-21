using UnityEngine;

/// <summary>
/// Filtro Lógico: Clase base para crear condiciones modulares (como distancia o línea de visión) 
/// que determinan si una habilidad específica puede ser activada en un momento dado.
/// </summary>

public abstract class SkillConditionSO : ScriptableObject
{
    public abstract bool Evaluate(in WorldModel wm, IBossContext ctx);
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Condición abstracta para comporner lógica de disponibilidad de una habilidad (distancia, LOS, stage, etc.)
/// </summary>
public abstract class SkillConditionSO : ScriptableObject
{
    public abstract bool Evaluate(in WorldModel wm, IBossContext ctx);
}
using UnityEngine;

/// <summary>
/// Configuración Raíz: Centraliza la definición de las fases (stages) del Boss, sus slots de 
/// habilidades principales y los parámetros globales de velocidad y rotación.
/// </summary>

[CreateAssetMenu(fileName = "BossConfig", menuName = "Boss/Config")]
public sealed class BossConfigSO : ScriptableObject
{
    [Header("Stages")] [Tooltip("1 Por golpe que resiste")]
    [SerializeField] private StageStatsSO[] stages;

    [Header("Slots de habilidades")]
    [SerializeField] private BossSkillSO primarySkill;
    [SerializeField] private BossSkillSO secondarySkill;

    [Header("Movimiento")]
    [SerializeField] private MovementMode movementMode;
    [SerializeField, Min(0f)] private float baseMoveSpeed = 3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 8f;

    public MovementMode MovementMode => movementMode;
    public float BaseMoveSpeed => baseMoveSpeed;
    public float RotationSpeed => rotationSpeed;
    
    public int StageCount => stages?.Length ?? 0;

    public StageStatsSO GetStage(int index) => (index >= 0 && index < StageCount) ? stages[index] : null;
    public BossSkillSO PrimarySkill => primarySkill;
    public BossSkillSO SecondarySkill => secondarySkill;
}

public enum MovementMode { Stationary, Chase } // Stationary = No move
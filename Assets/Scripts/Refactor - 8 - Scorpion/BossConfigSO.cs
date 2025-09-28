using UnityEngine;

/// <summary>
/// Config raíz del Boss. Define sus stages y slots de habilidades (dos por requerimiento),
/// además de parámetros globales de percepción para el GOAP.
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

    public StageStatsSO GetStage(int index)
        => (index >= 0 && index < StageCount) ? stages[index] : null;

    public BossSkillSO PrimarySkill => primarySkill;
    public BossSkillSO SecondarySkill => secondarySkill;
}

public enum MovementMode { Stationary, Chase } // Stationary = No move
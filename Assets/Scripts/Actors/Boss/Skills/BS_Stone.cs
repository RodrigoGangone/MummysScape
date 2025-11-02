using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Stone Skill")]
public class BS_Stone : BossSkillSO
{
    [Header("Prefab & Spawn")]
    [SerializeField] private GameObject stonePrefab;
    [Tooltip("Nombre del child desde donde sale la piedra (opcional).")]
    [SerializeField] private string launchSocketName = "_targetShoot";
    [Tooltip("Si no hay socket, usa este offset local del boss.")]
    [SerializeField] private Vector3 localSpawnOffset = new Vector3(0f, 1.5f, 0.5f);

    [Header("Trayectoria (Bézier)")]
    [Tooltip("Duración base del vuelo (se divide por el speedMultiplier del stage).")]
    [Min(0.05f)] [SerializeField] private float baseFlightDuration = 1.1f;
    [SerializeField] private float arcHeight = 4f;
    
    [Header("Predicción")]
    [Tooltip("Tiempo usado para predecir la posición futura del player (no cambia con el stage).")]
    [Min(0.05f)] [SerializeField] private float aimTime = 1.1f;
    [Tooltip("Escala el lead (1 = velocidad * aimTime).")]
    [SerializeField] private float leadMultiplier = 1.0f;
    [Tooltip("Dispersion XZ para evitar caída perfecta.")]
    [SerializeField] private float impactScatterRadius = 0.6f;

    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        if (stonePrefab == null)
        {
            Debug.LogWarning("[BS_Stone] Asigná el stonePrefab en el asset.");
            return;
        }

        // --- 1) Posición de salida ---
        Transform launchTf = FindDescendantByName(ctx.Transform, launchSocketName) ?? ctx.Transform;
        Vector3 startPos = launchTf.TransformPoint(localSpawnOffset);

        // --- 2) Predicción de impacto (desacoplada del tiempo de vuelo) ---
        var player = ctx.Player;
        Vector3 playerPos = player.Tf.position;
        Rigidbody playerRb = player.Rb;
        Vector3 playerVel = playerRb != null ? playerRb.velocity : Vector3.zero;

        float leadTime = Mathf.Max(0.0f, leadMultiplier * aimTime);
        Vector3 endPos = playerPos + playerVel * leadTime;

        if (impactScatterRadius > 0f)
        {
            Vector2 rnd = Random.insideUnitCircle * impactScatterRadius; // círculo en XZ
            endPos += new Vector3(rnd.x, 0f, rnd.y);
        }

        // --- 3) Control point de la Bézier (punto medio elevado) ---
        Vector3 mid = 0.5f * (startPos + endPos);
        Vector3 control = mid + Vector3.up * arcHeight;

        // --- 4) Escalado por Stage (velocidad = 1 / duración) ---
        float stageMul = GetStageSpeedMultiplier(wm);
        float flightDuration = Mathf.Max(0.05f, baseFlightDuration / stageMul);

        // --- 5) Instanciar y lanzar ---
        GameObject go = Instantiate(stonePrefab, startPos, Quaternion.identity);
        var proj = go.GetComponent<StoneProjectile>();
        if (proj == null) proj = go.AddComponent<StoneProjectile>();
        proj.Initialize(startPos, control, endPos, flightDuration, ctx);
    }

    private static float GetStageSpeedMultiplier(in WorldModel wm)
    {
        // Intenta leer el StageStatsSO actual y tomar su speedMultiplier
        try
        {
            var stats = wm.Config != null ? wm.Config.GetStage(wm.StageIndex) : null;
            if (stats != null && stats.speedMultiplier > 0.001f)
                return stats.speedMultiplier;
        }
        catch { /* por seguridad ante nulls o índices fuera de rango */ }

        return 1f; // fallback
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name == name) return c;
            var deep = FindDescendantByName(c, name);
            if (deep != null) return deep;
        }
        return null;
    }
}

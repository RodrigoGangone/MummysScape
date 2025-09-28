using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Geyser Skill")]
public class BS_Geyser : BossSkillSO
{
    [Header("Selección")] 
    [SerializeField] private GameObject providerEmpty;

    [Header("Radio alrededor del Player (XZ)")] [Min(0.1f)] 
    [SerializeField] private float baseRadius = 8f;

    [Tooltip("Si querés, multiplicá el radio por el speedMultiplier del stage.")] 
    [SerializeField] private bool scaleRadiusWithStage = false;

    [Tooltip("Máximo de puntos a activar (0 = sin límite). Luego del filtro, toma los más cercanos.")] 
    [SerializeField] private int maxPoints = 0;
    GeyserPointProvider Provider => providerEmpty.GetComponent<GeyserPointProvider>();

    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        if (Provider == null || Provider.points == null || Provider.points.Count == 0)
        {
            Debug.LogWarning("[BS_Geysers] No se encontró GeyserPointsProvider o la lista está vacía.");
            return;
        }

        // Radio efectivo
        float radius = baseRadius;
        if (scaleRadiusWithStage && wm.Config != null)
        {
            var stats = wm.Config.GetStage(wm.StageIndex);
            if (stats != null && stats.speedMultiplier > 0.001f)
                radius *= stats.speedMultiplier;
        }

        // Posición del player y distancias en plano XZ
        Vector3 p = ctx.Player.transform.position;

        var baseQuery = Provider.points
            .Select((tf, idx) => new { tf, idx, dist = PlanarDistanceXZ(tf.position, p) })
            .Where(x => x.tf != null && x.dist <= radius)
            .OrderBy(x => x.dist);

        var chosen = (maxPoints > 0 ? baseQuery.Take(maxPoints) : baseQuery).ToList();

        if (chosen.Count == 0)
        {
            Debug.Log($"[BS_Geysers] Ningún geyserpoint dentro del radio ({radius:F1} u).");
            return;
        }

        // SOLO LOGS por punto seleccionado (no se toca la lógica del Geyser)
        foreach (var x in chosen)
        {
            int N = x.idx + 1; // índice 1-based, según orden del provider
            Debug.Log($"[BS_Geysers] Habilitado geyser en geyserpoint -{N}-");
        }

        Debug.Log($"[BS_Geysers] Seleccionados {chosen.Count}/{Provider.points.Count} dentro de {radius:F1} u.");
    }
    
    private static float PlanarDistanceXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Skills/Scorpion/Geyser Skill (Solo Geysers)")]
public class BS_Geyser : BossSkillSO
{
    [Header("Selección")]
    [Min(0.1f)] [SerializeField] private float baseRadius = 6f;
    [SerializeField] private bool scaleRadiusWithStage = false;

    [Header("FX de viaje")]
    [SerializeField] private ParticleSystem travelParticlesPrefab;
    [Min(0.1f)] [SerializeField] private float travelSpeed = 10f; 

    private GeyserPointProvider Provider => FindObjectOfType<GeyserPointProvider>();

    protected override void Execute(in WorldModel wm, IBossContext ctx)
    {
        if (Provider == null)
        {
            Debug.LogWarning("[BS_Geyser] No hay GeyserPointProvider en escena.");
            return;
        }
        if (travelParticlesPrefab == null)
        {
            Debug.LogWarning("[BS_Geyser] Falta TravelParticles prefab.");
            return;
        }

        // Radio efectivo
        float radius = baseRadius;
        if (scaleRadiusWithStage && wm.Config != null)
        {
            var stageCfg = wm.Config.GetStage(wm.StageIndex);
            if (stageCfg != null && stageCfg.speedMultiplier > 0.001f)
                radius *= stageCfg.speedMultiplier;
        }

        Vector3 playerPos = ctx.Player.Tf.position;

        // 1) Detectar geysers cercanos
        List<Geyser> selection =
            Provider.geysers
             .Where(g => g != null)
             .Select(g => new { g, dist = PlanarDistanceXZ(g.transform.position, playerPos) })
             .Where(x => x.dist <= radius)
             .OrderBy(x => x.dist)
             .Select(x => x.g)
             .ToList();

        if (selection.Count == 0)
        {
            Debug.Log($"[BS_Geyser] Ningún Geyser dentro del radio {radius:F1}.");
            return;
        }

        // Origen de viaje (viewScorpion del boss, p.defaultTravelOrigin o transform del boss)
        var origin = (Provider.defaultTravelOrigin != null ? Provider.defaultTravelOrigin : ctx.Transform);

        // 2) Enviar una partícula por geyser seleccionado
        Provider.RunTravelFXToGeysers(
            selection,
            travelParticlesPrefab,
            travelSpeed,
            origin,
            onArrived: () =>
            {
                // 3) Activar los Geysers correspondientes (por unos segundos, según su propia lógica interna)
                foreach (var g in selection.Where(g => g != null)) g.ActivateIntenseMode(null);
            });

        
        // El estado puede salir aquí; el FX continúa en background.
    }

    private static float PlanarDistanceXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

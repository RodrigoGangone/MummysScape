using UnityEngine;

public class WindParticlesCameraAligned : MonoBehaviour
{
public ParticleSystem ps;
    public Camera cam;

    [Tooltip("Usar solo yaw (mantener 'arriba' del mundo). Si false, usa up de la cámara.")]
    public bool yawOnly = false;

    [Tooltip("Si true, conserva el componente Z original (forward del emisor). Si false, lo mapea a forward de la cámara.")]
    public bool keepEmitterZ = true;

    private ParticleSystem.Particle[] buffer;

    void Reset() { ps = GetComponent<ParticleSystem>(); }

    void LateUpdate()
    {
        if (!ps) ps = GetComponent<ParticleSystem>();
        if (!cam) cam = Camera.main;
        if (!ps || !cam) return;

        int alive = ps.particleCount;
        if (alive == 0) return;

        if (buffer == null || buffer.Length < alive)
            buffer = new ParticleSystem.Particle[Mathf.Max(256, alive)];

        int count = ps.GetParticles(buffer);

        // Bases del emisor (lo que tu PS ya usa para X/Y/Z)
        Transform t = ps.transform;
        Vector3 ex = t.right,  ey = t.up,     ez = t.forward;

        // Bases de la cámara
        Vector3 cx = cam.transform.right;
        Vector3 cy = yawOnly ? Vector3.up : cam.transform.up;
        Vector3 cz = cam.transform.forward;
        if (yawOnly)
        {
            // Si querés también ajustar cz a yaw (opcional)
            Vector3 f = cam.transform.forward; f.y = 0f;
            if (f.sqrMagnitude > 1e-6f) cz = f.normalized;
        }

        // Umbral de “recién nacido”: ~un frame de edad
        float bornThreshold = Mathf.Max(Time.deltaTime * 1.5f, 0.02f);

        for (int i = 0; i < count; i++)
        {
            var p = buffer[i];
            float age = p.startLifetime - p.remainingLifetime;
            if (age <= bornThreshold)
            {
                // 1) Descomponer la velocidad actual en los ejes del EMISOR
                float vx = Vector3.Dot(p.velocity, ex);
                float vy = Vector3.Dot(p.velocity, ey);
                float vz = Vector3.Dot(p.velocity, ez);

                // 2) Remapear SOLO XY a los ejes de la CÁMARA
                Vector3 v = cx * vx + cy * vy;
                v += (keepEmitterZ ? ez * vz : cz * vz);

                p.velocity = v;
                buffer[i] = p;
            }
        }

        ps.SetParticles(buffer, count);
    }
}

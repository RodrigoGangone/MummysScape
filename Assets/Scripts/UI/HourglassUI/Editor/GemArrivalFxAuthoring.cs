using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Genera los assets nativos del impacto y permite revisar su escala real en el HUD.</summary>
public static class GemArrivalFxAuthoring
{
    public const string Folder = "Assets/Art/VFX/GemArrival";
    public const string PrefabPath = Folder + "/ArrivalFX.prefab";
    public const string UiPath = "Assets/Prefabs/UI/UIRAW'S.prefab";
    private const string HourglassPath = "Assets/Prefabs/UI/Hourglass_low.prefab";
    private const string GemMaterialPath = "Assets/Art/Environment/Modules/Materials/Zone 1 -Pyramids/BeatleTex/Gem1.mat";

    [MenuItem("Tools/Gems/Build Arrival FX")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Crear el efecto fuera de Play Mode.");

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Art/VFX", "GemArrival");

        Material crystal = SaveAsset(new Material(AssetDatabase.LoadAssetAtPath<Material>(GemMaterialPath))
            { name = "Arrival Crystal" }, Folder + "/ArrivalCrystal.mat");
        crystal.SetFloat("_IsPicked", 1f);
        crystal.SetFloat("_MovementStrength", 0f);
        EditorUtility.SetDirty(crystal);

        var glow = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Arrival Flash" };
        glow.SetFloat("_Surface", 1f);
        glow.SetFloat("_Blend", 0f);
        glow.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        glow.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        glow.SetFloat("_ZWrite", 0f);
        glow.SetFloat("_Cull", (float)CullMode.Off);
        glow.SetColor("_BaseColor", Color.white);
        glow.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glow.renderQueue = (int)RenderQueue.Transparent;
        glow = SaveAsset(glow, Folder + "/ArrivalFlash.mat");

        Mesh shard = SaveAsset(CreateShard("Crystal Shard", 1f, 0f), Folder + "/CrystalShard.asset");
        Mesh splinter = SaveAsset(CreateShard("Crystal Splinter", 1.5f, 0.16f), Folder + "/CrystalSplinter.asset");
        Mesh star = SaveAsset(CreateStar(), Folder + "/ImpactStar.asset");
        Mesh ring = SaveAsset(CreateRing(), Folder + "/ImpactRing.asset");

        var root = new GameObject("ArrivalFX");
        try
        {
            ParticleSystem fragments = AddSystem(root, null, crystal, shard, 14, 0.42f, 0.64f, 0.15f, 0.27f, 1.35f, 2.15f);
            ConfigureFragments(fragments, -1.4f, 1.8f);
            ParticleSystem chips = AddSystem(root, "Small Crystal Chips", crystal, splinter, 9, 0.24f, 0.43f, 0.065f, 0.12f, 1.8f, 2.8f);
            ConfigureFragments(chips, -0.8f, 2.4f);

            ParticleSystem flash = AddSystem(root, "Contact Flash", glow, star, 1, 0.13f, 0.13f, 0.52f, 0.52f, 0f, 0f);
            ConfigureAccent(flash, new Color(0.8f, 0.92f, 1f, 1f), new Color(0.59f, 0.41f, 1f, 0f),
                new AnimationCurve(new Keyframe(0f, 0.65f), new Keyframe(0.18f, 1f), new Keyframe(1f, 0f)));
            ParticleSystem shock = AddSystem(root, "Crystal Impact Ring", glow, ring, 1, 0.28f, 0.28f, 1.22f, 1.22f, 0f, 0f);
            ConfigureAccent(shock, new Color(0.68f, 0.56f, 1f, 0.85f), new Color(0.3f, 0.51f, 1f, 0f),
                AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f));

            var appearance = root.AddComponent<GemArrivalVfx>();
            var settings = new SerializedObject(appearance);
            SerializedProperty renderers = settings.FindProperty("_fragmentRenderers");
            renderers.arraySize = 2;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = fragments.GetComponent<ParticleSystemRenderer>();
            renderers.GetArrayElementAtIndex(1).objectReferenceValue = chips.GetComponent<ParticleSystemRenderer>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }

        WireHourglassPivot();
        WireSharedUi();
        foreach (string guid in AssetDatabase.FindAssets("", new[] { Folder }))
            AssetDatabase.SaveAssetIfDirty(new GUID(guid));
        Debug.Log("[Gem Arrival] ArrivalFX creado y asignado; fragmentos, destello, anillo, golpe del reloj y shake listos.");
        GemArrivalFxPreview.Open();
    }

    private static T SaveAsset<T>(T asset, string path) where T : UnityEngine.Object
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        EditorUtility.CopySerialized(asset, existing);
        UnityEngine.Object.DestroyImmediate(asset);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static ParticleSystem AddSystem(GameObject root, string name, Material material, Mesh mesh,
        short count, float minLife, float maxLife, float minSize, float maxSize, float minSpeed, float maxSpeed)
    {
        GameObject go = name == null ? root : new GameObject(name);
        if (go != root) go.transform.SetParent(root.transform, false);
        go.layer = 22;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.08f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.useUnscaledTime = false;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        var shape = ps.shape;
        shape.enabled = minSpeed > 0f;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.035f;
        shape.radiusThickness = 1f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return ps;
    }

    private static void ConfigureFragments(ParticleSystem ps, float gravity, float spin)
    {
        var main = ps.main;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(-spin, spin);
        rotation.y = new ParticleSystem.MinMaxCurve(-spin * 1.7f, spin * 1.7f);
        rotation.z = new ParticleSystem.MinMaxCurve(-spin * 2f, spin * 2f);
        var force = ps.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.Local;
        force.y = gravity;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.65f), new Keyframe(0.08f, 1f), new Keyframe(0.45f, 0.85f), new Keyframe(1f, 0f)));
    }

    private static void ConfigureAccent(ParticleSystem ps, Color start, Color end, AnimationCurve scale)
    {
        ps.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, scale);
    }

    private static Mesh CreateShard(string name, float length, float skew)
    {
        Vector3[] points = {
            new(skew, 0.65f * length, 0.05f), new(-skew * 0.5f, -0.48f * length, -0.05f),
            new(-0.35f, 0.1f, -0.22f), new(0.3f, 0.02f, -0.25f),
            new(0.27f, -0.04f, 0.24f), new(-0.24f, 0.12f, 0.25f)
        };
        int[] faces = { 0,3,2, 0,4,3, 0,5,4, 0,2,5, 1,2,3, 1,3,4, 1,4,5, 1,5,2 };
        var vertices = new Vector3[faces.Length];
        var uv = new Vector2[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            vertices[i] = points[faces[i]];
            uv[i] = new Vector2(vertices[i].x + 0.5f, vertices[i].y / length + 0.5f);
        }
        var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = Enumerable.Range(0, faces.Length).ToArray() };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static Mesh CreateStar()
    {
        var vertices = new List<Vector3> { Vector3.zero };
        var triangles = new List<int>();
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            float radius = i % 2 == 0 ? 0.5f : 0.09f;
            vertices.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            triangles.AddRange(new[] { 0, i + 1, (i + 1) % 8 + 1 });
        }
        return PlanarMesh("Impact Star", vertices, triangles);
    }

    private static Mesh CreateRing()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        const int count = 48;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 direction = new(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            vertices.Add(direction * 0.5f);
            vertices.Add(direction * 0.475f);
            int n = (i + 1) % count * 2;
            triangles.AddRange(new[] { i * 2, n, n + 1, i * 2, n + 1, i * 2 + 1 });
        }
        return PlanarMesh("Impact Ring", vertices, triangles);
    }

    private static Mesh PlanarMesh(string name, List<Vector3> vertices, List<int> triangles)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.uv = vertices.Select(v => new Vector2(v.x + 0.5f, v.y + 0.5f)).ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void WireHourglassPivot()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(HourglassPath);
        // El pivote está serializado en el prefab base y conserva los IDs de Models/Animator.
        // No reserializar componentes legados ajenos al efecto al regenerar sus partículas.
        if (root.transform.Find("Gem Arrival Impact/Models") == null)
            throw new InvalidOperationException("Falta el pivote Gem Arrival Impact/Models en Hourglass_low.");
    }

    private static void WireSharedUi()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(UiPath);
        try
        {
            UIGemManager manager = root.GetComponentInChildren<UIGemManager>(true);
            Transform pivot = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Gem Arrival Impact");
            Camera hourglassCamera = pivot.parent.GetComponentInChildren<Camera>(true);
            Camera gemCamera = root.GetComponentsInChildren<Camera>(true).Single(c => c.targetTexture != null && c.targetTexture.name == "GemUI");
            UIGemArrivalImpact impact = manager.GetComponent<UIGemArrivalImpact>() ?? manager.gameObject.AddComponent<UIGemArrivalImpact>();
            var impactSettings = new SerializedObject(impact);
            impactSettings.FindProperty("_hourglassTarget").objectReferenceValue = pivot;
            impactSettings.FindProperty("_hourglassCamera").objectReferenceValue = hourglassCamera;
            impactSettings.FindProperty("_impulseSource").objectReferenceValue = root.GetComponentInChildren<CinemachineImpulseSource>(true);
            impactSettings.ApplyModifiedPropertiesWithoutUndo();
            var settings = new SerializedObject(manager);
            settings.FindProperty("_arrivalVfxPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<ParticleSystem>();
            settings.FindProperty("_gemCamera").objectReferenceValue = gemCamera;
            settings.FindProperty("_arrivalImpact").objectReferenceValue = impact;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, UiPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}

public sealed class GemArrivalFxPreview : EditorWindow
{
    private PreviewRenderUtility _preview;
    private GameObject _gemRoot;
    private ParticleSystem _effect;
    private Material[] _materials;
    private float _time = 0.1f;
    private int _slot = 1;
    private bool _playing;
    private double _lastTime;

    [MenuItem("Tools/Gems/Preview Arrival FX")]
    public static void Open() => GetWindow<GemArrivalFxPreview>("Gem Arrival Preview");

    private void OnEnable() { _lastTime = EditorApplication.timeSinceStartup; }

    private void Setup()
    {
        if (_preview != null) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(GemArrivalFxAuthoring.PrefabPath) == null) return;
        _preview = new PreviewRenderUtility();
        GameObject ui = PrefabUtility.LoadPrefabContents(GemArrivalFxAuthoring.UiPath);
        try
        {
            Transform gems = ui.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Gem_UI");
            _gemRoot = Instantiate(gems.gameObject);
            _preview.AddSingleGO(_gemRoot);
            Camera source = _gemRoot.GetComponentInChildren<Camera>(true);
            _preview.camera.CopyFrom(source);
            _preview.camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            // CopyFrom también copia la máscara de la escena aislada del prefab.
            _preview.camera.overrideSceneCullingMask = _gemRoot.sceneCullingMask;
            _preview.camera.targetTexture = null;
            _preview.camera.aspect = 1f;
            _preview.camera.clearFlags = CameraClearFlags.SolidColor;
            _preview.camera.backgroundColor = new Color(0.032f, 0.022f, 0.062f, 1f);
            source.enabled = false;
            _materials = _gemRoot.GetComponentsInChildren<MeshRenderer>().Select(r => {
                Material material = new(r.sharedMaterial);
                material.SetFloat("_IsPicked", 1f);
                r.sharedMaterial = material;
                return material;
            }).ToArray();
            CreateEffect();
        }
        finally { PrefabUtility.UnloadPrefabContents(ui); }
    }

    private void CreateEffect()
    {
        if (_effect != null) DestroyImmediate(_effect.gameObject);
        Transform slot = _gemRoot.GetComponentsInChildren<Transform>().Single(t => t.name == "Collectible_" + _slot);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GemArrivalFxAuthoring.PrefabPath);
        GameObject go = Instantiate(prefab, _gemRoot.transform);
        go.transform.SetPositionAndRotation(slot.position - _preview.camera.transform.forward * 0.3f, _preview.camera.transform.rotation);
        _effect = go.GetComponent<ParticleSystem>();
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            ps.useAutoRandomSeed = false;
            ps.randomSeed = 41;
        }
    }

    private void OnGUI()
    {
        Setup();
        if (_preview == null) { EditorGUILayout.HelpBox("Primero ejecutar Tools > Gems > Build Arrival FX.", MessageType.Info); return; }
        EditorGUILayout.LabelField("Impacto de cristal · tamaño real del HUD: 280 × 280", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(_playing ? "Pausar" : "Reproducir")) { _playing = !_playing; _lastTime = EditorApplication.timeSinceStartup; }
        if (GUILayout.Button("Repetir")) { _time = 0f; _playing = true; _lastTime = EditorApplication.timeSinceStartup; }
        int slot = EditorGUILayout.IntSlider("Gema", _slot, 1, 3);
        if (slot != _slot) { _slot = slot; CreateEffect(); }
        EditorGUILayout.EndHorizontal();
        _time = EditorGUILayout.Slider("Tiempo (s)", _time, 0f, 1f);
        if (Event.current.type == EventType.Repaint)
        {
            _effect.Simulate(_time, true, true, false);
            Rect area = new(12f, 88f, Mathf.Min(560f, position.width - 24f), Mathf.Min(560f, position.width - 24f));
            _preview.BeginPreview(area, GUIStyle.none);
            _preview.Render(true);
            Texture texture = _preview.EndPreview();
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit);
        }
        if (_playing)
        {
            double now = EditorApplication.timeSinceStartup;
            _time += (float)(now - _lastTime);
            _lastTime = now;
            if (_time > 1f) _time = 0f;
            Repaint();
        }
    }

    private void OnDisable()
    {
        _preview?.Cleanup();
        _preview = null;
        if (_materials != null)
            foreach (Material material in _materials) DestroyImmediate(material);
    }
}

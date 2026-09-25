using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

[Category("GemArrivalFeedback")]
public sealed class GemArrivalFeedbackTests
{
    private const string ArrivalPath = "Assets/Art/VFX/GemArrival/ArrivalFX.prefab";
    private const string UiPath = "Assets/Prefabs/UI/UIRAW'S.prefab";

    [Test]
    public void SharedUiAssignsArrivalToAllThreeCameraVisibleSlots()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(UiPath);
        Assert.That(root, Is.Not.Null);
        var manager = root.GetComponentInChildren<UIGemManager>(true);
        Assert.That(manager, Is.Not.Null);
        var settings = new SerializedObject(manager);
        var expected = AssetDatabase.LoadAssetAtPath<GameObject>(ArrivalPath);
        Assert.That(expected, Is.Not.Null);
        Assert.That(Reference<ParticleSystem>(settings, "_arrivalVfxPrefab"),
            Is.EqualTo(expected.GetComponent<ParticleSystem>()));

        var camera = Reference<Camera>(settings, "_gemCamera");
        Assert.That(camera, Is.Not.Null);
        Assert.That(camera.targetTexture, Is.Not.Null);
        Assert.That(Reference<UIGemArrivalImpact>(settings, "_arrivalImpact"), Is.Not.Null);
        var targets = settings.FindProperty("_gemTargets");
        Assert.That(targets.arraySize, Is.EqualTo(3));
        var unique = new HashSet<Transform>();
        for (int i = 0; i < targets.arraySize; i++)
        {
            var target = targets.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            Assert.That(target, Is.Not.Null);
            Assert.That(unique.Add(target), Is.True, "Cada gema necesita un destino propio.");
            Assert.That(target.IsChildOf(camera.transform.parent), Is.True);
            Assert.That(camera.cullingMask & (1 << target.gameObject.layer), Is.Not.Zero,
                $"La cámara no renderiza el slot {i + 1}.");
            Vector3 viewport = camera.WorldToViewportPoint(target.position);
            Assert.That(viewport.x, Is.EqualTo(0.25f + 0.25f * i).Within(0.03f));
            Assert.That(viewport.y, Is.EqualTo(0.57f).Within(0.03f));
            Assert.That(viewport.z, Is.GreaterThan(camera.nearClipPlane));
            Assert.That(viewport.z, Is.LessThan(camera.farClipPlane));
        }
    }

    [Test]
    public void ArrivalHasFiniteVisibleMeshFragmentsAndValidMaterials()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(ArrivalPath);
        Assert.That(root, Is.Not.Null);
        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        Assert.That(systems, Is.Not.Empty);
        int meshSystems = 0;
        foreach (var particles in systems)
        {
            Assert.That(particles.main.loop, Is.False, particles.name);
            Assert.That(particles.main.playOnAwake, Is.False, particles.name);
            Assert.That(particles.main.duration, Is.GreaterThan(0f), particles.name);
            Assert.That(particles.main.startLifetime.constantMax, Is.GreaterThan(0f), particles.name);
            Assert.That(particles.main.maxParticles, Is.GreaterThan(0), particles.name);
            Assert.That(particles.gameObject.layer, Is.EqualTo(22), particles.name);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.enabled, Is.True, particles.name);
            Assert.That(renderer.sharedMaterials, Is.Not.Empty, particles.name);
            foreach (var material in renderer.sharedMaterials)
            {
                Assert.That(material, Is.Not.Null, particles.name);
                Assert.That(material.shader, Is.Not.Null, particles.name);
                Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False, material.name);
            }
            if (renderer.renderMode != ParticleSystemRenderMode.Mesh) continue;
            meshSystems++;
            Assert.That(renderer.mesh, Is.Not.Null, "Los fragmentos deben tener geometría real.");
            Assert.That(renderer.mesh.vertexCount, Is.GreaterThan(3));
        }
        Assert.That(meshSystems, Is.GreaterThan(0), "La colisión necesita fragmentos de gema.");

        var tint = root.GetComponent<GemArrivalVfx>();
        Assert.That(tint, Is.Not.Null);
        var fragments = new SerializedObject(tint).FindProperty("_fragmentRenderers");
        Assert.That(fragments.arraySize, Is.GreaterThan(0));
        for (int i = 0; i < fragments.arraySize; i++)
            Assert.That(fragments.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
    }

    [Test]
    public void HourglassImpactDoesNotAnimateItsCameraOrHeartbeatRoot()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(UiPath);
        var impact = root.GetComponentInChildren<UIGemArrivalImpact>(true);
        Assert.That(impact, Is.Not.Null);
        var settings = new SerializedObject(impact);
        var target = Reference<Transform>(settings, "_hourglassTarget");
        var camera = Reference<Camera>(settings, "_hourglassCamera");
        Assert.That(target, Is.Not.Null);
        Assert.That(camera, Is.Not.Null);
        Assert.That(target.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
        Assert.That(camera.transform.IsChildOf(target), Is.False,
            "La cámara debe quedar fuera del pivote para que se vea el golpe.");
        var hourglass = root.GetComponentInChildren<HourglassManager>(true);
        Assert.That(hourglass, Is.Not.Null);
        var heartbeat = Reference<Transform>(new SerializedObject(hourglass), "_heartbeatTarget");
        Assert.That(target, Is.Not.EqualTo(heartbeat), "El impacto no debe competir con el latido.");
        Assert.That(settings.FindProperty("_impulseSource").objectReferenceValue, Is.Not.Null);
        var hourglassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/Hourglass_low.prefab");
        Assert.That(hourglassPrefab, Is.Not.Null);
        var pivot = hourglassPrefab.transform.Find("Gem Arrival Impact");
        Assert.That(pivot, Is.Not.Null);
        Assert.That(pivot.Find("Models"), Is.Not.Null);
    }

    private static T Reference<T>(SerializedObject settings, string field) where T : Object =>
        settings.FindProperty(field).objectReferenceValue as T;
}

// These tests use a temporary empty scene and never load levels or write Save/PlayerPrefs.
[Category("GemArrivalFeedback")]
public sealed class GemArrivalFeedbackPlayModeTests
{
    private readonly List<GameObject> _roots = new List<GameObject>();
    private readonly List<Material> _materials = new List<Material>();
    private GameEventManager _events;
    private GameEvent _pause;
    private GameEvent _arrival;

    [UnitySetUp]
    public IEnumerator EnterPlay()
    {
        GemArrivalTestScenes.OpenEmpty();
        yield return new EnterPlayMode();
        Assert.That(GameEventManager.Instance == null, Is.True,
            "Ejecutar las pruebas de feedback en una escena vacía para preservar la partida.");
        Time.timeScale = 1f;
        _pause = ScriptableObject.CreateInstance<GameEvent>();
        _arrival = ScriptableObject.CreateInstance<GameEvent>();
        var eventRoot = Root("Gem feedback test events");
        _events = eventRoot.AddComponent<GameEventManager>();
        _events.levelEvents = new GameEventManager.LevelEvents
        {
            OnPauseChanged = _pause,
            OnGemReachedUI = _arrival
        };
        eventRoot.SetActive(true);
    }

    [UnityTearDown]
    public IEnumerator LeavePlay()
    {
        for (int i = _roots.Count - 1; i >= 0; i--)
        {
            if (_roots[i] == null) continue;
            _roots[i].SetActive(false);
            Object.Destroy(_roots[i]);
        }
        yield return null;
        if (_pause != null) Object.Destroy(_pause);
        if (_arrival != null) Object.Destroy(_arrival);
        foreach (Material material in _materials)
            if (material != null) Object.Destroy(material);
        _materials.Clear();
        if (GameEventManager.Instance == _events) GameEventManager.Instance = null;
        _roots.Clear();
        Time.timeScale = 1f;
        if (EditorApplication.isPlaying)
            yield return new ExitPlayMode();
        GemArrivalTestScenes.Restore();
    }

    [UnityTest]
    public IEnumerator ArrivalsKeepTheirFragmentsWhilePausedThenCleanUpWithoutReplayingFinishedFlash()
    {
        var uiAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UIRAW'S.prefab");
        var source = new SerializedObject(uiAsset.GetComponentInChildren<UIGemManager>(true));
        var sourceMaterials = source.FindProperty("_gemMaterials");
        var materials = new Material[3];
        var targets = new Transform[3];
        var root = Root("Isolated arrival particles");
        for (int i = 0; i < 3; i++)
        {
            materials[i] = new Material((Material)sourceMaterials.GetArrayElementAtIndex(i).objectReferenceValue);
            _materials.Add(materials[i]);
            targets[i] = new GameObject($"Temporary slot {i + 1}").transform;
            targets[i].SetParent(root.transform, false);
            targets[i].localPosition = Vector3.right * (i - 1);
            targets[i].gameObject.layer = 22;
        }

        var cameraRoot = Root("Arrival test camera");
        var camera = cameraRoot.AddComponent<Camera>();
        camera.transform.position = Vector3.back * 10f;
        cameraRoot.SetActive(true);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/GemArrival/ArrivalFX.prefab");
        var manager = root.AddComponent<UIGemManager>();
        Set(manager, "_gemMaterials", materials);
        Set(manager, "_gemTargets", targets);
        Set(manager, "_gemCamera", camera);
        Set(manager, "_arrivalVfxPrefab", prefab.GetComponent<ParticleSystem>());
        root.SetActive(true);
        yield return null; // Start reads saved state into temporary materials only.

        _arrival.Raise(1);
        _arrival.Raise(2);
        _arrival.Raise(3);
        var effects = root.GetComponentsInChildren<GemArrivalVfx>();
        Assert.That(effects.Length, Is.EqualTo(3));
        var flashes = new ParticleSystem[3];
        var fragments = new ParticleSystem[3];
        for (int i = 0; i < effects.Length; i++)
        {
            Assert.That(materials[i].GetFloat("_IsPicked"), Is.EqualTo(1f));
            Assert.That(
                Vector3.Distance(effects[i].transform.position, targets[i].position),
                Is.LessThan(0.00001f),
                $"El Arrival de la gema {i + 1} debe ejecutarse exactamente sobre su target.");            flashes[i] = effects[i].transform.Find("Contact Flash").GetComponent<ParticleSystem>();
            fragments[i] = effects[i].GetComponent<ParticleSystem>();
            var renderers = new SerializedObject(effects[i]).FindProperty("_fragmentRenderers");
            for (int j = 0; j < renderers.arraySize; j++)
            {
                var renderer = (ParticleSystemRenderer)renderers.GetArrayElementAtIndex(j).objectReferenceValue;
                Assert.That(renderer.sharedMaterial, Is.EqualTo(materials[i]));
            }
            foreach (Transform child in effects[i].GetComponentsInChildren<Transform>())
                Assert.That(child.gameObject.layer, Is.EqualTo(22));
        }

        yield return new WaitForSecondsRealtime(0.16f);
        for (int i = 0; i < flashes.Length; i++)
        {
            Assert.That(flashes[i].IsAlive(false), Is.False, "El flash debe haber terminado antes de pausar.");
            Assert.That(fragments[i].IsAlive(true), Is.True, "Los fragmentos deben durar más que el flash.");
        }
        _pause.Raise(true);
        float[] pausedTimes = { fragments[0].time, fragments[1].time, fragments[2].time };
        yield return new WaitForSecondsRealtime(0.75f);
        Assert.That(root.GetComponentsInChildren<GemArrivalVfx>().Length, Is.EqualTo(3),
            "La limpieza no debe destruir efectos que siguen vivos durante pausa.");
        for (int i = 0; i < fragments.Length; i++)
        {
            Assert.That(fragments[i].isPaused, Is.True);
            Assert.That(fragments[i].time, Is.EqualTo(pausedTimes[i]).Within(0.00001f));
        }

        _pause.Raise(false);
        yield return null;
        for (int i = 0; i < flashes.Length; i++)
        {
            Assert.That(flashes[i].IsAlive(false), Is.False, "Reanudar no debe volver a emitir un flash terminado.");
            Assert.That(flashes[i].particleCount, Is.Zero);
        }
        yield return Until(() => root.GetComponentsInChildren<GemArrivalVfx>().Length == 0);
        Assert.That(root.GetComponentsInChildren<ParticleSystem>(), Is.Empty);
    }

    [UnityTest]
    public IEnumerator ImpactFreezesDuringPauseAndRetriggersWithoutPermanentPoseDrift()
    {
        var root = Root("Isolated hourglass impact");

        var target = new GameObject("Dedicated model pivot").transform;
        target.SetParent(root.transform);
        target.localPosition = new Vector3(1.2f, 2.3f, -0.7f);
        target.localRotation = Quaternion.Euler(12f, 34f, 5f);
        target.localScale = new Vector3(0.8f, 1.1f, 0.9f);

        Vector3 restPosition = target.localPosition;
        Quaternion restRotation = target.localRotation;
        Vector3 restScale = target.localScale;

        // Representa la gema concreta que recibió el Arrival.
        var gemTarget = new GameObject("Impacted gem").transform;
        gemTarget.SetParent(root.transform);
        gemTarget.localPosition = new Vector3(2f, 1f, 0f);

        var impact = root.AddComponent<UIGemArrivalImpact>();

        Set(impact, "_hourglassTarget", target);
        Set(impact, "_duration", 0.6f);
        Set(impact, "_shakeForce", 0f);

        root.SetActive(true);

        impact.Play(1, gemTarget);

        yield return Until(() =>
            (target.localPosition - restPosition).sqrMagnitude > 0.000001f);

        impact.SetPaused(true);

        Vector3 pausedPosition = target.localPosition;
        Quaternion pausedRotation = target.localRotation;
        Vector3 pausedScale = target.localScale;

        yield return new WaitForSecondsRealtime(0.7f);

        AssertPose(
            target,
            pausedPosition,
            pausedRotation,
            pausedScale);

        impact.SetPaused(false);
        impact.Play(3, gemTarget);

        yield return new WaitForSecondsRealtime(0.75f);

        AssertPose(
            target,
            restPosition,
            restRotation,
            restScale);

        impact.Play(2, gemTarget);

        yield return Until(() =>
            (target.localPosition - restPosition).sqrMagnitude > 0.000001f);

        impact.enabled = false;

        AssertPose(
            target,
            restPosition,
            restRotation,
            restScale);
    }
    [UnityTest]
    public IEnumerator PauseEventFreezesConcurrentFlightsAndForcedCompletionArrivesOnlyOnce()
    {
        var cameraRoot = Root("Flight test camera");
        var camera = cameraRoot.AddComponent<Camera>();
        camera.transform.position = Vector3.back * 10f;
        cameraRoot.SetActive(true);
        var canvasRoot = Root("Flight test canvas", typeof(RectTransform));
        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var animator = canvasRoot.AddComponent<GemFlightAnimator>();
        var visual = Root("Flight visual", typeof(RectTransform));
        Set(animator, "_worldCamera", camera);
        Set(animator, "_canvasRect", (RectTransform)canvasRoot.transform);
        Set(animator, "_gemUiPrefab", visual);
        Set(animator, "_travelDuration", 0.3f);
        canvasRoot.SetActive(true);
        yield return null;

        int arrivals = 0;
        animator.PlayFromWorld(Vector3.zero, new Vector2(120f, 80f), () => arrivals++);
        animator.PlayFromWorld(Vector3.right, new Vector2(-120f, 80f), () => arrivals++);
        _pause.Raise(true);
        var first = (RectTransform)canvasRoot.transform.GetChild(0);
        var second = (RectTransform)canvasRoot.transform.GetChild(1);
        Vector2 firstPaused = first.anchoredPosition;
        Vector2 secondPaused = second.anchoredPosition;
        yield return new WaitForSecondsRealtime(0.45f);
        Assert.That(first.anchoredPosition, Is.EqualTo(firstPaused));
        Assert.That(second.anchoredPosition, Is.EqualTo(secondPaused));
        Assert.That(animator.ActiveFlightCount, Is.EqualTo(2));
        Assert.That(arrivals, Is.Zero);

        _pause.Raise(false);
        yield return Until(() => arrivals == 2);
        Assert.That(animator.ActiveFlightCount, Is.Zero);
        yield return null;
        Assert.That(canvasRoot.transform.childCount, Is.Zero);

        _pause.Raise(true);
        animator.PlayFromWorld(Vector3.zero, Vector2.one, () => arrivals++);
        animator.PlayFromWorld(Vector3.right, Vector2.zero, () => arrivals++);
        animator.CompleteAllImmediately();
        animator.CompleteAllImmediately();
        Assert.That(arrivals, Is.EqualTo(4), "Win/lose debe completar cada vuelo exactamente una vez.");
        Assert.That(animator.ActiveFlightCount, Is.Zero);
        yield return null;
        Assert.That(canvasRoot.transform.childCount, Is.Zero);
    }

    private GameObject Root(string name, params Type[] components)
    {
        var root = new GameObject(name, components);
        root.SetActive(false);
        _roots.Add(root);
        return root;
    }

    private static void Set(object instance, string field, object value) =>
        instance.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(instance, value);

    private static IEnumerator Until(Func<bool> condition)
    {
        double deadline = EditorApplication.timeSinceStartup + 5d;
        while (!condition())
        {
            Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline),
                "El feedback no alcanzó el estado esperado.");
            yield return null;
        }
    }

    private static void AssertPose(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Assert.That(Vector3.Distance(target.localPosition, position), Is.LessThan(0.00001f));
        Assert.That(Quaternion.Angle(target.localRotation, rotation), Is.LessThan(0.001f));
        Assert.That(Vector3.Distance(target.localScale, scale), Is.LessThan(0.00001f));
    }
}

internal static class GemArrivalTestScenes
{
    private const string Key = "GemArrivalFeedback.SavedSceneSetup";

    [Serializable]
    private sealed class SavedSetup
    {
        public SavedScene[] scenes;
    }

    [Serializable]
    private sealed class SavedScene
    {
        public string path;
        public bool isLoaded;
        public bool isActive;
    }

    public static void EnsureSafeToRun()
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False,
            "Las pruebas se inician fuera de Play Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Assert.That(scene.isDirty, Is.False,
                $"La escena {scene.name} tiene cambios sin guardar; las pruebas no los guardan ni descartan.");
            Assert.That(!string.IsNullOrEmpty(scene.path) || scene.rootCount == 0, Is.True,
                "Una escena sin archivo debe estar vacía para poder restaurarla sin pérdida.");
        }
    }

    public static void OpenEmpty()
    {
        EnsureSafeToRun();
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var saved = new SavedSetup { scenes = new SavedScene[setup.Length] };
        for (int i = 0; i < setup.Length; i++)
        {
            saved.scenes[i] = new SavedScene
            {
                path = setup[i].path,
                isLoaded = setup[i].isLoaded,
                isActive = setup[i].isActive
            };
        }
        SessionState.SetString(Key, JsonUtility.ToJson(saved));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    public static void Restore()
    {
        string json = SessionState.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        var saved = JsonUtility.FromJson<SavedSetup>(json);
        if (saved.scenes.Length == 1 && string.IsNullOrEmpty(saved.scenes[0].path))
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        else
        {
            var setup = new SceneSetup[saved.scenes.Length];
            for (int i = 0; i < setup.Length; i++)
                setup[i] = new SceneSetup
                {
                    path = saved.scenes[i].path,
                    isLoaded = saved.scenes[i].isLoaded,
                    isActive = saved.scenes[i].isActive
                };
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        SessionState.EraseString(Key);
    }
}

[InitializeOnLoad]
internal static class GemArrivalFeedbackTestMenu
{
    private const string RunningKey = "GemArrivalFeedback.MenuRun";
    private static readonly TestRunnerApi Api;

    static GemArrivalFeedbackTestMenu()
    {
        Api = ScriptableObject.CreateInstance<TestRunnerApi>();
        Api.hideFlags = HideFlags.HideAndDontSave;
        Api.RegisterCallbacks(new Callbacks());
    }

    [MenuItem("Tools/Gems/Run Arrival Checks")]
    public static void Run()
    {
        GemArrivalTestScenes.EnsureSafeToRun();
        SessionState.SetBool(RunningKey, true);
        Api.Execute(new ExecutionSettings(new Filter
        {
            testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
            categoryNames = new[] { "GemArrivalFeedback" }
        }));
    }

    private sealed class Callbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            SessionState.SetBool(RunningKey, false);
            TestRunnerApi.SaveResultToFile(result, "Temp/GemArrivalTests.xml");
            GemArrivalTestScenes.Restore();
            Debug.Log($"[GemArrivalChecks] {result.PassCount} passed, {result.FailCount} failed. " +
                "Results: Temp/GemArrivalTests.xml");
        }
    }
}

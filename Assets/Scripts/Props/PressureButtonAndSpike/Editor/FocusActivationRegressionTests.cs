using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Pruebas con Brain, transiciones, eventos, física y controladores reales.</summary>
public sealed class FocusActivationRegressionTests
{
    private readonly List<GameObject> _roots = new List<GameObject>();
    private readonly List<GameEvent> _channels = new List<GameEvent>();
    private GameEventManager _events;
    private FocusManager _manager;
    private CinemachineBrain _brain;
    private CinemachineVirtualCamera _focusCamera;

    [UnitySetUp]
    public IEnumerator EnterPlay()
    {
        yield return new EnterPlayMode();
        Time.timeScale = 1f;
        var eventRoot = Root("Events");
        _events = eventRoot.AddComponent<GameEventManager>();
        _events.playerEvents = CreateChannels<GameEventManager.PlayerEvents>();
        _events.levelEvents = CreateChannels<GameEventManager.LevelEvents>();
        eventRoot.SetActive(true);
        var lockRoot = Root("Locks");
        lockRoot.AddComponent<PlayerLock>();
        lockRoot.SetActive(true);

        var output = Root("Output camera");
        output.AddComponent<Camera>();
        _brain = output.AddComponent<CinemachineBrain>();
        _brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.15f);
        output.SetActive(true);
        var gameplay = Root("Gameplay camera");
        gameplay.transform.position = Vector3.back * 10;
        gameplay.AddComponent<CinemachineVirtualCamera>().Priority = 10;
        gameplay.SetActive(true);
        var focus = Root("Focus camera");
        _focusCamera = focus.AddComponent<CinemachineVirtualCamera>();
        _focusCamera.Priority = 0;
        focus.SetActive(true);
        var managerRoot = Root("Focus manager");
        _manager = managerRoot.AddComponent<FocusManager>();
        Set(_manager, "focusCam", _focusCamera);
        Set(_manager, "bufferBetweenFocus", 0f);
        managerRoot.SetActive(true);
        yield return Delay(0.05f);
    }

    [UnityTearDown]
    public IEnumerator LeavePlay()
    {
        for (int i = _roots.Count - 1; i >= 0; i--)
            if (_roots[i] != null) { _roots[i].SetActive(false); Object.Destroy(_roots[i]); }
        yield return null;
        foreach (var channel in _channels) if (channel != null) Object.Destroy(channel);
        _roots.Clear();
        _channels.Clear();
        CinemachineCore.UniformDeltaTimeOverride = -1f;
        Time.timeScale = 1f;
        yield return new ExitPlayMode();
    }

    private T CreateChannels<T>() where T : struct
    {
        object result = new T();
        foreach (var field in typeof(T).GetFields())
        {
            var channel = ScriptableObject.CreateInstance<GameEvent>();
            _channels.Add(channel);
            field.SetValue(result, channel);
        }
        return (T)result;
    }

    private GameObject Root(string name)
    {
        var root = new GameObject(name);
        root.SetActive(false);
        _roots.Add(root);
        return root;
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void Invoke(object target, string method, params object[] arguments) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);

    private static IEnumerator Until(Func<bool> predicate)
    {
        double deadline = EditorApplication.timeSinceStartup + 15;
        while (!predicate())
        {
            Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline), "La secuencia no terminó.");
            yield return null;
        }
    }

    private static IEnumerator Delay(float seconds)
    {
        float until = Time.time + seconds;
        yield return Until(() => Time.time >= until);
    }

    private FocusOnActivation AddFocus(GameObject root, float blend = 0.3f, float duration = 0.3f)
    {
        var marker = Root("Focus position");
        marker.transform.position = new Vector3(10, 0, -10);
        var source = root.AddComponent<FocusOnActivation>();
        Set(source, "cameraFocusPos", marker.transform);
        Set(source, "blendInDuration", blend);
        Set(source, "focusDuration", duration);
        return source;
    }

    private MonoBehaviour Platform(bool vertical, out FocusOnActivation source, bool addFocus = true)
    {
        var root = Root(vertical ? "Vertical" : "Horizontal");
        var a = Root("Waypoint A");
        var b = Root("Waypoint B");
        b.transform.position = vertical ? Vector3.up * 5 : Vector3.right * 5;
        source = addFocus ? AddFocus(root) : null;
        MonoBehaviour platform = vertical ? root.AddComponent<MoveVerticalPlatform>() : root.AddComponent<MoveHorizontalPlatform>();
        Set(platform, "waypoints", new[] { a.transform, b.transform });
        Set(platform, vertical ? "isMovingOnStart" : "isMoving", false);
        Set(platform, "glowDuration", 0.04f);
        root.SetActive(true);
        return platform;
    }

    private PressureButtonStateResolver Button(out FocusOnActivation source)
    {
        var root = Root("Pressure button");
        root.AddComponent<Rigidbody>().isKinematic = true;
        root.AddComponent<BoxCollider>().isTrigger = true;
        var sensor = root.AddComponent<WeightSensor>();
        var resolver = root.AddComponent<PressureButtonStateResolver>();
        Set(resolver, "_weightSensor", sensor);
        source = AddFocus(root);
        Set(source, "onlyOnce", false); // El prefab conserva este valor; el botón controla la unicidad.
        var trigger = root.AddComponent<PressureButtonOneShotFocusTrigger>();
        Set(trigger, "_stateResolver", resolver);
        Set(trigger, "_focusOnActivation", source);
        root.SetActive(true);
        return resolver;
    }

    private SpikeTrapController Trap(PressureButtonStateResolver button)
    {
        var root = Root("Spears");
        var motion = new GameObject("Motion");
        motion.transform.SetParent(root.transform);
        var body = motion.AddComponent<Rigidbody>();
        var visual = new GameObject("Visual");
        visual.transform.SetParent(motion.transform);
        var trap = root.AddComponent<SpikeTrapController>();
        Set(trap, "_motionRoot", motion.transform);
        Set(trap, "_motionRigidbody", body);
        Set(trap, "_visualShakeRoot", visual.transform);
        Set(trap, "_halfRaisedLocalPosition", Vector3.down);
        Set(trap, "_loweredLocalPosition", Vector3.down * 2);
        Set(trap, "_shakeDuration", 0f);
        Set(trap, "_moveDuration", 0.1f);
        root.SetActive(true);
        trap.RegisterButton(button, button);
        return trap;
    }

    [UnityTest]
    public IEnumerator SlowBlendActivatesAfterCameraArrivalAndKeepsFullDuration()
    {
        var root = Root("Source");
        var source = AddFocus(root, 0.35f, 0.25f);
        root.SetActive(true);
        int readyCount = 0, finishedCount = 0;
        float arrived = 0;
        var handle = source.ActivateWhenFocused(source, focused =>
        {
            Assert.That(focused, Is.True);
            Assert.That(_brain.ActiveVirtualCamera, Is.SameAs(_focusCamera));
            Assert.That(_brain.IsBlending, Is.False);
            arrived = Time.time;
            readyCount++;
        }, () => finishedCount++);
        source.ActivateWhenFocused(source, _ => Assert.Fail("Duplicó la solicitud pendiente."));
        yield return Delay(0.12f);
        Assert.That(readyCount, Is.Zero);
        yield return Until(() => handle.IsFinished);
        Assert.That(readyCount, Is.EqualTo(1));
        Assert.That(finishedCount, Is.EqualTo(1));
        Assert.That(Time.time - arrived, Is.GreaterThanOrEqualTo(0.23f));
        source.ActivateWhenFocused(source, focused => { Assert.That(focused, Is.False); readyCount++; });
        Assert.That(readyCount, Is.EqualTo(2), "onlyOnce omite el foco, no la acción.");
    }

    [UnityTest]
    public IEnumerator CutsAndQueuedRequestsEachWaitForTheirOwnCameraUpdate()
    {
        var aRoot = Root("A");
        var a = AddFocus(aRoot, 0f, 0.08f);
        aRoot.SetActive(true);
        var bRoot = Root("B");
        var b = AddFocus(bRoot, 0.2f, 0.08f);
        bRoot.SetActive(true);
        var order = new List<string>();
        a.ActivateWhenFocused(a, _ => order.Add("A"), () => order.Add("end A"));
        var second = b.ActivateWhenFocused(b, _ =>
        {
            Assert.That(_brain.IsBlending, Is.False);
            order.Add("B");
        }, () => order.Add("end B"));
        Assert.That(order, Is.Empty);
        yield return Until(() => second.IsFinished);
        CollectionAssert.AreEqual(new[] { "A", "end A", "B", "end B" }, order);
        yield return Until(() => !_manager.IsBusy);
        Assert.That(PlayerLock.Instance.IsLocked, Is.False);
    }

    [UnityTest]
    public IEnumerator PauseDuringBlendDefersActivationAndDisabledOwnerCancels()
    {
        var root = Root("Source");
        var source = AddFocus(root);
        root.SetActive(true);
        bool activated = false;
        int finished = 0;
        var handle = source.ActivateWhenFocused(source, _ => activated = true, () => finished++);
        yield return Until(() => _brain.IsBlending);
        _events.levelEvents.OnPauseChanged.Raise(true);
        yield return Delay(0.4f);
        Assert.That(activated, Is.False);
        source.enabled = false;
        _events.levelEvents.OnPauseChanged.Raise(false);
        yield return Until(() => !_manager.IsBusy);
        Assert.That(handle.IsFinished, Is.True);
        Assert.That(finished, Is.EqualTo(1));
        Assert.That(activated, Is.False);
        Assert.That(PlayerLock.Instance.IsLocked, Is.False);
    }

    [UnityTest]
    public IEnumerator MissingFocusFallsBackAndDestroyingQueuedOwnerDoesNotActivate()
    {
        var invalidRoot = Root("No position");
        var invalid = invalidRoot.AddComponent<FocusOnActivation>();
        invalidRoot.SetActive(true);
        int count = 0;
        LogAssert.Expect(LogType.Warning, "[FocusOnActivation] Foco no disponible; activando directamente.");
        invalid.ActivateWhenFocused(invalid, focused => { Assert.That(focused, Is.False); count++; });
        Assert.That(count, Is.EqualTo(1));
        var aRoot = Root("First");
        var a = AddFocus(aRoot);
        aRoot.SetActive(true);
        a.ActivateWhenFocused(a, _ => { });
        var bRoot = Root("Queued");
        var b = AddFocus(bRoot);
        bRoot.SetActive(true);
        var handle = b.ActivateWhenFocused(b, _ => Assert.Fail("Se activó un objeto destruido."));
        Object.Destroy(bRoot);
        yield return Until(() => !_manager.IsBusy);
        Assert.That(handle.IsFinished, Is.True);
    }

    [UnityTest]
    public IEnumerator SpearsWaitForFocusThenUseLatestWeightAndNeverRefocus()
    {
        var button = Button(out var source);
        var trap = Trap(button);
        yield return null;
        Invoke(button, "PublishWeight", 1);
        Assert.That(button.EffectiveState, Is.EqualTo(PressureButtonState.HalfPressed));
        Assert.That(button.TrapWeight, Is.Zero);
        yield return Delay(0.1f);
        Assert.That(trap.CurrentLocalPosition, Is.EqualTo(Vector3.zero));
        Invoke(button, "PublishWeight", 2);
        Assert.That(button.TrapWeight, Is.Zero);
        yield return Until(() => button.TrapWeight == 2);
        Assert.That(_brain.ActiveVirtualCamera, Is.SameAs(_focusCamera));
        Assert.That(_brain.IsBlending, Is.False);
        yield return Delay(0.15f);
        Assert.That(trap.CurrentState, Is.EqualTo(SpikeTrapState.Lowered));
        Assert.That(_manager.IsBusy, Is.True);
        yield return Until(() => !_manager.IsBusy);
        Invoke(button, "PublishWeight", 0);
        Invoke(button, "PublishWeight", 1);
        Assert.That(button.TrapWeight, Is.EqualTo(1));
        Assert.That(_manager.IsBusy, Is.False);
        Invoke(button, "PublishWeight", 2);
        Assert.That(button.TrapWeight, Is.EqualTo(2));
        Assert.That(source.IsPending, Is.False);
    }

    [UnityTest]
    public IEnumerator ReleasedWeightDuringBlendIsNotReplayedAndOtherButtonsStillSum()
    {
        var a = Button(out _);
        var b = Button(out _);
        b.GetComponent<PressureButtonOneShotFocusTrigger>().enabled = false;
        var trap = Trap(a);
        trap.RegisterButton(b, b);
        yield return null;
        Invoke(a, "PublishWeight", 1);
        Invoke(b, "PublishWeight", 1);
        yield return Delay(0.1f);
        Assert.That(trap.TotalEffectiveWeight, Is.EqualTo(1));
        Invoke(a, "PublishWeight", 0);
        yield return Until(() => !_manager.IsBusy);
        Assert.That(a.TrapWeight, Is.Zero);
        Assert.That(trap.TotalEffectiveWeight, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator BothPlatformTypesMoveAfterEffectsAndRespectOtherLocksAndPause()
    {
        var horizontal = Platform(false, out var source);
        var vertical = Platform(true, out _ , false);
        yield return null;
        var handle = source.ActivateWhenFocused(source, focused =>
        {
            ((IFocusActivatablePlatform)horizontal).StartActionWithoutFocus(focused);
            ((IFocusActivatablePlatform)vertical).StartActionWithoutFocus(focused);
        }, () =>
        {
            ((IFocusActivatablePlatform)horizontal).EndActivationFocus();
            ((IFocusActivatablePlatform)vertical).EndActivationFocus();
        });
        yield return Delay(0.1f);
        Assert.That(horizontal.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(vertical.transform.position, Is.EqualTo(Vector3.zero));
        yield return Until(() => horizontal.transform.position.x > 0 && vertical.transform.position.y > 0);
        Assert.That(horizontal.transform.position.x, Is.GreaterThan(0));
        Assert.That(vertical.transform.position.y, Is.GreaterThan(0));
        _events.playerEvents.OnLockRequested.Raise("Death", true);
        Vector3 h = horizontal.transform.position, v = vertical.transform.position;
        yield return Delay(0.05f);
        Assert.That(horizontal.transform.position, Is.EqualTo(h));
        Assert.That(vertical.transform.position, Is.EqualTo(v));
        _events.levelEvents.OnPauseChanged.Raise(true);
        _events.playerEvents.OnLockRequested.Raise("Death", false);
        yield return Delay(0.05f);
        Assert.That(horizontal.transform.position, Is.EqualTo(h));
        _events.levelEvents.OnPauseChanged.Raise(false);
        yield return Delay(0.05f);
        Assert.That(horizontal.transform.position.x, Is.GreaterThan(h.x));
        Assert.That(vertical.transform.position.y, Is.GreaterThan(v.y));
    }

    [UnityTest]
    public IEnumerator EagleUsesOneFocusForDeduplicatedGroupAndFreezesOutsiders()
    {
        var horizontal = Platform(false, out var firstFocus);
        var vertical = Platform(true, out var otherFocus);
        var outsider = Platform(false, out _, false);
        var eagleRoot = Root("Eagle");
        var collider = eagleRoot.AddComponent<BoxCollider>();
        var eagle = eagleRoot.AddComponent<ActivateObjectsBullet>();
        Set(eagle, "_platformsAll", new List<GameObject>
            { null, horizontal.gameObject, horizontal.gameObject, vertical.gameObject });
        eagleRoot.SetActive(true);
        var bulletRoot = Root("Projectile");
        bulletRoot.tag = "Bullet";
        var bullet = bulletRoot.AddComponent<BoxCollider>();
        yield return null;
        ((IFocusActivatablePlatform)outsider).StartActionWithoutFocus(false);
        Invoke(eagle, "OnTriggerEnter", bullet);
        Invoke(eagle, "OnTriggerEnter", bullet);
        Assert.That(collider.enabled, Is.False);
        Assert.That(firstFocus.IsPending, Is.True);
        Assert.That(otherFocus.IsPending, Is.False);
        yield return Until(() => PlayerLock.Instance.IsLocked);
        var outsiderPosition = outsider.transform.position;
        yield return Until(() => vertical.transform.position.y > 0);
        Assert.That(horizontal.transform.position.x, Is.GreaterThan(0));
        Assert.That(outsider.transform.position, Is.EqualTo(outsiderPosition));
        _manager.enabled = false;
        Assert.That(PlayerLock.Instance.IsLocked, Is.False);
        _events.playerEvents.OnLockRequested.Raise(FocusManager.LockId, true);
        Vector3 h = horizontal.transform.position, v = vertical.transform.position;
        yield return Delay(0.05f);
        Assert.That(horizontal.transform.position, Is.EqualTo(h), "Se filtró el permiso del grupo al interrumpir.");
        Assert.That(vertical.transform.position, Is.EqualTo(v));
        _events.playerEvents.OnLockRequested.Raise(FocusManager.LockId, false);
    }

    [UnityTest]
    public IEnumerator StartActionStopsAndCancelsPendingPlatformActivation()
    {
        var platform = (MoveHorizontalPlatform)Platform(false, out _);
        yield return null;
        platform.StartAction();
        platform.StartAction();
        yield return Until(() => !_manager.IsBusy);
        Assert.That(platform.transform.position, Is.EqualTo(Vector3.zero));
        platform.StartAction(); // onlyOnce ya fue consumido: no repite cámara.
        yield return Until(() => platform.transform.position.x > 0);
        Assert.That(platform.transform.position.x, Is.GreaterThan(0));
        platform.StartAction();
        var stopped = platform.transform.position;
        yield return Delay(0.05f);
        Assert.That(platform.transform.position, Is.EqualTo(stopped));
    }

    [UnityTest]
    public IEnumerator LosingCameraBeforeArrivalFallsBackExactlyOnce()
    {
        var root = Root("Source");
        var source = AddFocus(root);
        root.SetActive(true);
        int count = 0;
        var handle = source.ActivateWhenFocused(source, focused =>
        {
            Assert.That(focused, Is.False);
            count++;
        });
        yield return Until(() => _brain.IsBlending);
        LogAssert.Expect(LogType.Warning, "[FocusManager] Se perdió la cámara; activando sin foco.");
        Object.Destroy(_focusCamera.gameObject);
        yield return Until(() => !_manager.IsBusy);
        Assert.That(handle.IsFinished, Is.True);
        Assert.That(count, Is.EqualTo(1));
        Assert.That(PlayerLock.Instance.IsLocked, Is.False);
    }

    [UnityTest]
    public IEnumerator DisablingEagleWhileQueuedCancelsWholeGroup()
    {
        var horizontal = Platform(false, out _);
        var vertical = Platform(true, out _);
        var eagleRoot = Root("Eagle");
        var eagle = eagleRoot.AddComponent<ActivateObjectsBullet>();
        Set(eagle, "_platformsAll", new List<GameObject> { horizontal.gameObject, vertical.gameObject });
        eagleRoot.SetActive(true);
        yield return null;
        Invoke(eagle, "ActivatePlatforms");
        eagle.enabled = false;
        yield return Until(() => !_manager.IsBusy);
        Assert.That(horizontal.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(vertical.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(PlayerLock.Instance.IsLocked, Is.False);
    }

    [UnityTest]
    public IEnumerator LegacyFocusAndRevealKeepTheirOriginalTiming()
    {
        var marker = Root("Legacy position");
        marker.transform.position = Vector3.right * 10;
        bool completed = false;
        _brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 1f);
        float start = Time.time;
        _manager.RequestRevealFocus(1, marker.transform, null, 0.05f, 0f,
            AnimationCurve.Linear(0, 0, 1, 1), () => completed = true);
        _manager.RequestObjectFocus(marker.transform, null, 0.05f, 0f,
            AnimationCurve.Linear(0, 0, 1, 1));
        yield return Until(() => !_manager.IsBusy);
        Assert.That(completed, Is.True);
        Assert.That(Time.time - start, Is.LessThan(0.8f), "El contrato anterior no espera el blend de un segundo.");
    }

    private ParticleSystem ActivationParticles(float lifetime)
    {
        var root = Root("Activation particles");
        var particles = root.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
        root.SetActive(true);
        return particles;
    }

    private IEnumerator VerifyEffectsThenMovement(bool vertical)
    {
        var platform = Platform(vertical, out var source);
        var particles = ActivationParticles(0.5f);
        Set(platform, "activationParticles", particles);
        Set(platform, "glowDuration", 0.2f);
        Set(source, "blendInDuration", 0f);
        Set(source, "focusDuration", 0.12f);
        int high = 0, low = 0, rumbleFrame = -1;
        _events.levelEvents.OnRumbleHigh.Register<float, float>((_, __) =>
        {
            Assert.That(platform.transform.position.sqrMagnitude, Is.GreaterThan(0));
            Assert.That(particles.IsAlive(true), Is.False, "El movimiento debe esperar a las partículas.");
            high++;
            rumbleFrame = Time.frameCount;
        });
        _events.levelEvents.OnRumbleLow.Register<float, float>((_, __) => low++);
        yield return null;
        if (vertical) ((MoveVerticalPlatform)platform).StartAction();
        else ((MoveHorizontalPlatform)platform).StartAction();
        yield return Until(() => particles.particleCount > 0);
        yield return Delay(0.25f);
        Assert.That(platform.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(high + low, Is.Zero);
        Assert.That(_manager.IsBusy, Is.True, "El foco debe cubrir toda la preparación.");
        yield return Until(() => high > 0);
        Assert.That(Time.frameCount, Is.EqualTo(rumbleFrame));
        Assert.That(high, Is.EqualTo(1));
        Assert.That(low, Is.EqualTo(1));
        Assert.That(_manager.IsBusy, Is.True);
        yield return Delay(0.15f);
        Assert.That(high, Is.EqualTo(1));
        Assert.That(low, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator HorizontalEffectsFinishBeforeMovementAndRumble() => VerifyEffectsThenMovement(false);

    [UnityTest]
    public IEnumerator VerticalEffectsFinishBeforeMovementAndRumble() => VerifyEffectsThenMovement(true);

    [UnityTest]
    public IEnumerator SpearsGlowAndParticlesDelayMotionAndExtendFocus()
    {
        var button = Button(out var source);
        var trap = Trap(button);
        var particles = ActivationParticles(0.4f);
        var visual = Root("Glow mesh").AddComponent<MeshRenderer>();
        visual.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/Modules/Materials/Zone 1 -Pyramids/Spears/Mat_Spears.mat");
        trap.GetComponent<ActivationGlow>().Configure(new Renderer[] { visual });
        Set(trap, "_activationParticles", particles);
        Set(trap, "_glowDuration", 0.25f);
        Set(source, "blendInDuration", 0f);
        Set(source, "focusDuration", 0.15f);
        yield return null;
        Invoke(button, "PublishWeight", 2);
        yield return Until(() => particles.particleCount > 0);
        yield return Delay(0.1f);
        var properties = new MaterialPropertyBlock();
        visual.GetPropertyBlock(properties, 0);
        Assert.That(properties.GetFloat("_ActivationGlowIntensity"), Is.GreaterThan(0));
        Assert.That(trap.CurrentLocalPosition, Is.EqualTo(Vector3.zero));
        _events.levelEvents.OnPauseChanged.Raise(true);
        yield return Delay(0.15f);
        Assert.That(trap.CurrentLocalPosition, Is.EqualTo(Vector3.zero));
        Assert.That(particles.isPaused, Is.True);
        _events.levelEvents.OnPauseChanged.Raise(false);
        yield return Until(() => trap.IsMoving);
        Assert.That(particles.IsAlive(true), Is.False);
        Assert.That(_manager.IsBusy, Is.True);
        visual.GetPropertyBlock(properties, 0);
        Assert.That(properties.GetFloat("_ActivationGlowIntensity"), Is.Zero);
        yield return Until(() => trap.CurrentState == SpikeTrapState.Lowered);
    }

    [UnityTest]
    public IEnumerator CancellingEffectsNeverMovesOrRumbles()
    {
        var platform = (MoveHorizontalPlatform)Platform(false, out _, false);
        Set(platform, "glowDuration", 0.3f);
        var particles = ActivationParticles(0.4f);
        Set(platform, "activationParticles", particles);
        int rumble = 0;
        _events.levelEvents.OnRumbleHigh.Register<float, float>((_, __) => rumble++);
        var button = Button(out _);
        button.GetComponent<PressureButtonOneShotFocusTrigger>().enabled = false;
        var trap = Trap(button);
        Set(trap, "_glowDuration", 0.3f);
        yield return null;
        platform.StartAction();
        Invoke(button, "PublishWeight", 1);
        yield return Delay(0.1f);
        platform.StartAction();
        Invoke(button, "PublishWeight", 0);
        yield return Delay(0.4f);
        Assert.That(platform.transform.position, Is.EqualTo(Vector3.zero));
        Assert.That(trap.CurrentLocalPosition, Is.EqualTo(Vector3.zero));
        Assert.That(trap.IsTransitioning, Is.False);
        Assert.That(particles.IsAlive(true), Is.False);
        Assert.That(rumble, Is.Zero);
    }
    [UnityTest]
    public IEnumerator LinkedSpearsShareFirstAndLastPhysicsStepDespiteDifferentEffects()
    {
        var button = Button(out var focus);
        Set(focus, "blendInDuration", 0f);
        var a = Trap(button);
        var b = Trap(button);
        Set(a, "_activationParticles", ActivationParticles(0.18f));
        var randomParticles = ActivationParticles(0.5f);
        var main = randomParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        Set(b, "_activationParticles", randomParticles);
        Set(a, "_glowDuration", 0.08f);
        Set(b, "_glowDuration", 0.2f);
        Set(a, "_shakeDuration", 0.04f);
        Set(b, "_shakeDuration", 0.12f);
        Set(a, "_moveDuration", 0.12f);
        Set(b, "_moveDuration", 0.4f);
        yield return null;
        Invoke(button, "PublishWeight", 2);
        yield return Until(() => a.IsPreparingActivation);
        yield return Delay(0.1f);
        _events.levelEvents.OnPauseChanged.Raise(true);
        yield return Delay(0.1f);
        Assert.That(a.CurrentLocalPosition, Is.EqualTo(Vector3.zero));
        Assert.That(b.CurrentLocalPosition, Is.EqualTo(Vector3.zero));
        _events.levelEvents.OnPauseChanged.Raise(false);
        float started = -1;
        while (a.CurrentState != SpikeTrapState.Lowered || b.CurrentState != SpikeTrapState.Lowered)
        {
            yield return new WaitForFixedUpdate();
            Assert.That(a.CurrentLocalPosition.y, Is.EqualTo(b.CurrentLocalPosition.y).Within(0.0001f));
            Assert.That(a.CurrentState, Is.EqualTo(b.CurrentState), "Deben terminar en el mismo paso.");
            if (started < 0 && a.IsMoving) started = Time.fixedTime;
        }
        Assert.That(Time.fixedTime - started, Is.GreaterThanOrEqualTo(0.36f));
    }

    [UnityTest]
    public IEnumerator OverlappingButtonsMergeRedirectAndPauseTheirSharedSpears()
    {
        var first = Button(out _);
        var second = Button(out _);
        first.GetComponent<PressureButtonOneShotFocusTrigger>().enabled = false;
        second.GetComponent<PressureButtonOneShotFocusTrigger>().enabled = false;
        var a = Trap(first);
        var shared = Trap(first);
        shared.RegisterButton(second, second);
        var c = Trap(second);
        Set(a, "_moveDuration", 0.4f);
        Set(shared, "_moveDuration", 0.2f);
        Set(c, "_moveDuration", 0.1f);
        yield return null;
        Invoke(first, "PublishWeight", 1);
        Invoke(second, "PublishWeight", 1);
        yield return Until(() => a.CurrentLocalPosition.y < -0.1f);
        Assert.That(shared.TotalEffectiveWeight, Is.EqualTo(2));
        _events.levelEvents.OnPauseChanged.Raise(true);
        var position = a.CurrentLocalPosition;
        yield return Delay(0.1f);
        Assert.That(a.CurrentLocalPosition, Is.EqualTo(position));
        _events.levelEvents.OnPauseChanged.Raise(false);
        Invoke(first, "PublishWeight", 0);
        Invoke(second, "PublishWeight", 2);
        yield return null;
        yield return Until(() => !a.IsTransitioning && !shared.IsTransitioning && !c.IsTransitioning);
        Assert.That(a.CurrentState, Is.EqualTo(SpikeTrapState.Raised));
        Assert.That(shared.CurrentState, Is.EqualTo(SpikeTrapState.Lowered));
        Assert.That(c.CurrentState, Is.EqualTo(SpikeTrapState.Lowered));
        Assert.That(shared.TotalEffectiveWeight, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator PendingGroupUsesLatestWeightAndRemovesDisabledMember()
    {
        var button = Button(out _);
        button.GetComponent<PressureButtonOneShotFocusTrigger>().enabled = false;
        var a = Trap(button);
        var b = Trap(button);
        Set(a, "_glowDuration", 0.2f);
        Set(b, "_glowDuration", 0.5f);
        yield return null;
        Invoke(button, "PublishWeight", 1);
        yield return Delay(0.06f);
        Invoke(button, "PublishWeight", 2);
        b.enabled = false;
        yield return Until(() => a.CurrentState == SpikeTrapState.Lowered);
        Assert.That(a.CurrentLocalPosition.y, Is.EqualTo(-2).Within(0.001f));
        Assert.That(b.CurrentLocalPosition, Is.EqualTo(Vector3.zero));
        Invoke(button, "PublishWeight", 0);
        yield return Until(() => a.CurrentState == SpikeTrapState.Raised);
    }

    [UnityTest]
    public IEnumerator LegacyAndDirectConnectionsShareOneGroupWithoutDuplicateWeight()
    {
        var button = Button(out _);
        button.GetComponent<PressureButtonOneShotFocusTrigger>().enabled = false;
        var a = Trap(button);
        var b = Trap(button);
        b.UnregisterButtons(button);
        var root = Root("Legacy orchestrator");
        var legacy = root.AddComponent<MultiButtonTrapOrchestrator>();
        Set(legacy, "_buttonResolvers", new[] { button, button });
        Set(legacy, "_spikeTrapTarget", b);
        root.SetActive(true);
        Set(a, "_glowDuration", 0.05f);
        Set(b, "_glowDuration", 0.25f);
        yield return null;
        Invoke(button, "PublishWeight", 1);
        yield return null;
        while (a.CurrentState != SpikeTrapState.HalfRaised)
        {
            yield return new WaitForFixedUpdate();
            Assert.That(a.CurrentLocalPosition.y, Is.EqualTo(b.CurrentLocalPosition.y).Within(0.0001f));
        }
        Assert.That(a.TotalEffectiveWeight, Is.EqualTo(1));
        Assert.That(b.TotalEffectiveWeight, Is.EqualTo(1));
    }

}


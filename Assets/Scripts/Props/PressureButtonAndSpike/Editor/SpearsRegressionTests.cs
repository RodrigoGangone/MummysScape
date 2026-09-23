using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Pruebas EditMode que entran en Play para verificar eventos y física reales.</summary>
public sealed class SpearsRegressionTests
{
    private readonly List<GameObject> _roots = new List<GameObject>();

    [UnitySetUp]
    public IEnumerator EnterPlay()
    {
        yield return new EnterPlayMode();
        Time.timeScale = 1f;
    }

    [UnityTearDown]
    public IEnumerator LeavePlay()
    {
        foreach (GameObject root in _roots) if (root != null) Object.Destroy(root);
        _roots.Clear();
        yield return null;
        yield return new ExitPlayMode();
    }

    private GameObject Root(string name, Vector3 position = default)
    {
        var root = new GameObject(name);
        root.SetActive(false);
        root.transform.position = position;
        _roots.Add(root);
        return root;
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }

    private SpikeTrapController Trap(int half = 1, int full = 2, float duration = 0f)
    {
        GameObject root = Root("Lanza");
        var motion = new GameObject("Motion");
        motion.transform.SetParent(root.transform, false);
        Rigidbody body = motion.AddComponent<Rigidbody>();
        var visual = new GameObject("Visual");
        visual.transform.SetParent(motion.transform, false);
        var trap = root.AddComponent<SpikeTrapController>();
        Set(trap, "_motionRoot", motion.transform);
        Set(trap, "_motionRigidbody", body);
        Set(trap, "_visualShakeRoot", visual.transform);
        Set(trap, "_halfRaisedLocalPosition", Vector3.down * 0.75f);
        Set(trap, "_loweredLocalPosition", Vector3.down * 2f);
        Set(trap, "_halfRaisedWeight", half);
        Set(trap, "_loweredWeight", full);
        Set(trap, "_moveDuration", duration);
        Set(trap, "_shakeDuration", 0f);
        root.SetActive(true);
        return trap;
    }

    private PressureButtonStateResolver Button(Vector3 position, float hold = 0.2f, params SpikeTrapController[] traps)
    {
        GameObject root = Root("Botón", position);
        root.AddComponent<Rigidbody>();
        var box = root.AddComponent<BoxCollider>();
        box.size = Vector3.one * 2f;
        var sensor = root.AddComponent<WeightSensor>();
        root.AddComponent<TimerService>();
        var timer = root.AddComponent<PressureButtonHoldTimer>();
        Set(timer, "_duration", hold);
        var resolver = root.AddComponent<PressureButtonStateResolver>();
        Set(resolver, "_weightSensor", sensor);
        Set(resolver, "_holdTimer", timer);
        var coordinator = root.AddComponent<PressureButtonCoordinator>();
        var plate = new GameObject("Placa");
        plate.transform.SetParent(root.transform, false);
        var mover = root.AddComponent<PressureButtonPlateMover>();
        Set(mover, "_plate", plate.transform);
        Set(mover, "_halfPressedLocalPosition", Vector3.down * 0.1f);
        Set(mover, "_fullyPressedLocalPosition", Vector3.down * 0.2f);
        Set(mover, "_duration", 0f);
        Set(coordinator, "_plateMover", mover);
        Set(coordinator, "_stateResolver", resolver);
        Set(coordinator, "_spikeTrapTargets", Array.ConvertAll(traps, trap => (MonoBehaviour)trap));
        root.SetActive(true);
        return resolver;
    }

    private ConstantWeightProvider Load(PressureButtonStateResolver button, int weight, bool multipleColliders = false)
    {
        GameObject root = Root("Carga", button.transform.position);
        var provider = root.AddComponent<ConstantWeightProvider>();
        provider.SetWeight(weight);
        var body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;
        root.AddComponent<BoxCollider>().size = Vector3.one * 0.25f;
        if (multipleColliders)
        {
            var child = new GameObject("Segundo collider");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<SphereCollider>().radius = 0.2f;
        }
        root.SetActive(true);
        return provider;
    }

    private static IEnumerator WaitSeconds(float seconds)
    {
        // El runner EditMode no procesa WaitForSeconds aunque el test haya entrado en Play.
        float targetTime = Time.time + seconds;
        double timeout = UnityEditor.EditorApplication.timeSinceStartup + 10d;
        while (Time.time < targetTime)
        {
            Assert.That(UnityEditor.EditorApplication.timeSinceStartup, Is.LessThan(timeout), "Play no avanzó el tiempo.");
            yield return null;
        }
    }

    private static IEnumerator Settle()
    {
        Physics.SyncTransforms();
        yield return WaitSeconds(0.03f);
        yield return WaitSeconds(0.03f);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator SharedConnectionsSumOnceAndRemainIndependent()
    {
        var shared = Trap();
        var firstOnly = Trap();
        var secondOnly = Trap();
        var a = Button(Vector3.left * 10, 0.2f, shared, shared, firstOnly);
        var b = Button(Vector3.right * 10, 0.2f, shared, secondOnly);
        var legacyRoot = Root("Conexión antigua");
        var legacy = legacyRoot.AddComponent<MultiButtonTrapOrchestrator>();
        Set(legacy, "_buttonResolvers", new[] { a, b, a, null });
        Set(legacy, "_spikeTrapTarget", shared);
        legacyRoot.SetActive(true);
        Load(a, 1);
        var loadB = Load(b, 1);
        yield return Settle();
        Assert.That(shared.TotalEffectiveWeight, Is.EqualTo(2));
        Assert.That(shared.CurrentState, Is.EqualTo(SpikeTrapState.Lowered));
        Assert.That(firstOnly.CurrentState, Is.EqualTo(SpikeTrapState.HalfRaised));
        Assert.That(secondOnly.CurrentState, Is.EqualTo(SpikeTrapState.HalfRaised));
        Assert.That(a.GetComponent<PressureButtonHoldTimer>().IsRunning, Is.False);
        Assert.That(b.GetComponent<PressureButtonHoldTimer>().IsRunning, Is.False);
        Assert.That(a.GetComponent<PressureButtonPlateMover>().CurrentState, Is.EqualTo(PressureButtonState.HalfPressed));
        legacy.enabled = false;
        yield return Settle();
        Assert.That(shared.TotalEffectiveWeight, Is.EqualTo(2), "Retirar una ruta no retira la conexión directa.");
        Object.Destroy(loadB.gameObject);
        yield return WaitSeconds(0.35f);
        Assert.That(shared.TotalEffectiveWeight, Is.EqualTo(1));
        Assert.That(secondOnly.CurrentState, Is.EqualTo(SpikeTrapState.Raised));
    }

    [UnityTest]
    public IEnumerator CustomThresholdsAndWeightChangesWithoutVisualStateChange()
    {
        var trap = Trap(2, 5);
        var button = Button(Vector3.left * 10, 0.5f, trap);
        Set(button, "_halfPressThreshold", 2);
        Set(button, "_fullPressThreshold", 3);
        Invoke(button, "OnValidate");
        var load = Load(button, 1);
        yield return Settle();
        Assert.That(trap.CurrentState, Is.EqualTo(SpikeTrapState.Raised));
        Assert.That(button.EffectiveState, Is.EqualTo(PressureButtonState.Released));
        load.SetWeight(3);
        yield return Settle();
        Assert.That(trap.CurrentState, Is.EqualTo(SpikeTrapState.HalfRaised));
        int changes = 0;
        button.EffectiveWeightChanged += _ => changes++;
        load.SetWeight(5);
        yield return Settle();
        Assert.That(changes, Is.EqualTo(1));
        Assert.That(button.EffectiveState, Is.EqualTo(PressureButtonState.FullyPressed));
        Assert.That(trap.CurrentState, Is.EqualTo(SpikeTrapState.Lowered));
        load.SetWeight(0);
        yield return Settle();
        Assert.That(button.EffectiveWeight, Is.EqualTo(3), "Retiene el umbral, no el máximo peso previo.");
        yield return WaitSeconds(0.6f);
        Assert.That(button.EffectiveWeight, Is.Zero);
        Assert.That(trap.CurrentState, Is.EqualTo(SpikeTrapState.Raised));
    }

    [UnityTest]
    public IEnumerator HoldsAreIndependentAndRearmingCancelsTheOldCountdown()
    {
        var trap = Trap(1, 3);
        var a = Button(Vector3.left * 10, 0.3f, trap);
        var b = Button(Vector3.right * 10, 0.8f, trap);
        var loadA = Load(a, 2);
        var loadB = Load(b, 2);
        yield return Settle();
        loadA.SetWeight(0);
        loadB.SetWeight(0);
        yield return Settle();
        Assert.That(trap.TotalEffectiveWeight, Is.EqualTo(4));
        loadA.SetWeight(2);
        yield return WaitSeconds(0.4f);
        Assert.That(a.GetComponent<PressureButtonHoldTimer>().IsRunning, Is.False);
        Assert.That(a.EffectiveWeight, Is.EqualTo(2));
        Assert.That(b.EffectiveWeight, Is.EqualTo(2));
        loadA.SetWeight(1);
        yield return WaitSeconds(0.35f);
        Assert.That(a.EffectiveWeight, Is.EqualTo(1));
        yield return WaitSeconds(0.2f);
        Assert.That(b.EffectiveWeight, Is.Zero);
        Assert.That(trap.TotalEffectiveWeight, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator DisabledButtonsTimersAndTrapsRecoverWithoutStaleWeight()
    {
        var trap = Trap();
        var button = Button(Vector3.left * 10, 1f, trap);
        var load = Load(button, 2);
        yield return Settle();
        load.SetWeight(0);
        yield return Settle();
        button.GetComponent<PressureButtonHoldTimer>().enabled = false;
        yield return Settle();
        Assert.That(button.EffectiveWeight, Is.Zero);
        load.SetWeight(1);
        button.gameObject.SetActive(false);
        yield return Settle();
        Assert.That(trap.TotalEffectiveWeight, Is.Zero);
        button.gameObject.SetActive(true);
        yield return Settle();
        Assert.That(trap.TotalEffectiveWeight, Is.EqualTo(1));
        trap.gameObject.SetActive(false);
        load.SetWeight(2);
        yield return Settle();
        trap.gameObject.SetActive(true);
        yield return Settle();
        Assert.That(trap.CurrentState, Is.EqualTo(SpikeTrapState.Lowered));
        button.enabled = false;
        yield return Settle();
        Assert.That(trap.TotalEffectiveWeight, Is.Zero);
        button.enabled = true;
        yield return Settle();
        Assert.That(trap.TotalEffectiveWeight, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator CompoundSleepingLoadSurvivesCleanupAndInspectorChanges()
    {
        var button = Button(Vector3.left * 10);
        var load = Load(button, 1, true);
        yield return Settle();
        var sensor = button.GetComponent<WeightSensor>();
        Assert.That(sensor.TotalWeight, Is.EqualTo(1));
        Assert.That(sensor.ProviderCount, Is.EqualTo(1));
        load.GetComponent<Rigidbody>().Sleep();
        yield return WaitSeconds(1.2f);
        Assert.That(sensor.TotalWeight, Is.EqualTo(1));
        var serialized = new SerializedObject(load);
        serialized.FindProperty("_weight").intValue = 4;
        serialized.ApplyModifiedProperties();
        yield return Settle();
        Assert.That(sensor.TotalWeight, Is.EqualTo(4));
        load.GetComponent<BoxCollider>().enabled = false;
        yield return WaitSeconds(0.35f);
        Assert.That(sensor.TotalWeight, Is.EqualTo(4), "El segundo collider todavía está en el sensor.");
        load.transform.position += Vector3.forward * 20;
        yield return Settle();
        yield return WaitSeconds(0.8f);
        Assert.That(sensor.TotalWeight, Is.Zero);
    }

    [UnityTest]
    public IEnumerator MissingExitRecoveryUsesOverlapAndSensorCanBeReenabled()
    {
        var button = Button(Vector3.left * 10);
        var load = Load(button, 1);
        yield return Settle();
        var sensor = button.GetComponent<WeightSensor>();
        sensor.enabled = false;
        yield return Settle();
        Assert.That(sensor.TotalWeight, Is.Zero, "Los eventos trigger no deben registrar peso con el componente deshabilitado.");
        load.GetComponent<Rigidbody>().Sleep();
        sensor.enabled = true;
        yield return Settle();
        Assert.That(sensor.TotalWeight, Is.EqualTo(1));
        Object.Destroy(load.gameObject);
        yield return WaitSeconds(0.4f);
        Assert.That(sensor.TotalWeight, Is.Zero);
        Assert.That(sensor.ProviderCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator RapidMotionChangesFinishAtLatestTarget()
    {
        var trap = Trap(1, 2, 0.15f);
        var a = Button(Vector3.left * 10, 0.1f, trap);
        var b = Button(Vector3.right * 10, 0.1f, trap);
        var loadA = Load(a, 1);
        var loadB = Load(b, 0);
        yield return Settle();
        loadB.SetWeight(1);
        yield return Settle();
        loadA.SetWeight(0);
        loadB.SetWeight(0);
        yield return WaitSeconds(0.35f);
        Assert.That(trap.CurrentState, Is.EqualTo(SpikeTrapState.Raised));
        Assert.That(trap.IsTransitioning, Is.False);
        Assert.That(trap.CurrentLocalPosition.sqrMagnitude, Is.LessThan(0.0001f));
    }

    [UnityTest]
    public IEnumerator WeightSensorStillExposesProviderDataForSeesaw()
    {
        var a = Button(Vector3.left * 10);
        var b = Button(Vector3.right * 10);
        var load = Load(a, 2, true);
        yield return Settle();
        var providers = new List<WeightSensor.ProviderData>();
        a.GetComponent<WeightSensor>().CopyProviderDataTo(providers);
        Assert.That(providers.Count, Is.EqualTo(1));
        Assert.That(providers[0].ColliderCount, Is.EqualTo(2));
        Assert.That(providers[0].Weight, Is.EqualTo(2));
        Assert.That(providers[0].Owner, Is.SameAs(load));
        var balance = Root("Resolución del balancín").AddComponent<SeesawStateResolver>();
        balance.Resolve(a.GetComponent<WeightSensor>().TotalWeight, b.GetComponent<WeightSensor>().TotalWeight);
        Assert.That(balance.CurrentState, Is.EqualTo(SeesawState.LeftHeavy));
        load.transform.position = b.transform.position;
        yield return Settle();
        a.GetComponent<WeightSensor>().CopyProviderDataTo(providers);
        Assert.That(providers, Is.Empty);
        b.GetComponent<WeightSensor>().CopyProviderDataTo(providers);
        Assert.That(providers.Count, Is.EqualTo(1));
        Assert.That(providers[0].Weight, Is.EqualTo(2));
        balance.Resolve(a.GetComponent<WeightSensor>().TotalWeight, b.GetComponent<WeightSensor>().TotalWeight);
        Assert.That(balance.CurrentState, Is.EqualTo(SeesawState.RightHeavy));
    }

    [UnityTest]
    public IEnumerator DisabledProviderOnSleepingBodyCanResumeItsContribution()
    {
        var button = Button(Vector3.left * 10);
        var load = Load(button, 1);
        yield return Settle();
        var sensor = button.GetComponent<WeightSensor>();
        load.GetComponent<Rigidbody>().Sleep();
        load.enabled = false;
        yield return WaitSeconds(0.7f);
        Assert.That(sensor.TotalWeight, Is.Zero);
        Assert.That(sensor.ProviderCount, Is.Zero);
        load.enabled = true;
        yield return Settle();
        Assert.That(sensor.TotalWeight, Is.EqualTo(1));
        Assert.That(sensor.ProviderCount, Is.EqualTo(1));
        button.GetComponent<BoxCollider>().enabled = false;
        yield return Settle();
        Assert.That(sensor.TotalWeight, Is.Zero);
        button.GetComponent<BoxCollider>().enabled = true;
        yield return Settle();
        Assert.That(sensor.TotalWeight, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator InspectorConnectionEditsWithdrawOldRoutesAndOverflowIsClamped()
    {
        var oldTrap = Trap();
        var newTrap = Trap();
        var a = Button(Vector3.left * 10, 0.2f, oldTrap);
        var b = Button(Vector3.right * 10, 0.2f, newTrap);
        Load(a, int.MaxValue);
        Load(b, int.MaxValue);
        yield return Settle();
        var coordinator = a.GetComponent<PressureButtonCoordinator>();
        Set(coordinator, "_spikeTrapTargets", new MonoBehaviour[] { newTrap, null, newTrap });
        Invoke(coordinator, "OnValidate");
        yield return Settle();
        Assert.That(oldTrap.TotalEffectiveWeight, Is.Zero);
        Assert.That(oldTrap.CurrentState, Is.EqualTo(SpikeTrapState.Raised));
        Assert.That(newTrap.TotalEffectiveWeight, Is.EqualTo(int.MaxValue));
        Assert.That(newTrap.ContributingButtons.Count, Is.EqualTo(2));
        coordinator.enabled = false;
        yield return Settle();
        Assert.That(newTrap.ContributingButtons.Count, Is.EqualTo(1));
        coordinator.enabled = true;
        yield return Settle();
        Assert.That(newTrap.ContributingButtons.Count, Is.EqualTo(2));
    }
}


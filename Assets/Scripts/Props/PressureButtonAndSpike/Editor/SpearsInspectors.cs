using UnityEditor;
using UnityEngine;

// Sólo presentación: las reglas y validaciones siguen en los componentes de runtime.
public abstract class SpearsInspectorBase : UnityEditor.Editor
{
    private bool _showReferences;

    protected void Field(string name, string label, string tooltip = null)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(name), new GUIContent(label, tooltip), true);
    }

    protected void References(params string[] names)
    {
        _showReferences = EditorGUILayout.Foldout(_showReferences, "Referencias internas (avanzado)", true);
        if (!_showReferences) return;
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            foreach (string name in names) EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
    }

    protected void Required(string name, string message)
    {
        if (serializedObject.FindProperty(name).objectReferenceValue == null)
            EditorGUILayout.HelpBox(message, MessageType.Warning);
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}

[CustomEditor(typeof(PressureButtonCoordinator)), CanEditMultipleObjects]
public sealed class PressureButtonCoordinatorInspector : SpearsInspectorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Field("_spikeTrapTargets", "Lanzas conectadas", "Arrastrá las instancias de Spears de la escena.");
        SerializedProperty targets = serializedObject.FindProperty("_spikeTrapTargets");
        if (!serializedObject.isEditingMultipleObjects)
        {
            if (targets.arraySize == 0)
                EditorGUILayout.HelpBox("Este botón todavía no controla ninguna lanza.", MessageType.Info);
            for (int i = 0; i < targets.arraySize; i++)
            {
                Object value = targets.GetArrayElementAtIndex(i).objectReferenceValue;
                if (value == null || value is not ISpikeTrapController)
                    EditorGUILayout.HelpBox($"Destino {i + 1}: asigná un SpikeTrapController válido.", MessageType.Warning);
                else if (value is not SpikeTrapController)
                    EditorGUILayout.HelpBox($"Destino {i + 1}: usa compatibilidad por estados, sin suma de peso.", MessageType.Info);
            }
        }
        References("_stateResolver", "_plateMover");
        Required("_stateResolver", "Falta el componente que resuelve el peso del botón.");
        Required("_plateMover", "Falta el componente de movimiento de la placa.");
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(PressureButtonStateResolver)), CanEditMultipleObjects]
public sealed class PressureButtonStateResolverInspector : SpearsInspectorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Field("_halfPressThreshold", "Peso para media pulsación");
        Field("_fullPressThreshold", "Peso para pulsación completa");
        References("_weightSensor", "_holdTimer");
        Required("_weightSensor", "Falta el sensor de peso del botón.");
        Required("_holdTimer", "Sin temporizador el botón no conserva la pulsación al quitar peso.");
        serializedObject.ApplyModifiedProperties();
        if (!Application.isPlaying || targets.Length != 1) return;
        var button = (PressureButtonStateResolver)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Peso real", button.RealWeight);
            EditorGUILayout.IntField("Aporte efectivo", button.EffectiveWeight);
            EditorGUILayout.EnumPopup("Estado de la placa", button.EffectiveState);
        }
    }
}

[CustomEditor(typeof(SpikeTrapController)), CanEditMultipleObjects]
public sealed class SpikeTrapControllerInspector : SpearsInspectorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Field("_halfRaisedWeight", "Peso para media altura");
        Field("_loweredWeight", "Peso para bajar completamente");
        Field("_raisedLocalPosition", "Posición arriba");
        Field("_halfRaisedLocalPosition", "Posición a media altura");
        Field("_loweredLocalPosition", "Posición abajo");
        Field("_initialState", "Estado inicial sin botones");
        Field("_moveDuration", "Duración del movimiento");
        Field("_moveCurve", "Curva del movimiento");
        Field("_shakeLocalDirection", "Dirección de vibración");
        Field("_shakeAmplitude", "Amplitud de vibración");
        Field("_shakeFrequency", "Frecuencia de vibración");
        Field("_shakeDuration", "Duración de vibración");
        References("_motionRoot", "_motionRigidbody", "_visualShakeRoot");
        Required("_motionRoot", "Falta la raíz móvil de las lanzas.");
        Required("_motionRigidbody", "Falta el Rigidbody de la raíz móvil.");
        Required("_visualShakeRoot", "Falta la raíz visual de vibración.");
        serializedObject.ApplyModifiedProperties();
        if (!Application.isPlaying || targets.Length != 1) return;
        var trap = (SpikeTrapController)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Peso total", trap.TotalEffectiveWeight);
            EditorGUILayout.EnumPopup("Estado actual", trap.CurrentState);
            EditorGUILayout.EnumPopup("Estado objetivo", trap.TargetState);
            foreach (PressureButtonStateResolver button in trap.ContributingButtons)
            {
                EditorGUILayout.ObjectField("Botón conectado", button, typeof(PressureButtonStateResolver), true);
                if (button != null) EditorGUILayout.IntField("Aporte", button.EffectiveWeight);
            }
        }
    }
}

[CustomEditor(typeof(PressureButtonHoldTimer)), CanEditMultipleObjects]
public sealed class PressureButtonHoldTimerInspector : SpearsInspectorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Field("_duration", "Retención (segundos)", "Los cambios se aplican a la siguiente cuenta regresiva.");
        References("_timerService");
        Required("_timerService", "Falta TimerService; la retención no podrá iniciarse.");
        serializedObject.ApplyModifiedProperties();
        if (!Application.isPlaying || targets.Length != 1) return;
        var timer = (PressureButtonHoldTimer)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Retención activa", timer.IsRunning);
            EditorGUILayout.Slider("Progreso", timer.Progress, 0f, 1f);
        }
    }
}

[CustomEditor(typeof(PressureButtonPlateMover)), CanEditMultipleObjects]
public sealed class PressureButtonPlateMoverInspector : SpearsInspectorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Field("_releasedLocalPosition", "Posición liberada");
        Field("_halfPressedLocalPosition", "Posición a media pulsación");
        Field("_fullyPressedLocalPosition", "Posición de pulsación completa");
        Field("_duration", "Duración del movimiento");
        Field("_movementCurve", "Curva del movimiento");
        Field("_initialState", "Estado inicial");
        References("_plate");
        Required("_plate", "Falta la referencia a la placa visual.");
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(MultiButtonTrapOrchestrator)), CanEditMultipleObjects]
public sealed class MultiButtonTrapOrchestratorInspector : SpearsInspectorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Conserva conexiones existentes. Para botones nuevos usá Lanzas conectadas en cada botón.", MessageType.Info);
        Field("_buttonResolvers", "Botones");
        Field("_spikeTrapTarget", "Lanza");
        Required("_spikeTrapTarget", "Sin lanza asignada este orquestador no tiene efecto.");
        Object destination = serializedObject.FindProperty("_spikeTrapTarget").objectReferenceValue;
        if (destination != null && destination is not ISpikeTrapController)
            EditorGUILayout.HelpBox("El destino debe implementar ISpikeTrapController.", MessageType.Warning);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(WeightSensor)), CanEditMultipleObjects]
public sealed class WeightSensorInspector : SpearsInspectorBase
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Field("_cleanupInterval", "Intervalo de limpieza");
        Field("_staleColliderGrace", "Espera antes de verificar superposición");
        using (new EditorGUI.DisabledScope(true))
        {
            Field("_totalWeight", "Peso total");
            Field("_providerCount", "Objetos detectados");
        }
        serializedObject.ApplyModifiedProperties();
    }
}

#if UNITY_EDITOR
using System;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerPrefsRegistry))]
public class PlayerPrefsRegistryEditor : Editor
{
    SerializedProperty _presetProp;
    SerializedProperty _lockProp;
    SerializedProperty _keyPrefixesProp;
    SerializedProperty _keysProp;
    SerializedProperty _valuesProp;

    bool _showEntries = true;
    string _search = "";

    void OnEnable()
    {
        _presetProp       = serializedObject.FindProperty("preset");
        _lockProp         = serializedObject.FindProperty("lockToPreset");
        _keyPrefixesProp  = serializedObject.FindProperty("keyPrefixes");
        _keysProp         = serializedObject.FindProperty("keys");
        _valuesProp       = serializedObject.FindProperty("values");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1) Preset arriba (como te gusta)
        DrawPresetUI();
        EditorGUILayout.Space(8);

        // 2) Prefijos visibles (se puede editar si no está lockeado)
        DrawPrefixesUI();
        EditorGUILayout.Space(8);

        // 3) Herramientas -> SOLO 2 botones
        DrawTools();
        EditorGUILayout.Space(8);

        // 4) Vista de entradas (solo lectura)
        DrawEntries();

        serializedObject.ApplyModifiedProperties();
    }

    // ----------------- PRESET -----------------
    void DrawPresetUI()
    {
        EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_presetProp);
        EditorGUILayout.PropertyField(_lockProp, new GUIContent("Lock a preset"));

        using (new EditorGUI.DisabledScope(
                   ((PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue) ==
                   PlayerPrefsRegistry.RegistryKeyPreset.None))
        {
            if (GUILayout.Button("Aplicar preset → Prefijos"))
            {
                var registry = (PlayerPrefsRegistry)target;
                var prefixes = PlayerPrefsRegistry.PresetToPrefixes(
                    (PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue);

                Undo.RecordObject(registry, "Apply Preset");
                SetStringArray(_keyPrefixesProp, prefixes);
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();

                Debug.Log($"[Registry] Prefijos actualizados desde preset: {string.Join(", ", prefixes)}");
            }
        }
    }

    // ----------------- PREFIJOS -----------------
    void DrawPrefixesUI()
    {
        EditorGUILayout.LabelField("Filtros (Prefijos aceptados)", EditorStyles.boldLabel);

        bool locked = _lockProp.boolValue &&
                      (PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue !=
                      PlayerPrefsRegistry.RegistryKeyPreset.None;

        using (new EditorGUI.DisabledScope(locked))
        {
            EditorGUILayout.PropertyField(_keyPrefixesProp, includeChildren: true);
        }

        EditorGUILayout.HelpBox(
            "Este Registry reflejará claves que comiencen con alguno de estos prefijos.\n" +
            "Sugerencia: usar el 'Preset' para no tipear strings.",
            MessageType.Info);
    }

    // ----------------- HERRAMIENTAS (2 BOTONES) -----------------
void DrawTools()
{
    EditorGUILayout.LabelField("Herramientas", EditorStyles.boldLabel);

    var currentPreset =
        (PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue;

    using (new EditorGUILayout.VerticalScope("box"))
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // BOTÓN 1 – Borrar datos del PRESET actual
            GUI.backgroundColor = new Color(0.95f, 0.75f, 0.3f);
            if (GUILayout.Button("Borrar datos de ESTE preset (Prefs + Registries)",
                                 GUILayout.Height(32)))
            {
                GUI.backgroundColor = Color.white;

                if (currentPreset == PlayerPrefsRegistry.RegistryKeyPreset.None)
                {
                    EditorUtility.DisplayDialog(
                        "Sin preset",
                        "Elegí un preset arriba (Gems, GemTotals, etc.) antes de usar este botón.",
                        "Ok");
                }
                else if (EditorUtility.DisplayDialog(
                             "Confirmar",
                             $"Se borrarán TODAS las PlayerPrefs de guardado que pertenezcan al preset '{currentPreset}'.\n\n" +
                             "Ejemplo: si elegís Gems+GemTotals, se borran todas las 'gem.*' y 'gemTotal.*'.",
                             "Sí, borrar", "Cancelar"))
                {
                    ClearByPreset(currentPreset);
                }
            }

            // BOTÓN 2 – Borrar TODO el guardado (todas las familias)
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("Borrar TODAS las keys de guardado",
                                 GUILayout.Height(32)))
            {
                GUI.backgroundColor = Color.white;

                if (EditorUtility.DisplayDialog(
                        "Confirmar borrado TOTAL",
                        "Se borrarán TODAS las keys de guardado del juego:\n" +
                        "- Gems\n- GemTotals\n- LevelCompleted\n- Volumen (sound/fx)\n\n" +
                        "Otros PlayerPrefs (plugins, editor, etc.) NO se tocan.",
                        "Sí, borrar todo", "Cancelar"))
                {
                    ClearByPreset(PlayerPrefsRegistry.RegistryKeyPreset.All);
                }
            }

            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.HelpBox(
            "• Botón 1: borra SOLO la familia del preset seleccionado (ej. todas las gemas).\n" +
            "• Botón 2: borra TODO el progreso/guardado del juego (todas las familias del enum RegistryKeyPreset),\n" +
            "  tanto en PlayerPrefs como en TODOS los PlayerPrefsRegistry.",
            MessageType.Info);
    }
}

    // ----------------- ENTRADAS (solo lectura) -----------------
    void DrawEntries()
    {
        _showEntries = EditorGUILayout.Foldout(
            _showEntries,
            $"Entradas (solo lectura) [{_keysProp.arraySize}]",
            true);
        if (!_showEntries) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            _search = EditorGUILayout.TextField("Buscar", _search);
            if (GUILayout.Button("Copiar todo", GUILayout.Width(100)))
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _keysProp.arraySize; i++)
                {
                    string k = _keysProp.GetArrayElementAtIndex(i).stringValue;
                    if (!string.IsNullOrEmpty(_search) && !k.Contains(_search)) continue;

                    string v = _valuesProp.GetArrayElementAtIndex(i).stringValue;
                    sb.AppendLine($"{k}={v}");
                }
                EditorGUIUtility.systemCopyBuffer = sb.ToString();
                ShowToast("Contenido copiado al portapapeles.");
            }
        }

        EditorGUILayout.Space(4);

        var head = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Key", head);
            GUILayout.Label("Value", head);
            GUILayout.Space(64);
        }
        EditorGUILayout.Separator();

        for (int i = 0; i < _keysProp.arraySize; i++)
        {
            string k = _keysProp.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrEmpty(_search) && !k.Contains(_search)) continue;

            string v = _valuesProp.GetArrayElementAtIndex(i).stringValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(k, GUILayout.Height(18));
                EditorGUILayout.SelectableLabel(v, GUILayout.Height(18));

                if (GUILayout.Button("Copiar", GUILayout.Width(60)))
                    EditorGUIUtility.systemCopyBuffer = k;

                if (GUILayout.Button("Borrar", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Borrar clave",
                        $"¿Eliminar '{k}' de PlayerPrefs y del Registry?", "Sí", "No"))
                    {
                        PlayerPrefs.DeleteKey(k);
                        PlayerPrefs.Save();

                        var registry = (PlayerPrefsRegistry)target;
                        Undo.RecordObject(registry, "Remove Entry");
                        registry.RemoveEntry(k);
                        EditorUtility.SetDirty(registry);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
    }

    // ----------------- HELPERS -----------------
    static bool HasAnyPrefix(string key, string[] prefixes)
    {
        if (prefixes == null || prefixes.Length == 0) return false;
        foreach (var p in prefixes)
        {
            if (!string.IsNullOrEmpty(p) &&
                key.StartsWith(p, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    void SetStringArray(SerializedProperty prop, string[] values)
    {
        prop.arraySize = values?.Length ?? 0;
        if (values == null) return;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    void ShowToast(string msg)
    {
        SceneView.lastActiveSceneView?.ShowNotification(new GUIContent(msg));
    }

static void ClearByPreset(PlayerPrefsRegistry.RegistryKeyPreset preset)
{
    // Mapeamos el preset a los prefijos reales usando PrefKeys.Prefix
    string[] prefixes = PlayerPrefsRegistry.PresetToPrefixes(preset);
    if (prefixes == null || prefixes.Length == 0)
    {
        Debug.LogWarning($"[Registry] ClearByPreset llamado con preset {preset}, pero no hay prefijos asociados.");
        return;
    }

    int totalKeys = 0;
    int totalRegistries = 0;

    // Buscamos TODOS los PlayerPrefsRegistry del proyecto
    string[] guids = AssetDatabase.FindAssets("t:PlayerPrefsRegistry");
    foreach (var guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var reg = AssetDatabase.LoadAssetAtPath<PlayerPrefsRegistry>(path);
        if (reg == null) continue;

        var so = new SerializedObject(reg);
        var keysProp   = so.FindProperty("keys");
        var valuesProp = so.FindProperty("values");

        var indices = new List<int>();

        for (int i = 0; i < keysProp.arraySize; i++)
        {
            string k = keysProp.GetArrayElementAtIndex(i).stringValue;
            if (string.IsNullOrEmpty(k)) continue;
            if (!HasAnyPrefix(k, prefixes)) continue;

            // Borramos la PlayerPref real
            PlayerPrefs.DeleteKey(k);
            indices.Add(i);
        }

        if (indices.Count > 0)
        {
            // Borramos las entradas en el SO, de atrás para adelante
            indices.Sort();
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                int idx = indices[i];
                keysProp.DeleteArrayElementAtIndex(idx);
                valuesProp.DeleteArrayElementAtIndex(idx);
                totalKeys++;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(reg);
            totalRegistries++;
        }
    }

    PlayerPrefs.Save();

    Debug.Log($"[Registry] Borradas {totalKeys} keys para preset {preset}. Registries afectados: {totalRegistries}.");

    var view = SceneView.lastActiveSceneView;
    view?.ShowNotification(new GUIContent($"Borradas {totalKeys} keys ({preset})."));
}

}
#endif

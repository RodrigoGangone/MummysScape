#if UNITY_EDITOR
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

        DrawPresetUI();
        EditorGUILayout.Space(8);

        // ---------- Filtros ----------
        EditorGUILayout.LabelField("Filtros (Prefijos aceptados)", EditorStyles.boldLabel);

        // Si está lockeado al preset, muestro los prefijos pero deshabilitados
        bool locked = _lockProp.boolValue && (PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue != PlayerPrefsRegistry.RegistryKeyPreset.None;
        using (new EditorGUI.DisabledScope(locked))
        {
            EditorGUILayout.PropertyField(_keyPrefixesProp, includeChildren: true);
        }

        EditorGUILayout.HelpBox(
            "Este Registry reflejará claves que comiencen con alguno de estos prefijos.\n" +
            "Sugerencia: usar el 'Preset' para no tipear strings.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        // ---------- Herramientas ----------
        DrawTools();

        EditorGUILayout.Space(8);

        // ---------- Entradas (solo lectura) ----------
        DrawEntries();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawPresetUI()
    {
        EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_presetProp);
        EditorGUILayout.PropertyField(_lockProp, new GUIContent("Lock a preset"));

        // Botón: Aplicar preset → Prefijos
        using (new EditorGUI.DisabledScope(((PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue) == PlayerPrefsRegistry.RegistryKeyPreset.None))
        {
            if (GUILayout.Button("Aplicar preset → Prefijos"))
            {
                var registry = (PlayerPrefsRegistry)target;
                var prefixes = PlayerPrefsRegistry.PresetToPrefixes((PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue);
                Undo.RecordObject(registry, "Apply Preset");
                SetStringArray(_keyPrefixesProp, prefixes);
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Registry] Prefijos actualizados desde preset: {string.Join(", ", prefixes)}");
            }
        }
    }

    void DrawTools()
    {
        var registry = (PlayerPrefsRegistry)target;

        EditorGUILayout.LabelField("Herramientas", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Borrar SOLO Registry"))
            {
                Undo.RecordObject(registry, "Clear Registry");
                registry.ClearAll();
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                Debug.Log("[Registry] Lista de entradas limpiada.");
            }

            if (GUILayout.Button("Borrar PlayerPrefs (listados)"))
            {
                if (EditorUtility.DisplayDialog(
                        "Confirmar",
                        "Se borrarán de PlayerPrefs todas las claves actualmente listadas en este Registry.",
                        "Sí", "No"))
                {
                    for (int i = 0; i < _keysProp.arraySize; i++)
                    {
                        string k = _keysProp.GetArrayElementAtIndex(i).stringValue;
                        PlayerPrefs.DeleteKey(k);
                    }
                    PlayerPrefs.Save();
                    Debug.Log("[PlayerPrefs] Claves listadas borradas.");
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Borrar TODO (Prefs listados + Registry)"))
            {
                if (EditorUtility.DisplayDialog(
                        "Confirmar",
                        "Se borrarán de PlayerPrefs las claves listadas y se vaciará el Registry.",
                        "Sí", "No"))
                {
                    Undo.RecordObject(registry, "Clear All");

                    for (int i = 0; i < _keysProp.arraySize; i++)
                    {
                        string k = _keysProp.GetArrayElementAtIndex(i).stringValue;
                        PlayerPrefs.DeleteKey(k);
                    }
                    PlayerPrefs.Save();

                    registry.ClearAll();
                    EditorUtility.SetDirty(registry);
                    AssetDatabase.SaveAssets();

                    Debug.Log("[PlayerPrefs] y [Registry] borrados.");
                }
            }

            if (GUILayout.Button("Purgar entradas no-matcheadas"))
            {
                Undo.RecordObject(registry, "Purge Non-Matching");
                var toRemove = new List<string>();
                for (int i = 0; i < _keysProp.arraySize; i++)
                {
                    string k = _keysProp.GetArrayElementAtIndex(i).stringValue;
                    if (!registry.Matches(k)) toRemove.Add(k);
                }
                foreach (var k in toRemove)
                    registry.RemoveEntry(k);

                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Registry] Entradas purgadas: {toRemove.Count}");
            }
        }

        EditorGUILayout.Space(6);

        // -------- NUEVO: BORRAR TODO GLOBAL --------
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("🚨 Borrar TODO GLOBAL (PlayerPrefs + TODOS los Registries)"))
            {
                GUI.backgroundColor = Color.white;
                if (EditorUtility.DisplayDialog(
                        "Borrar TODO GLOBAL",
                        "Esto eliminará TODOS los PlayerPrefs y vaciará TODOS los PlayerPrefsRegistry del proyecto.\n¿Estás seguro?",
                        "Sí, borrar todo", "Cancelar"))
                {
                    // 1) Borrar todos los PlayerPrefs
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();

                    // 2) Limpiar todos los registries del proyecto
                    string[] guids = AssetDatabase.FindAssets("t:PlayerPrefsRegistry");
                    int count = 0;
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var reg = AssetDatabase.LoadAssetAtPath<PlayerPrefsRegistry>(path);
                        if (reg == null) continue;
                        Undo.RecordObject(reg, "Clear All Registries");
                        reg.ClearAll();
                        EditorUtility.SetDirty(reg);
                        count++;
                    }
                    AssetDatabase.SaveAssets();

                    Debug.Log($"[GLOBAL] PlayerPrefs borrados y {count} registries limpiados.");
                    ShowToast("Se borró TODO GLOBAL (Prefs + Registries).");
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }

    void DrawEntries()
    {
        _showEntries = EditorGUILayout.Foldout(_showEntries, $"Entradas (solo lectura) [{_keysProp.arraySize}]", true);
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

    // ---- helpers ----
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
}
#endif

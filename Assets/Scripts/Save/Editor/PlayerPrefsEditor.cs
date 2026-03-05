#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(PlayerPrefsRegistry))]
public class PlayerPrefsRegistryEditor : Editor
{
    SerializedProperty _presetProp;
    SerializedProperty _lockProp;
    SerializedProperty _keyPrefixesProp;
    SerializedProperty _keysProp;
    SerializedProperty _valuesProp;

    bool _showEntries = true;
    bool _showCreator = false;
    string _search = "";
    Vector2 _scrollPos;

    // Variables para creación manual
    string _newKey = "";
    string _newValue = "1";
    int _newTypeIndex = 0;

    // Estilos
    GUIStyle _headerStyle;
    GUIStyle _groupHeaderStyle;

    void OnEnable()
    {
        _presetProp = serializedObject.FindProperty("preset");
        _lockProp = serializedObject.FindProperty("lockToPreset");
        _keyPrefixesProp = serializedObject.FindProperty("keyPrefixes");
        _keysProp = serializedObject.FindProperty("keys");
        _valuesProp = serializedObject.FindProperty("values");

        SyncValuesFromDisk();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        InitStyles();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Gestor de Guardado (Maxi Engine)", _headerStyle);
        EditorGUILayout.Space(5);

        DrawConfigSection();
        DrawSimulatorSection();
        DrawToolsSection();
        DrawDataSection();

        serializedObject.ApplyModifiedProperties();
    }

    // ---------------------------------------------------------
    // SECCIÓN 1: CONFIGURACIÓN
    // ---------------------------------------------------------
    void DrawConfigSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Filtros y Presets", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var current = (PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue;
                var next = (PlayerPrefsRegistry.RegistryKeyPreset)EditorGUILayout.EnumFlagsField(
                    new GUIContent("Preset Activo"), current);
                if (EditorGUI.EndChangeCheck())
                {
                    _presetProp.intValue = (int)next;
                    ApplyPresetPrefixes();
                }

                var icon = _lockProp.boolValue ? "InspectorLock" : "InspectorUnlock";
                if (GUILayout.Button(EditorGUIUtility.IconContent(icon), EditorStyles.iconButton, GUILayout.Width(22),
                        GUILayout.Height(18)))
                    _lockProp.boolValue = !_lockProp.boolValue;
            }

            bool locked = _lockProp.boolValue && (PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue !=
                PlayerPrefsRegistry.RegistryKeyPreset.None;
            using (new EditorGUI.DisabledScope(locked))
                EditorGUILayout.PropertyField(_keyPrefixesProp, new GUIContent("Prefijos Rastreados"), true);
        }
        EditorGUILayout.Space(5);
    }

    // ---------------------------------------------------------
    // SECCIÓN 2: SIMULADOR
    // ---------------------------------------------------------
    void DrawSimulatorSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            _showCreator = EditorGUILayout.Foldout(_showCreator, "Simulador / Crear Datos", true, EditorStyles.foldoutHeader);
            if (_showCreator)
            {
                EditorGUILayout.HelpBox("Crea claves manualmente para simular progreso.", MessageType.None);
                using (new EditorGUILayout.HorizontalScope())
                {
                    string[] opts = { "Int", "Float", "String" };
                    _newTypeIndex = EditorGUILayout.Popup(_newTypeIndex, opts, GUILayout.Width(60));
                    _newKey = EditorGUILayout.TextField(_newKey, GUILayout.ExpandWidth(true));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Valor:", GUILayout.Width(40));
                    _newValue = EditorGUILayout.TextField(_newValue);
                    GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
                    if (GUILayout.Button(EditorGUIUtility.IconContent("SaveAs"), GUILayout.Height(20), GUILayout.Width(40))) SimulateData();
                    GUI.backgroundColor = Color.white;
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Gem Global", EditorStyles.miniButton)) { _newKey = "gemTotal.Global"; _newValue = "100"; }
                if (GUILayout.Button("Level 1 Nav", EditorStyles.miniButton)) { _newKey = "nav.last_visited_index"; _newValue = "1"; }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.Space(5);
    }

    // ---------------------------------------------------------
    // SECCIÓN 3: HERRAMIENTAS
    // ---------------------------------------------------------
    void DrawToolsSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Mantenimiento", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent(" Escanear Disco", EditorGUIUtility.IconContent("d_ViewToolOrbit").image), GUILayout.Height(24)))
                    PerformSmartDiscovery();

                if (GUILayout.Button(new GUIContent(" Organizar (Tipo)", EditorGUIUtility.IconContent("AlphabeticalSorting").image), GUILayout.Height(24)))
                    SortRegistryByType();
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.2f);
                if (GUILayout.Button(new GUIContent(" Limpiar Vista", EditorGUIUtility.IconContent("TreeEditor.Trash").image), GUILayout.Height(24)))
                    ConfirmAndClearVisible();

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button(new GUIContent(" FORMAT TOTAL", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), GUILayout.Height(24)))
                    ConfirmAndNukeAll();
                GUI.backgroundColor = Color.white;
            }
        }
        EditorGUILayout.Space(5);
    }

    // ---------------------------------------------------------
    // SECCIÓN 4: DATOS
    // ---------------------------------------------------------
    void DrawDataSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), EditorStyles.iconButton, GUILayout.Width(22)))
                {
                    SyncValuesFromDisk();
                    ShowToast("Sincronizado");
                }

                _showEntries = EditorGUILayout.Foldout(_showEntries, $"Datos [{_keysProp.arraySize}]", true, EditorStyles.foldoutHeader);
                if (_showEntries)
                {
                    GUILayout.FlexibleSpace();
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(120));
                }
            }

            if (!_showEntries) return;
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Key", GUILayout.Width(200));
                GUILayout.Label("Value", GUILayout.ExpandWidth(true));
                GUILayout.Label("", GUILayout.Width(80));
            }

            var currentPrefixes = GetCurrentPrefixes();
            string lastGroup = "";

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MinHeight(100), GUILayout.MaxHeight(400));

            int visibleCount = 0;
            for (int i = 0; i < _keysProp.arraySize; i++)
            {
                string k = _keysProp.GetArrayElementAtIndex(i).stringValue;

                if (!MatchesPrefix(k, currentPrefixes)) continue;
                if (!string.IsNullOrEmpty(_search) && !k.Contains(_search, StringComparison.OrdinalIgnoreCase)) continue;

                string currentGroup = GetGroupHeader(k);
                if (currentGroup != lastGroup && string.IsNullOrEmpty(_search))
                {
                    DrawGroupHeader(currentGroup);
                    lastGroup = currentGroup;
                }

                DrawEditableRow(i);
                visibleCount++;
            }

            if (visibleCount == 0 && _keysProp.arraySize > 0)
                EditorGUILayout.LabelField("Oculto por filtro.", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();
        }
    }

    void DrawGroupHeader(string title)
    {
        EditorGUILayout.Space(5);
        var rect = EditorGUILayout.GetControlRect(false, 18);
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        EditorGUI.LabelField(rect, title, _groupHeaderStyle);
    }

    void DrawEditableRow(int index)
    {
        SerializedProperty keyProp = _keysProp.GetArrayElementAtIndex(index);
        SerializedProperty valProp = _valuesProp.GetArrayElementAtIndex(index);
        string k = keyProp.stringValue;
        string v = valProp.stringValue;

        if (index % 2 == 0) EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 22), new Color(0, 0, 0, 0.05f));

        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(22)))
        {
            EditorGUILayout.SelectableLabel(k, EditorStyles.label, GUILayout.Width(200), GUILayout.Height(20));

            bool exists = PlayerPrefs.HasKey(k);
            if (!exists) GUI.color = new Color(1, 1, 1, 0.5f);

            EditorGUI.BeginChangeCheck();
            string newValue = EditorGUILayout.DelayedTextField(v, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                UpdateDiskValue(k, newValue);
                valProp.stringValue = newValue;
            }

            GUI.color = Color.white;

            if (string.IsNullOrEmpty(_search))
            {
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(20))) MoveItem(index, -1);
                if (GUILayout.Button("▼", EditorStyles.miniButtonRight, GUILayout.Width(20))) MoveItem(index, 1);
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("SaveAs"), EditorStyles.iconButton, GUILayout.Width(22)))
            {
                UpdateDiskValue(k, newValue);
                ShowToast("Guardado");
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), EditorStyles.iconButton, GUILayout.Width(22)))
                DeleteSingleKey(k);
        }
    }

    // ---------------------------------------------------------
    // LÓGICA DE NEGOCIO
    // ---------------------------------------------------------

    void SortRegistryByType()
    {
        var reg = (PlayerPrefsRegistry)target;
        var list = new List<KeyValuePair<string, string>>();
        var so = new SerializedObject(reg);
        var kP = so.FindProperty("keys");
        var vP = so.FindProperty("values");

        for (int i = 0; i < kP.arraySize; i++)
            list.Add(new KeyValuePair<string, string>(kP.GetArrayElementAtIndex(i).stringValue, vP.GetArrayElementAtIndex(i).stringValue));

        list.Sort((a, b) =>
        {
            int wA = GetTypeWeight(a.Key);
            int wB = GetTypeWeight(b.Key);
            if (wA != wB) return wA.CompareTo(wB);
            return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
        });

        kP.ClearArray(); vP.ClearArray();
        for (int i = 0; i < list.Count; i++)
        {
            kP.InsertArrayElementAtIndex(i); vP.InsertArrayElementAtIndex(i);
            kP.GetArrayElementAtIndex(i).stringValue = list[i].Key;
            vP.GetArrayElementAtIndex(i).stringValue = list[i].Value;
        }

        so.ApplyModifiedProperties();
        ShowToast("Lista organizada");
    }

    void MoveItem(int index, int direction)
    {
        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _keysProp.arraySize) return;
        _keysProp.MoveArrayElement(index, newIndex);
        _valuesProp.MoveArrayElement(index, newIndex);
        serializedObject.ApplyModifiedProperties();
    }

    string GetGroupHeader(string key)
    {
        if (key.StartsWith("level.")) return "--- LEVELS ---";
        if (key.StartsWith("nav.")) return "--- NAVIGATION ---"; //
        if (key.StartsWith("seen.cinematic.")) return "--- CINEMATICS ---";
        if (key.StartsWith("seen.tutorial.")) return "--- SEEN (TUTORIALS) ---";
        if (key.StartsWith("seen.level")) return "--- SEEN (REVEALS) ---";
        if (key.StartsWith("seen.")) return "--- SEEN (OTHERS) ---";
        if (key.StartsWith("gemTotal.")) return "--- GEM TOTALS ---";
        if (key.StartsWith("gem.")) return "--- GEMS ---";
        if (key.StartsWith("volume.")) return "--- AUDIO / SETTINGS ---";
        return "--- OTROS ---";
    }

    int GetTypeWeight(string key)
    {
        if (key.StartsWith("level.")) return 0;
        if (key.StartsWith("nav.")) return 2; //
        if (key.StartsWith("seen.cinematic.")) return 5;
        if (key.StartsWith("seen.level")) return 10;
        if (key.StartsWith("seen.tutorial")) return 11;
        if (key.StartsWith("seen.")) return 12;
        if (key.StartsWith("gemTotal.")) return 20;
        if (key.StartsWith("gem.")) return 21;
        if (key.StartsWith("volume.")) return 90;
        return 100;
    }

    void PerformSmartDiscovery()
    {
        var registry = (PlayerPrefsRegistry)target;
        int found = 0;
        var potentialKeys = new HashSet<string>();

        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(currentScene))
        {
            for (int i = 1; i <= 50; i++) potentialKeys.Add($"gem.{i}.{currentScene}");
            potentialKeys.Add($"gemTotal.{currentScene}");
        }

        potentialKeys.Add("gemTotal.Global");
        potentialKeys.Add(PrefKeys.SeenGemsCount()); //
        potentialKeys.Add(PrefKeys.LastLevelPlayed); //

        for (int i = 0; i <= 50; i++)
        {
            potentialKeys.Add($"level.completed.index.{i}");
            potentialKeys.Add($"seen.level_reveal.{i}");
        }

        potentialKeys.Add("volume.sound.master");
        potentialKeys.Add("volume.sound.music");
        potentialKeys.Add("volume.fx.sfx");

        foreach (var key in potentialKeys)
        {
            if (PlayerPrefs.HasKey(key) && registry.Matches(key))
            {
                registry.UpdateEntry(key, GetValueAsString(key));
                found++;
            }
        }

        if (found > 0) { EditorUtility.SetDirty(registry); serializedObject.Update(); Repaint(); ShowToast($"{found} claves importadas."); }
        else ShowToast("Sin datos nuevos.");
    }

    // --- Helpers de Persistencia ---
    void SimulateData()
    {
        if (string.IsNullOrEmpty(_newKey)) return;
        try
        {
            if (_newTypeIndex == 0) PlayerPrefs.SetInt(_newKey, int.Parse(_newValue));
            else if (_newTypeIndex == 1) PlayerPrefs.SetFloat(_newKey, float.Parse(_newValue));
            else PlayerPrefs.SetString(_newKey, _newValue);
            PlayerPrefs.Save();
            ((PlayerPrefsRegistry)target).UpdateEntry(_newKey, _newValue);
            EditorUtility.SetDirty(target); ShowToast("Creado");
        }
        catch { ShowToast("Error formato"); }
    }

    void UpdateDiskValue(string key, string s)
    {
        if (int.TryParse(s, out int i)) PlayerPrefs.SetInt(key, i);
        else if (float.TryParse(s, out float f)) PlayerPrefs.SetFloat(key, f);
        else PlayerPrefs.SetString(key, s);
        PlayerPrefs.Save();
    }

    void SyncValuesFromDisk()
    {
        bool changed = false;
        for (int i = 0; i < _keysProp.arraySize; i++)
        {
            string k = _keysProp.GetArrayElementAtIndex(i).stringValue;
            if (PlayerPrefs.HasKey(k))
            {
                string disk = GetValueAsString(k);
                var vp = _valuesProp.GetArrayElementAtIndex(i);
                if (vp.stringValue != disk) { vp.stringValue = disk; changed = true; }
            }
        }
        if (changed) serializedObject.ApplyModifiedProperties();
    }

    string GetValueAsString(string key)
    {
        string s = PlayerPrefs.GetString(key, "__NOT_STRING__");
        if (s != "__NOT_STRING__") return s;
        float f = PlayerPrefs.GetFloat(key, float.NaN);
        int i = PlayerPrefs.GetInt(key, 0);
        if (!float.IsNaN(f) && Math.Abs(f % 1) > 0.0001f) return f.ToString("F2");
        return i.ToString();
    }

    void ApplyPresetPrefixes()
    {
        var p = PlayerPrefsRegistry.PresetToPrefixes((PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue);
        _keyPrefixesProp.arraySize = p.Length;
        for (int i = 0; i < p.Length; i++) _keyPrefixesProp.GetArrayElementAtIndex(i).stringValue = p[i];
        serializedObject.ApplyModifiedProperties();
    }

    string[] GetCurrentPrefixes()
    {
        var l = new List<string>();
        for (int i = 0; i < _keyPrefixesProp.arraySize; i++) l.Add(_keyPrefixesProp.GetArrayElementAtIndex(i).stringValue);
        return l.ToArray();
    }

    bool MatchesPrefix(string k, string[] p)
    {
        if (p == null || p.Length == 0) return true;
        foreach (var x in p) if (!string.IsNullOrEmpty(x) && k.StartsWith(x)) return true;
        return false;
    }

    void ConfirmAndClearVisible() { if (EditorUtility.DisplayDialog("Limpiar", "¿Borrar visibles?", "Si", "No")) ClearByPreset((PlayerPrefsRegistry.RegistryKeyPreset)_presetProp.intValue); }
    void ConfirmAndNukeAll() { if (EditorUtility.DisplayDialog("NUCLEAR", "¿Borrar TODO?", "Si", "No")) { PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); ((PlayerPrefsRegistry)target).ClearAll(); } }
    void DeleteSingleKey(string k) { if (EditorUtility.DisplayDialog("Borrar", k, "Si", "No")) { PlayerPrefs.DeleteKey(k); ((PlayerPrefsRegistry)target).RemoveEntry(k); } }

    void ClearByPreset(PlayerPrefsRegistry.RegistryKeyPreset preset)
    {
        string[] prefixes = PlayerPrefsRegistry.PresetToPrefixes(preset);
        var so = new SerializedObject(target);
        var kP = so.FindProperty("keys");
        var vP = so.FindProperty("values");
        for (int i = kP.arraySize - 1; i >= 0; i--)
        {
            string k = kP.GetArrayElementAtIndex(i).stringValue;
            foreach (var p in prefixes) if (k.StartsWith(p)) { PlayerPrefs.DeleteKey(k); kP.DeleteArrayElementAtIndex(i); vP.DeleteArrayElementAtIndex(i); break; }
        }
        so.ApplyModifiedProperties(); PlayerPrefs.Save();
    }

    void InitStyles()
    {
        if (_headerStyle == null) _headerStyle = new GUIStyle(EditorStyles.largeLabel) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        if (_groupHeaderStyle == null) _groupHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }, alignment = TextAnchor.MiddleCenter };
    }

    void ShowToast(string m) => SceneView.lastActiveSceneView?.ShowNotification(new GUIContent(m));
}
#endif
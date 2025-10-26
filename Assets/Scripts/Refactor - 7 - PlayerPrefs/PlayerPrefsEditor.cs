using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerPrefsRegistry))]
public class PlayerPrefsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Dibuja los campos normales del ScriptableObject
        DrawDefaultInspector();

        var registry = (PlayerPrefsRegistry)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Herramientas", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Borrar SOLO Registry"))
        {
            Undo.RecordObject(registry, "Clear Registry");
            registry.ClearAll();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log("[Registry] Lista de entradas limpiada.");
        }

        if (GUILayout.Button("Borrar PlayerPrefs"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[PlayerPrefs] Todos los prefs borrados.");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Borrar TODO (Prefs + Registry)"))
        {
            Undo.RecordObject(registry, "Clear All");
            registry.ClearAll();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("[PlayerPrefs] y [Registry] borrados.");
        }
    }
}
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameEvent))]
public class CustomGameEvent : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var gameEvent = (GameEvent)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("=== Subscribers (runtime only) ===", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            if (gameEvent.Listeners.Count == 0)
            {
                EditorGUILayout.LabelField("   (no listeners)");
            }
            else
            {
                foreach (var listener in gameEvent.Listeners)
                {
                    if (listener != null)
                        EditorGUILayout.LabelField($" - {listener.Target} → {listener.Method.Name}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Run the game to see subscribers.", MessageType.Info);
        }
    }
}
#endif

using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
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
            int total = gameEvent.NoParamListeners.Count + gameEvent.ParamListeners.Count;

            if (total == 0)
            {
                EditorGUILayout.LabelField("   (no listeners)");
            }
            else
            {
                if (gameEvent.NoParamListeners.Count > 0)
                {
                    EditorGUILayout.LabelField("Without Parameters:", EditorStyles.boldLabel);
                    foreach (var listener in gameEvent.NoParamListeners)
                    {
                        if (listener != null)
                            EditorGUILayout.LabelField($" • {listener.Target} → {listener.Method.Name}");
                    }
                }

                if (gameEvent.ParamListeners.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("With Parameters:", EditorStyles.boldLabel);
                    foreach (var listener in gameEvent.ParamListeners)
                    {
                        if (listener != null)
                            EditorGUILayout.LabelField($" • {listener.Target} → {listener.Method.Name}");
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Run the game to see current subscribers.", MessageType.Info);
        }
    }
}
#endif
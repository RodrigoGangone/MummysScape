#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[CustomEditor(typeof(GameEvent))]
[CanEditMultipleObjects]
public class CustomGameEvent : Editor
{
    private bool _autoRefresh = true;
    private string _testPayload = "";
    private string _search = "";
    private Vector2 _scroll;

    // No uses claves null en el diccionario
    private readonly Dictionary<UnityEngine.Object, bool> _foldouts = new();

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }
    private double _nextRepaint;
    private void OnEditorUpdate()
    {
        if (!_autoRefresh || !Application.isPlaying) return;
        if (EditorApplication.timeSinceStartup >= _nextRepaint)
        {
            Repaint();
            _nextRepaint = EditorApplication.timeSinceStartup + 0.25; // 4 fps
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var evt = (GameEvent)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("GameEvent • Runtime", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            _autoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh (Play Mode)", _autoRefresh);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Raise()", GUILayout.Height(22))) evt.Raise();
            if (GUILayout.Button("Raise(obj)", GUILayout.Height(22))) evt.Raise(_testPayload);
            EditorGUILayout.EndHorizontal();

            _testPayload = EditorGUILayout.TextField("Payload de prueba:", _testPayload);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Suscriptores (Play Mode)", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Entrá en Play para ver los suscriptores actuales.", MessageType.Info);
            return;
        }

        DrawSubscribers(evt);
    }

    private void DrawSubscribers(GameEvent evt)
    {
        var noParam = evt.NoParamListeners ?? Array.Empty<Action>();
        var withParam = evt.ParamListeners ?? Array.Empty<Action<object>>();

        // Construimos entradas: para Param intentamos desempaquetar el wrapper a su delegate real
        var entries = new List<SubEntry>(noParam.Count + withParam.Count);

        foreach (var d in noParam)
            entries.Add(SubEntry.FromDelegate(d, "void Raise()"));

        foreach (var d in withParam)
        {
            var real = TryUnwrapRealDelegate(d) ?? d; // si no se puede, mostramos el wrapper
            entries.Add(SubEntry.FromDelegate(real, "void Raise(object payload)"));
        }

        // Búsqueda
        if (!string.IsNullOrWhiteSpace(_search))
        {
            string term = _search.Trim();
            entries = entries.Where(e =>
                (e.TargetName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (e.ComponentType?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (e.MethodName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (e.SceneName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (e.HierarchyPath?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
            ).ToList();
        }

        int count = entries.Count;
        EditorGUILayout.LabelField($"Total: {count}");

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            _search = EditorGUILayout.TextField("Buscar", _search);
            if (GUILayout.Button("Limpiar", GUILayout.Width(70))) _search = "";
        }

        if (count == 0)
        {
            EditorGUILayout.HelpBox("No hay listeners registrados.", MessageType.Info);
            return;
        }

        // Agrupado por TargetObject (si no hay, usamos 'this' como clave sentinela)
        var byTarget = entries
            .GroupBy(e => (UnityEngine.Object)(e.TargetObject ?? this))
            .OrderBy(g => ReferenceEquals(g.Key, this) ? 1 : 0)
            .ThenBy(g => g.Key.name);

        EditorGUILayout.Space(6);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(160));

        foreach (var group in byTarget)
        {
            UnityEngine.Object key = group.Key;
            bool isStaticGroup = ReferenceEquals(key, this);

            string header;
            GUIContent iconContent = null;

            if (isStaticGroup)
            {
                header = $"<Static/Unknown Target>  ({group.Count()})";
            }
            else
            {
                var targetObj = key;
                GameObject go = (targetObj is Component c) ? c.gameObject :
                                (targetObj is GameObject g) ? g : null;

                string scene = go ? (go.scene.IsValid() ? go.scene.name : "(no scene)") : "(asset)";
                string path = go ? GetHierarchyPath(go.transform) : "(asset)";

                header = $"{targetObj.name}  •  {scene}  •  {path}  ({group.Count()})";
                iconContent = EditorGUIUtility.ObjectContent(targetObj, targetObj.GetType());
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool open = _foldouts.TryGetValue(key, out var v) ? v : true;

                using (new EditorGUILayout.HorizontalScope())
                {
                    // Foldout (overload compatible)
                    var foldRect = GUILayoutUtility.GetRect(14, 18, GUILayout.Width(14));
                    open = EditorGUI.Foldout(foldRect, open, GUIContent.none, true);

                    if (!ReferenceEquals(iconContent, null))
                        GUILayout.Label(iconContent.image, GUILayout.Width(18), GUILayout.Height(18));
                    EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();

                    if (!ReferenceEquals(key, this))
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(50))) EditorGUIUtility.PingObject(key);
                        if (GUILayout.Button("Select", GUILayout.Width(60))) Selection.activeObject = key;
                    }
                }

                _foldouts[key] = open;
                if (!open) continue;

                foreach (var e in group.OrderBy(x => x.ComponentType).ThenBy(x => x.MethodName))
                    DrawMethodRow(e);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMethodRow(SubEntry e)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (e.ComponentIcon != null) GUILayout.Label(e.ComponentIcon, GUILayout.Width(18), GUILayout.Height(18));
            else GUILayout.Space(20);

            var rich = $"<b>{e.ComponentType}</b> → {e.MethodName}  <color=#888888>({e.Signature})</color>";
            if (e.IsStatic) rich = "⚙️ <b>static</b> → " + rich;
            if (e.IsDestroyed) rich = $"<color=#ff6a6a>✖ (destroyed)</color>  {rich}";

            GUILayout.Label(new GUIContent(rich), GetRichLabel());
        }

        using (new EditorGUI.IndentLevelScope(1))
        {
            var scene = string.IsNullOrEmpty(e.SceneName) ? "(n/a)" : e.SceneName;
            var path = string.IsNullOrEmpty(e.HierarchyPath) ? "(n/a)" : e.HierarchyPath;
            EditorGUILayout.LabelField($"Escena: {scene}    Ruta: {path}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(2);
    }

    private static GUIStyle _richLabel;
    private static GUIStyle GetRichLabel()
    {
        if (_richLabel == null)
            _richLabel = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = false };
        return _richLabel;
    }
    private static string GetHierarchyPath(Transform t)
    {
        if (!t) return "";
        var stack = new Stack<string>();
        while (t) { stack.Push(t.name); t = t.parent; }
        return string.Join("/", stack);
    }

    // ---------- UNWRAP DEL WRAPPER DE Register<T> ----------
    // Intenta extraer el delegate real desde el closure del wrapper (Action<object>)
    private static Delegate TryUnwrapRealDelegate(Delegate wrapper)
    {
        if (wrapper == null) return null;

        // Caso común: Target del wrapper es una clase de closure (DisplayClass)
        object target = wrapper.Target;
        if (target == null) return null;

        var t = target.GetType();
        // Heurística típica de closures C#: tipo anidado, privado y con "<>" en el nombre
        bool looksLikeClosure =
            t.IsNestedPrivate &&
            (t.Name.Contains("DisplayClass") || t.Name.Contains("<>"));

        if (!looksLikeClosure)
            return null;

        // Buscamos cualquier campo que sea Delegate (Action<>, Func<>, etc.)
        // Recursivo: a veces el closure referencia otro closure
        return FindDelegateFieldRecursive(target, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    private static Delegate FindDelegateFieldRecursive(object obj, HashSet<object> seen)
    {
        if (obj == null || seen.Contains(obj)) return null;
        seen.Add(obj);

        var type = obj.GetType();
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var f in type.GetFields(BF))
        {
            var val = f.GetValue(obj);
            if (val == null) continue;

            if (val is Delegate del)
                return del;

            // Si es otra clase "closure", seguimos
            var ft = f.FieldType;
            bool potentialClosure = !ft.IsPrimitive && !ft.IsEnum && !typeof(UnityEngine.Object).IsAssignableFrom(ft);
            if (potentialClosure)
            {
                var inner = FindDelegateFieldRecursive(val, seen);
                if (inner != null) return inner;
            }
        }

        return null;
    }

    // Comparador por referencia para evitar bucles al recorrer closures
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    // ----- Modelo para mostrar -----
    private class SubEntry
    {
        public UnityEngine.Object TargetObject;
        public string TargetName;
        public string ComponentType;
        public Texture ComponentIcon;
        public string MethodName;
        public string Signature;
        public string SceneName;
        public string HierarchyPath;
        public bool IsStatic;
        public bool IsDestroyed;

        public static SubEntry FromDelegate(Delegate d, string signature)
        {
            var e = new SubEntry
            {
                Signature = signature,
                MethodName = d?.Method?.Name ?? "(null)",
                IsStatic = d?.Method?.IsStatic ?? false
            };

            var target = d?.Target;
            var method = d?.Method;

            if (target is UnityEngine.Object uo)
            {
                e.TargetObject = uo;
                e.IsDestroyed = (uo == null);
                e.TargetName = uo ? uo.name : "(destroyed)";
            }
            else
            {
                e.TargetObject = null;
                e.TargetName = target?.ToString() ?? "(static)";
                e.IsDestroyed = false;
            }

            if (target is Component comp && comp)
            {
                e.ComponentType = comp.GetType().Name;
                e.ComponentIcon = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image;
                var go = comp.gameObject;
                e.SceneName = go.scene.IsValid() ? go.scene.name : "(no scene)";
                e.HierarchyPath = GetHierarchyPath(comp.transform);
            }
            else if (target is GameObject go && go)
            {
                e.ComponentType = go.GetType().Name;
                e.ComponentIcon = EditorGUIUtility.ObjectContent(go, go.GetType()).image;
                e.SceneName = go.scene.IsValid() ? go.scene.name : "(no scene)";
                e.HierarchyPath = GetHierarchyPath(go.transform);
            }
            else if (target is ScriptableObject so && so)
            {
                e.ComponentType = so.GetType().Name;
                e.ComponentIcon = EditorGUIUtility.ObjectContent(so, so.GetType()).image;
                e.SceneName = "(asset)";
                e.HierarchyPath = "(asset)";
            }
            else
            {
                e.ComponentType = method?.DeclaringType?.Name ?? "(type?)";
                e.ComponentIcon = null;
                e.SceneName = e.IsStatic ? "(static)" : "(n/a)";
                e.HierarchyPath = e.IsStatic ? "(static)" : "(n/a)";
            }

            return e;
        }
    }
}
#endif

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PaletteManagerWindow : EditorWindow
{
    private List<PaletteCategory> categories = new List<PaletteCategory>();
    private int selectedCategoryIndex = 0;
    private GameObject selectedPrefab = null;
    private GameObject previewInstance = null;
    private Vector2 scrollPosition;
    
    private Quaternion previewRotation = Quaternion.identity;
    
    // Índice del vértice del objeto nuevo que se usará para pegar (0 al 7)
    private int currentAnchorIndex = 0;

    [MenuItem("Tools/Paleta de Niveles")]
    public static void ShowWindow()
    {
        PaletteManagerWindow window = GetWindow<PaletteManagerWindow>("Paleta 3D");
        window.titleContent = new GUIContent("Paleta 3D");
    }

    private void OnEnable()
    {
        string[] guids = AssetDatabase.FindAssets("t:PaletteCategory");
        categories.Clear();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            categories.Add(AssetDatabase.LoadAssetAtPath<PaletteCategory>(path));
        }

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyPreview();
    }

    private void OnGUI()
    {
        if (categories.Count == 0)
        {
            EditorGUILayout.HelpBox("No se encontraron ScriptableObjects de tipo PaletteCategory.", MessageType.Info);
            return;
        }

        string[] categoryNames = categories.ConvertAll(c => c.categoryName).ToArray();
        selectedCategoryIndex = GUILayout.Toolbar(selectedCategoryIndex, categoryNames);

        EditorGUILayout.Space(10);

        PaletteCategory currentCategory = categories[selectedCategoryIndex];
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        int columns = Mathf.Max(1, Mathf.FloorToInt(position.width / 90f));
        int currentColumn = 0;

        EditorGUILayout.BeginHorizontal();
        GUIStyle centeredMiniLabel = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

        foreach (GameObject prefab in currentCategory.prefabs)
        {
            if (prefab == null) continue;

            Texture2D previewTex = AssetPreview.GetAssetPreview(prefab);

// Si Unity todavía está generando la preview en caché, devuelve null.
            if (previewTex == null)
            {
                // Verificamos si realmente se está procesando en segundo plano
                if (AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
                {
                    // Forzamos a la ventana a actualizarse en el próximo frame para capturar la imagen cuando termine
                    Repaint(); 
                }
    
                // Mientras carga (o si falla), usamos el ícono estándar miniatura del Prefab como fallback
                previewTex = AssetPreview.GetMiniThumbnail(prefab);
            }            Color originalColor = GUI.backgroundColor;
            if (selectedPrefab == prefab) GUI.backgroundColor = Color.cyan;

            EditorGUILayout.BeginVertical(GUILayout.Width(80), GUILayout.Height(100));
            
            if (GUILayout.Button(previewTex, GUILayout.Width(75), GUILayout.Height(75)))
            {
                if (selectedPrefab == prefab)
                {
                    selectedPrefab = null;
                    DestroyPreview();
                }
                else
                {
                    selectedPrefab = prefab;
                    CreatePreview();
                    currentAnchorIndex = 0; // Resetear el ancla al cambiar de objeto
                }
            }
            
            GUILayout.Label(prefab.name, centeredMiniLabel, GUILayout.Width(75));
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalColor;

            currentColumn++;
            if (currentColumn >= columns)
            {
                currentColumn = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        if (selectedPrefab != null)
        {
            EditorGUILayout.HelpBox(
                $"Activo: {selectedPrefab.name}\n" +
                "[Click Izq] Instanciar | [Rueda Mouse] Cambiar Asset\n" +
                "[Q] Cambiar Vértice Ancla | [R] Rotaciones\n" +
                "[Escape] Cancelar", MessageType.Info);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (selectedPrefab == null) return;

        // Esto evita que Unity seleccione otros objetos al hacer click en la nada
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        
        Event e = Event.current;

        // 1. Cambio de Vértice Ancla
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Q)
        {
            currentAnchorIndex = (currentAnchorIndex + 1) % 8;
            e.Use();
            return;
        }

        // 2. Cambio de Objeto con la Rueda
        if (e.type == EventType.ScrollWheel && !e.alt)
        {
            PaletteCategory cat = categories[selectedCategoryIndex];
            if (cat.prefabs.Count > 0)
            {
                int currentIndex = cat.prefabs.IndexOf(selectedPrefab);
                
                if (e.delta.y > 0) currentIndex = (currentIndex + 1) % cat.prefabs.Count;
                else currentIndex = (currentIndex - 1 + cat.prefabs.Count) % cat.prefabs.Count;

                selectedPrefab = cat.prefabs[currentIndex];
                CreatePreview();
                currentAnchorIndex = 0;
                e.Use(); 
                return;
            }
        }

        // 3. Rotación
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
        {
            if (e.shift) previewRotation *= Quaternion.Euler(90, 0, 0); 
            else if (e.control || e.command) previewRotation *= Quaternion.Euler(0, 0, 90);
            else previewRotation *= Quaternion.Euler(0, 90, 0); 

            if (previewInstance != null) previewInstance.transform.rotation = previewRotation;
            e.Use();
            return;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (previewInstance == null) CreatePreview();
            if (!previewInstance.activeSelf) previewInstance.SetActive(true);

            Bounds targetBounds = hit.collider.bounds;

            // Vértice del bloque existente más cercano al mouse
            Vector3 targetVertex = GetClosestBoundsVertex(targetBounds, hit.point);

            // Movemos la preview al punto de impacto para calcular su caja en ese espacio
            previewInstance.transform.position = hit.point;
            Bounds previewBounds = GetMaxBounds(previewInstance);

            // Obtenemos las 8 esquinas del objeto que vamos a colocar
            Vector3[] previewVertices = GetBoundsVertices(previewBounds);
            
            // Usamos la esquina seleccionada manualmente con la tecla Q
            Vector3 activePreviewVertex = previewVertices[currentAnchorIndex];

            // Compensación exacta
            Vector3 offset = targetVertex - activePreviewVertex;
            previewInstance.transform.position += offset;

            // --- SECCIÓN DE DIBUJADO DE GIZMOS ---
            
            Bounds finalBounds = GetMaxBounds(previewInstance);
            Vector3[] finalVertices = GetBoundsVertices(finalBounds);

            // Dibujamos un wireframe amarillo alrededor del objeto (Soluciona las caras vacías/transparentes)
            Handles.color = new Color(1f, 1f, 0f, 0.4f);
            Handles.DrawWireCube(finalBounds.center, finalBounds.size);

            // Esfera Cyan en el vértice objetivo del bloque existente
            Handles.color = Color.cyan;
            Handles.SphereHandleCap(0, targetVertex, Quaternion.identity, 0.15f, EventType.Repaint);

            // Esfera Magenta en el vértice "ancla" actual de la pieza que estás moviendo
            Handles.color = Color.magenta;
            Handles.SphereHandleCap(0, finalVertices[currentAnchorIndex], Quaternion.identity, 0.18f, EventType.Repaint);

            sceneView.Repaint();

            // 4. Lógica estricta de colocación (Solo Click Izquierdo sin la tecla ALT)
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.shift && !e.control)
            {
                GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                newObject.transform.position = previewInstance.transform.position;
                newObject.transform.rotation = previewInstance.transform.rotation;
                
                Undo.RegisterCreatedObjectUndo(newObject, "Colocar Asset Modular");
                e.Use();
            }
        }
        else
        {
            if (previewInstance != null && previewInstance.activeSelf) 
                previewInstance.SetActive(false);
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            selectedPrefab = null;
            DestroyPreview();
            Repaint();
        }
    }

    private void CreatePreview()
    {
        DestroyPreview();
        if (selectedPrefab == null) return;

        previewInstance = Instantiate(selectedPrefab);
        previewInstance.name = "--- Preview Palette ---";
        
        previewInstance.transform.rotation = previewRotation;
        
        foreach (var col in previewInstance.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    private void DestroyPreview()
    {
        if (previewInstance != null) DestroyImmediate(previewInstance);
    }

    private Bounds GetMaxBounds(GameObject g)
    {
        var renderers = g.GetComponentsInChildren<MeshRenderer>();
        if (renderers == null || renderers.Length == 0)
            return new Bounds(g.transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    private Vector3 GetClosestBoundsVertex(Bounds b, Vector3 point)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;
        
        float x = Mathf.Abs(point.x - min.x) < Mathf.Abs(point.x - max.x) ? min.x : max.x;
        float y = Mathf.Abs(point.y - min.y) < Mathf.Abs(point.y - max.y) ? min.y : max.y;
        float z = Mathf.Abs(point.z - min.z) < Mathf.Abs(point.z - max.z) ? min.z : max.z;
        
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Devuelve las 8 esquinas matemáticas de un Bounding Box en un orden predecible.
    /// </summary>
    private Vector3[] GetBoundsVertices(Bounds b)
    {
        return new Vector3[8] {
            new Vector3(b.min.x, b.min.y, b.min.z),
            new Vector3(b.max.x, b.min.y, b.min.z),
            new Vector3(b.min.x, b.max.y, b.min.z),
            new Vector3(b.max.x, b.max.y, b.min.z),
            new Vector3(b.min.x, b.min.y, b.max.z),
            new Vector3(b.max.x, b.min.y, b.max.z),
            new Vector3(b.min.x, b.max.y, b.max.z),
            new Vector3(b.max.x, b.max.y, b.max.z)
        };
    }
}
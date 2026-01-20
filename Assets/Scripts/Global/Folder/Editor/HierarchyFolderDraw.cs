using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class HierarchyFolderDraw
{
    // Cargamos el icono de carpeta de Unity
    private static readonly Texture2D FolderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;

    static HierarchyFolderDraw()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        // 1. ARREGLO DEL GLITCH:
        // Si el objeto está seleccionado, NO hacemos nada.
        // Esto evita que nuestro texto se dibuje encima del texto de Unity.
        if (Selection.Contains(instanceID)) return;

        var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        if (obj.TryGetComponent<HierarchyFolder>(out var folder))
        {
            // 2. CALCULAR EL ÁREA TOTAL (Tapando el icono original)
            // 'selectionRect' empieza después del icono. Movemos X hacia la izquierda
            // para cubrir el área donde estaría el icono de cubo por defecto.
            Rect fullRect = new Rect(selectionRect);
            fullRect.xMin -= 20f; // Desplazamos a la izquierda
            // fullRect.width no necesita ajuste si solo movemos el xMin

            // 3. DIBUJAR FONDO
            EditorGUI.DrawRect(fullRect, folder.folderColor);

            // 4. DIBUJAR EL NUEVO ICONO DE CARPETA
            if (FolderIcon != null)
            {
                Rect iconRect = new Rect(fullRect);
                iconRect.width = 16f;
                iconRect.height = 16f;
                iconRect.x = fullRect.x + 2f; // Lo posicionamos al inicio del nuevo rectángulo
                iconRect.y += 1f; // Pequeño ajuste vertical

                // Dibujamos el icono
                GUI.DrawTexture(iconRect, FolderIcon, ScaleMode.ScaleToFit);
            }

            // 5. DIBUJAR TEXTO
            string labelText = folder.upperCaseName ? obj.name.ToUpper() : obj.name;
            
            GUIStyle textStyle = new GUIStyle(EditorStyles.label);
            textStyle.normal.textColor = folder.textColor;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleLeft;

            Rect labelRect = new Rect(fullRect);
            // Empezamos el texto después del nuevo icono que acabamos de dibujar
            labelRect.xMin += 22f; 

            EditorGUI.LabelField(labelRect, labelText, textStyle);
        }
    }
}
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary> 
/// Procesador de Limpieza: Automatiza la eliminación de las carpetas durante el proceso de build, 
/// reparentando todos sus objetos hijos a la raíz o al padre superior para optimizar la escena final. 
/// </summary>
public class HierarchyFolderBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        FlattenFolders(scene);
    }
    
    private static void FlattenFolders(Scene scene)
    {
        List<HierarchyFolder> folders = new List<HierarchyFolder>();
        
        // Buscamos el componente en los objetos de la escena
        foreach (GameObject rootObj in scene.GetRootGameObjects())
        {
            folders.AddRange(rootObj.GetComponentsInChildren<HierarchyFolder>(true));
        }

        // Procesamos de atrás hacia adelante para no romper la jerarquía al destruir
        for (int i = folders.Count - 1; i >= 0; i--)
        {
            HierarchyFolder folder = folders[i];
            
            if (folder == null) continue;

            Transform folderTrans = folder.transform;
            Transform parentTrans = folderTrans.parent;

            int childCount = folderTrans.childCount;
            
            var children = new Transform[childCount];
            for (int c = 0; c < childCount; c++) children[c] = folderTrans.GetChild(c);

            foreach (var child in children)
            {
                // Mantenemos la posición global al reparentar
                child.SetParent(parentTrans, true); 
            }

            // Destrucción inmediata ya que estamos en proceso de build
            Object.DestroyImmediate(folder.gameObject);
        }
    }
}
#endif
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Se ejecuta automáticamente al compilar (Build)
public class HierarchyFolderBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // Ejecutamos la limpieza
        FlattenFolders(scene);
    }

    // OPCIONAL: Descomenta esto si quieres ver el efecto también al dar PLAY en el editor
    /*
    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.playModeStateChanged += (state) =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Nota: Esto modifica la escena temporalmente. Unity suele revertirlo al salir de Play,
                // pero úsalo con precaución o solo confía en el BuildProcessor de arriba.
            }
        };
    }
    */

    private static void FlattenFolders(Scene scene)
    {
        // Buscamos todas las carpetas en la escena actual (incluso las desactivadas)
        List<HierarchyFolder> folders = new List<HierarchyFolder>();
        
        // Forma segura de buscar en la escena, incluyendo objetos raíz e hijos
        foreach (GameObject rootObj in scene.GetRootGameObjects())
        {
            folders.AddRange(rootObj.GetComponentsInChildren<HierarchyFolder>(true));
        }

        // Las procesamos de abajo hacia arriba para soportar carpetas dentro de carpetas
        for (int i = folders.Count - 1; i >= 0; i--)
        {
            HierarchyFolder folder = folders[i];
            
            // Si la carpeta ya fue borrada (por estar dentro de otra), saltar
            if (folder == null) continue;

            Transform folderTrans = folder.transform;
            Transform parentTrans = folderTrans.parent;

            // Movemos los hijos
            int childCount = folderTrans.childCount;
            
            // Recorremos los hijos al revés para mantener el orden al reparentar
            // O podemos usar GetChild(0) repetidamente.
            // La forma más segura para conservar índices:
            
            var children = new Transform[childCount];
            for (int c = 0; c < childCount; c++) children[c] = folderTrans.GetChild(c);

            // Desvinculamos y reasignamos padre
            foreach (var child in children)
            {
                if (parentTrans != null)
                {
                    child.SetParent(parentTrans, true); // Mantiene posición global
                }
                else
                {
                    child.SetParent(null, true); // Se va a la raíz de la escena
                }
            }

            // Finalmente, borramos la carpeta vacía
            Object.DestroyImmediate(folder.gameObject);
        }
    }
}
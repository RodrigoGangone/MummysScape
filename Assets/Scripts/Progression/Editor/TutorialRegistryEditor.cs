using UnityEditor;
using System.IO;
using System.Text;
using UnityEngine;

[CustomEditor(typeof(TutorialRegistry))]
public class TutorialRegistryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generar Enum de Tutoriales", GUILayout.Height(30)))
        {
            GenerateEnum((TutorialRegistry)target);
        }
    }

    private void GenerateEnum(TutorialRegistry registry)
    {
        string filePath = "Assets/Scripts/Progression/TutorialID.cs";
        
        // Aseguramos que la carpeta exista
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERADO AUTOMATICAMENTE - NO EDITAR");
        sb.AppendLine("public enum TutorialID");
        sb.AppendLine("{");
        sb.AppendLine("    None = 0,");

        foreach (var id in registry.tutorialIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            string cleanId = id.Replace(" ", "_");
            sb.AppendLine($"    {cleanId},");
        }

        sb.AppendLine("}");

        File.WriteAllText(filePath, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>TutorialID Enum</b> generado con éxito.</color>");
    }
}
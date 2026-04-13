using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public static class AudioConstantsGenerator
{
    // Ruta donde se guardarán los scripts generados (Respetando tu estructura)
    private const string GENERATED_PATH = "Assets/Scripts/Audio/Utils";

    [MenuItem("Tools/Mummy Tools/Generate All Audio IDs")]
    public static void GenerateAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:FxBank");
        List<FxBank> allBanks = new List<FxBank>();

        foreach (string guid in guids)
            
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            allBanks.Add(AssetDatabase.LoadAssetAtPath<FxBank>(path));
        }

        var groupedByBus = allBanks.GroupBy(b => b.bus);

        if (!Directory.Exists(GENERATED_PATH))
            Directory.CreateDirectory(GENERATED_PATH);

        foreach (var group in groupedByBus)
        {
            GenerateScriptForBus(group.Key, group.ToList());
        }

        AssetDatabase.Refresh();
        Debug.Log("<color=cyan><b>[AudioGenerator]</b> ¡Todos los IDs de audio han sido centralizados!</color>");
    }

    private static void GenerateScriptForBus(AudioBus bus, List<FxBank> banks)
    {
        string className = $"{bus}IDs";
        string filePath = Path.Combine(GENERATED_PATH, className + ".cs");

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// GENERADO AUTOMATICAMENTE - NO EDITAR");
        sb.AppendLine("public static class " + className);
        sb.AppendLine("{");

        foreach (var bank in banks)
        {
            // Limpieza del nombre del banco para la subclase
            string subClassName = bank.name.Replace(" ", "_").Replace("-", "_");
            sb.AppendLine($"    public static class {subClassName}");
            sb.AppendLine("    {");

            foreach (var entry in bank.entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;

                // Limpieza del nombre de la constante
                string varName = entry.key.Replace(" ", "_").Replace("-", "_");

                // --- SOLUCIÓN AL ERROR CS0542 ---
                // Si la constante se llama igual que la clase, le agregamos un sufijo [_Key] para evitar el error.
                if (varName.Equals(subClassName, System.StringComparison.OrdinalIgnoreCase))
                {
                    varName += "_Key";
                }

                sb.AppendLine($"        public const string {varName} = \"{entry.key}\";");
            }

            sb.AppendLine("    }");
            sb.AppendLine("");
        }

        sb.AppendLine("}");

        File.WriteAllText(filePath, sb.ToString());
    }
}
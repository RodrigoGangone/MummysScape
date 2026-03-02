using UnityEngine;

/// <summary> 
/// Componente de Organización: Define un objeto como "carpeta" visual en la jerarquía, permitiendo 
/// personalizar colores de fondo, texto y formato de nombre para mejorar la navegación en el editor. 
/// </summary>

[AddComponentMenu("Layout/Hierarchy Folder")]
public class HierarchyFolder : MonoBehaviour
{
    public Color folderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    public Color textColor = Color.white;
    
    public bool upperCaseName = true;
}
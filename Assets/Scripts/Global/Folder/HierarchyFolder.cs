using UnityEngine;

[AddComponentMenu("Layout/Hierarchy Folder")]
public class HierarchyFolder : MonoBehaviour
{
    // Color de fondo en la jerarquía
    public Color folderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    
    // Color del texto
    public Color textColor = Color.white;
    
    // Opción para poner el texto en mayúsculas automáticamente
    public bool upperCaseName = true;
}
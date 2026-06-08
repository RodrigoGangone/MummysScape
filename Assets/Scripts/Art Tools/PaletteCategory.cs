using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NuevaCategoria", menuName = "Paleta/Categoría")]
public class PaletteCategory : ScriptableObject
{
    public string categoryName;
    public List<GameObject> prefabs = new List<GameObject>();
}
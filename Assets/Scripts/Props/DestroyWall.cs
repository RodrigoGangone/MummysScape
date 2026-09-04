using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class BrickLayer
{
    [Tooltip("Arrastra aquí los Rigidbody de los ladrillos que forman este grupo/capa.")]
    public Rigidbody[] bricks;
}

public class DestroyWall : MonoBehaviour
{
    [Header("Estructura de la Pared")]
    [Tooltip("Define el orden de destrucción. El Elemento 0 saldrá volando en el primer golpe.")]
    [SerializeField] private BrickLayer[] destructionLayers;

    [Header("Físicas del Impacto")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDepthOffset = 0.5f;
    
    [Tooltip("Fuerza adicional hacia arriba usada en BasicActivate para el efecto de expansión.")]
    [SerializeField] private float basicUpwardModifier = 2f;

    [Header("Desvanecimiento")]
    [Tooltip("Tiempo en el que el Alpha pasará de 1 a 0 antes de desactivar el objeto.")]
    [SerializeField] private float timeToFade = 3f;

    private int _currentLayerIndex = 0;

    /// <summary>
    /// Activa la explosión usando un punto exacto de impacto y una dirección.
    /// </summary>
    public void Activate(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_currentLayerIndex >= destructionLayers.Length) return;

        Vector3 explosionPosition = hitPoint - (hitDirection.normalized * explosionDepthOffset);
        
        // Llamamos al método centralizado (sin modificador hacia arriba para respetar la dirección del golpe)
        ProcessLayerExplosion(explosionPosition, 0f);
    }

    /// <summary>
    /// Activa una explosión genérica desde el centro del objeto. 
    /// Los ladrillos salen disparados hacia arriba y hacia los lados.
    /// </summary>
    public void BasicActivate()
    {
        if (_currentLayerIndex >= destructionLayers.Length) return;

        // Tomamos la posición central del script (la pared) como epicentro
        Vector3 explosionPosition = transform.position;
        
        // Llamamos al método centralizado aplicando el modificador hacia arriba
        ProcessLayerExplosion(explosionPosition, basicUpwardModifier);
    }

    /// <summary>
    /// Método centralizado que maneja la física y el desvanecimiento para no repetir código.
    /// </summary>
    private void ProcessLayerExplosion(Vector3 explosionPosition, float upwardModifier)
    {
        Rigidbody[] currentBricks = destructionLayers[_currentLayerIndex].bricks;

        foreach (Rigidbody rb in currentBricks)
        {
            if (rb == null) continue;

            rb.isKinematic = false;
            
            // Aplicamos la fuerza. El 'upwardModifier' eleva los objetos mientras se expanden.
            rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardModifier);
        }

        // Verificamos si esta es la última capa de la pared
        bool isLastLayer = (_currentLayerIndex == destructionLayers.Length - 1);

        // Iniciamos la corrutina para desvanecer y apagar estos ladrillos
        StartCoroutine(FadeAndDeactivate(currentBricks, timeToFade, isLastLayer));

        _currentLayerIndex++;
    }

    private IEnumerator FadeAndDeactivate(Rigidbody[] bricks, float duration, bool isLastLayer)
    {
        List<Material> materials = new List<Material>();
        List<GameObject> objects = new List<GameObject>();

        foreach (Rigidbody rb in bricks)
        {
            if (rb == null) continue;
            
            objects.Add(rb.gameObject);
            
            Renderer rend = rb.GetComponent<Renderer>();
            if (rend != null)
            {
                materials.Add(rend.material);
            }
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    mat.SetFloat("_Alpha", currentAlpha);
                }
            }

            yield return null; 
        }

        foreach (Material mat in materials)
        {
            if (mat != null) mat.SetFloat("_Alpha", 0f);
        }

        foreach (GameObject obj in objects)
        {
            if (obj != null) obj.SetActive(false);
        }

        if (isLastLayer)
        {
            gameObject.SetActive(false);
        }
    }
}
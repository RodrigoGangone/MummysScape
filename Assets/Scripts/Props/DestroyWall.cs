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

    [Header("Desvanecimiento")]
    [Tooltip("Tiempo en el que el Alpha pasará de 1 a 0 antes de desactivar el objeto.")]
    [SerializeField] private float timeToFade = 3f;

    private int _currentLayerIndex = 0;

    public void Activate(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_currentLayerIndex >= destructionLayers.Length) return;

        Vector3 explosionPosition = hitPoint - (hitDirection.normalized * explosionDepthOffset);
        Rigidbody[] currentBricks = destructionLayers[_currentLayerIndex].bricks;

        foreach (Rigidbody rb in currentBricks)
        {
            if (rb == null) continue;

            rb.isKinematic = false;
            rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
        }

        // Verificamos si esta es la última capa de la pared
        bool isLastLayer = (_currentLayerIndex == destructionLayers.Length - 1);

        // Iniciamos la corrutina para desvanecer y apagar estos ladrillos
        StartCoroutine(FadeAndDeactivate(currentBricks, timeToFade, isLastLayer));

        _currentLayerIndex++;
    }

    private IEnumerator FadeAndDeactivate(Rigidbody[] bricks, float duration, bool isLastLayer)
    {
        // Recolectamos los materiales y GameObjects al inicio para no usar GetComponent en cada frame (optimización)
        List<Material> materials = new List<Material>();
        List<GameObject> objects = new List<GameObject>();

        foreach (Rigidbody rb in bricks)
        {
            if (rb == null) continue;
            
            objects.Add(rb.gameObject);
            
            Renderer rend = rb.GetComponent<Renderer>();
            if (rend != null)
            {
                // Al llamar .material en Unity, se crea una instancia independiente del material.
                // Esto es ideal para que solo se desvanezcan estos ladrillos y no todos los de la escena.
                materials.Add(rend.material);
            }
        }

        float elapsedTime = 0f;

        // Bucle de desvanecimiento
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // Interpolar linealmente entre 1 y 0
            float currentAlpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    mat.SetFloat("_Alpha", currentAlpha);
                }
            }

            yield return null; // Esperamos al siguiente frame
        }

        // Aseguramos que el valor termine exactamente en 0
        foreach (Material mat in materials)
        {
            if (mat != null) mat.SetFloat("_Alpha", 0f);
        }

        // Desactivamos los GameObjects de los ladrillos en lugar de destruirlos
        foreach (GameObject obj in objects)
        {
            if (obj != null) obj.SetActive(false);
        }

        // Si esta fue la última capa en destruirse, apagamos todo el objeto padre
        if (isLastLayer)
        {
            gameObject.SetActive(false);
        }
    }
}
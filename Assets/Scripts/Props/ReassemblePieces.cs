using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary> 
/// Efecto de Reensamblaje: Reconstruye visualmente objetos destruidos moviendo sus piezas 
/// de vuelta a sus coordenadas locales originales mediante interpolación suave. 
/// </summary>
public class ReassemblePieces : MonoBehaviour
{
    private struct PieceData
    {
        public Transform transform;
        public Vector3 initialLocalPos;
        public Quaternion initialLocalRot;
    }

    [Header("Timing")]
    public float WaitBeforeFlyBack = 2f; 
    public float AssembleDuration = 1.0f; 

    private List<PieceData> pieces = new List<PieceData>();

    void Awake()
    {
        // Buscamos todos los MeshRenderers en los hijos para identificar las piezas
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in renderers)
        {
            pieces.Add(new PieceData {
                transform = mr.transform,
                initialLocalPos = mr.transform.localPosition,
                initialLocalRot = mr.transform.localRotation
            });
        }
    }

    [ContextMenu("Start Reassembling")] // Permite probarlo desde el Inspector (clic derecho en el script)
    public void StartReassembling()
    {
        StartCoroutine(ReassembleRoutine());
    }

    private IEnumerator ReassembleRoutine()
    {
        yield return new WaitForSeconds(WaitBeforeFlyBack);

        // Listas temporales para guardar dónde quedó cada pieza después de la explosión/caída
        Vector3[] startPositions = new Vector3[pieces.Count];
        Quaternion[] startRotations = new Quaternion[pieces.Count];

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].transform == null) continue;

            if (pieces[i].transform.TryGetComponent<Rigidbody>(out var rb))
            {
                // CORRECCIÓN: Resetear velocidad ANTES de hacer el objeto Kinematic
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; 
            }

            // Guardamos el estado actual para que el Lerp sea suave y lineal
            startPositions[i] = pieces[i].transform.localPosition;
            startRotations[i] = pieces[i].transform.localRotation;
        }

        float elapsed = 0;
        while (elapsed < AssembleDuration)
        {
            elapsed += Time.deltaTime;
            // t va de 0 a 1
            float t = elapsed / AssembleDuration;
            // Aplicamos suavizado (aceleración al inicio y frenado al final)
            float smoothT = Mathf.SmoothStep(0, 1, t); 

            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].transform == null) continue;

                // Movemos de la posición de "caída" a la posición "original"
                pieces[i].transform.localPosition = Vector3.Lerp(startPositions[i], pieces[i].initialLocalPos, smoothT);
                pieces[i].transform.localRotation = Quaternion.Slerp(startRotations[i], pieces[i].initialLocalRot, smoothT);
            }
            yield return null;
        }

        // Aseguramos que lleguen exactamente a la posición final
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].transform == null) continue;
            pieces[i].transform.localPosition = pieces[i].initialLocalPos;
            pieces[i].transform.localRotation = pieces[i].initialLocalRot;
        }
    }
}
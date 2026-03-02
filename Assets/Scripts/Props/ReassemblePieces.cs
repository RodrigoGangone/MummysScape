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

    public void StartReassembling()
    {
        StartCoroutine(ReassembleRoutine());
    }

    private IEnumerator ReassembleRoutine()
    {
        yield return new WaitForSeconds(WaitBeforeFlyBack);

        foreach (var piece in pieces)
        {
            if (piece.transform.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true; 
                rb.velocity = Vector3.zero;
            }
        }

        float elapsed = 0;
        while (elapsed < AssembleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / AssembleDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t); 

            foreach (var piece in pieces)
            {
                if (piece.transform == null) continue;
                piece.transform.localPosition = Vector3.Lerp(piece.transform.localPosition, piece.initialLocalPos, smoothT);
                piece.transform.localRotation = Quaternion.Slerp(piece.transform.localRotation, piece.initialLocalRot, smoothT);
            }
            yield return null;
        }
    }
}
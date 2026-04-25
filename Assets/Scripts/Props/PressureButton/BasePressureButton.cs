using UnityEngine;
using UnityEngine;
using System.Linq;
using static Tags;
using static PlayerEnum.PlayerSize;

public abstract class BasePressureButton : MonoBehaviour
{
    [Header("Base Detection Settings")]
    [SerializeField] protected LayerMask detectionLayer;
    [SerializeField] protected Vector3 boxSize = new Vector3(0.8f, 0.2f, 0.8f);
    [SerializeField] protected float checkDistance = 0.5f;

    protected bool isOccupied;

    protected virtual void FixedUpdate()
    {
        bool currentlyOccupied = CheckOccupancy();

        if (currentlyOccupied && !isOccupied)
        {
            isOccupied = true;
            OnPress();
        }
        else if (!currentlyOccupied && isOccupied)
        {
            isOccupied = false;
            OnRelease();
        }
    }

    private bool CheckOccupancy()
    {
        // Simplificado: BoxCast y validación inmediata
        RaycastHit[] hits = Physics.BoxCastAll(transform.position, boxSize / 2, Vector3.up, Quaternion.identity, checkDistance, detectionLayer);

        foreach (var hit in hits)
        {
            // 1. Es una caja
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer(BOX_TAG)) return true;

            // 2. Es la Momia en tamaño Normal
            if (hit.collider.CompareTag(PLAYER_TAG))
            {
                var mummy = hit.collider.GetComponent<PlayerController>();
                if (mummy != null && mummy.Ctx.Model.Size == Normal) return true;
            }
        }
        return false;
    }

    // Métodos que cada tipo de botón implementará a su gusto
    protected abstract void OnPress();
    protected abstract void OnRelease();

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * checkDistance, boxSize);
    }
}
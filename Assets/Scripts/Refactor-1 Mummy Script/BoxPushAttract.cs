using UnityEngine;

/// <summary>
/// BoxPushAttract
/// Implementa IPushable para una caja (RB + BoxCollider) alineada a los ejes locales del prefab.
/// - Decide el eje permitido según la CARA donde empuja el player: cara ±X => eje local Z; cara ±Z => eje local X.
/// - Mueve la caja sólo sobre ese eje usando Rigidbody.MovePosition y bloquea con Physics.BoxCast (skin configurable).
/// - Entrega SnapPoint = centro horizontal de la cara, para centrar al player (soft-snap) mientras dura el push.
/// </summary>
public sealed class BoxPushAttract : MonoBehaviour
{
  
}
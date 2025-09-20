using UnityEngine;

/// <summary>
/// InteractionRuntime
/// Rehace el chequeo de PUSH con doble raycast ("brazos").
/// - Lanza 2 rayos frontales a mitad de altura del player, separados 1 unidad (±0.5 en local right).
/// - Valida que ambos rayos golpeen: (a) la MISMA caja (collider raíz) y (b) la MISMA CARA (normales ~iguales).
/// - Si hay BoxPushAttract/IPushable, delega el armado del PushInfo.
/// - Dibuja gizmos: ROJO = no válido, VERDE = raycast toca un BoxPushAttract en layer Interactable.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionRuntime : MonoBehaviour
{

}
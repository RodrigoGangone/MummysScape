using System;
using UnityEngine;

/// <summary>Datos necesarios para representar en UI una gema recién recogida.</summary>
[Serializable]
public readonly struct GemPickupData
{
    public int GemNumber { get; }
    public Vector3 WorldPosition { get; }

    public GemPickupData(int gemNumber, Vector3 worldPosition)
    {
        GemNumber = gemNumber;
        WorldPosition = worldPosition;
    }
}

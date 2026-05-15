// Base para cualquier efecto (Partículas, Sonidos, Animaciones, Shakes)

using UnityEngine;

public abstract class FeedbackAction : ScriptableObject
{
    public abstract void Play(PlayerContext ctx);
}

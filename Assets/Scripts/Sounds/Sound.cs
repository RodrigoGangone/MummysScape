using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    public bool loop;
}

public enum NameSounds
{
    Theme,
    Explosion,
    Jump,
    Shoot,
    GameOver
    // Agrega más nombres de sonidos según sea necesario
}
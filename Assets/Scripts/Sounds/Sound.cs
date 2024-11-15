using UnityEngine;

[System.Serializable]
public class Sound
{
    public NameSounds name;
    public AudioClip clip;
    public bool loop;
}

public enum NameSounds
{
    ActivateInteractable,
    BandageBlow,
    BandageShoot1,
    BandageShoot2,
    BandageShoot3,
    Collectable,
    Click,
    MovingBox,
    MovingPlatform,
    MainMenu1,
    MainMenu2,
    Lvl1_1,
    Walk,
    WalkInSand,
    WrapBox
}
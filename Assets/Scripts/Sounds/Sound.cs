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
    SFX_ActivateInteractable,
    SFX_BandageBlow,
    SFX_BandageShoot1,
    SFX_BandageShoot2,
    SFX_Collectable,
    SFX_Click,
    SFX_Hook,
    SFX_MovingBox,
    SFX_MovingPlatform,
    SFX_Portal,
    SFX_PoofSmoke,
    SFX_Walk,
    SFX_WalkInSand,
    SFX_WrapBox,
    Music_MainMenu1,
    Music_MainMenu2,
    Music_Lvl1_1,
    Music_Win,
    Music_Lose,
}
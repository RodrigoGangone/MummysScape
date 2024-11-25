using UnityEngine;

[System.Serializable]
public class Sound
{
    [Header("Sound Settings")]
    public NameSounds name;
    public AudioClip clip;
    public bool loop;
    
    [Header("3D Sound Settings")]
    [SerializeField] public float minDistance = 1f;
    [SerializeField] public float maxDistance = 10f;
}

public enum NameSounds
{
    SFX_ActivateInteractable,
    SFX_BandageBlow,
    SFX_BandageShoot1,
    SFX_BandageShoot2,
    SFX_BreakHourglass,
    SFX_BreakJar,
    SFX_Collectable,
    SFX_Click,
    SFX_Fire,
    SFX_HeartBeat_1,
    SFX_HeartBeat_2,
    SFX_Hook,
    SFX_MovingBox,
    SFX_MovingPlatform,
    SFX_Portal,
    SFX_PoofSmoke,
    SFX_MummyDeathNormal,
    SFX_MummyDeathSmall,
    SFX_MummyDeathSkull,
    SFX_MummyWalkNormal,
    SFX_MummyWalkSmall,
    SFX_MummyWalkHead,
    SFX_MummyWalkSandNormal,
    SFX_MummyWalkSandSmall,
    SFX_MummyWalkSandHead,
    SFX_WrapBox,
    Music_MainMenu1,
    Music_MainMenu2,
    Music_Lvl1_1,
    Music_Win,
    Music_Lose
}
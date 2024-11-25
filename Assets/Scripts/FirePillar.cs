using UnityEngine;

public class FirePillar : MonoBehaviour
{
    private AudioSource _fireAudio;

    private void Start()
    {
        _fireAudio = AudioManager.Instance.Play3DSFX(NameSounds.SFX_Fire, transform);
    }

    private void OnDisable()
    {
        _fireAudio.Stop();
        AudioSourceFactory.Instance.ReturnAudioSourceToPool(_fireAudio);
    }
}

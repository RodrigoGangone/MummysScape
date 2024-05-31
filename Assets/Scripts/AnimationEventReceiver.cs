using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private Player _player;

    private void Start()
    {
        _player = GameObject.Find("Mummy").GetComponent<Player>();
    }

    public void EVENT_ANIM_SHOOT()
    {
        _player._modelPlayer.Shoot();
    }
}
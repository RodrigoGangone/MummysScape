using UnityEditor.Animations;
using UnityEngine;

public class ViewPlayer
{
    Player _player;
    private Animator[] _animatorController;

    public Material hookMaterial;

    public ViewPlayer(Player p)
    {
        _player = p;
        hookMaterial = _player._bandage.GetComponent<LineRenderer>().material;
    }

    public void PLAY_PUFF()
    {
        _player._puffFX.Play();
    }

    public void PLAY_ANIM(string anim)
    {
        _animatorController = _player.GetComponentsInChildren<Animator>();

        foreach (var controller in _animatorController)
        {
            controller.SetTrigger(anim);
        }
    }

    // ───────▄▀▀▀▀▀▀▀▀▀▀▄▄
    // ────▄▀▀░░░░░░░░░░░░░▀▄
    // ──▄▀░░░░░░░░░░░░░░░░░░▀▄
    // ──█░░░░░░░░░░░░░░░░░░░░░▀▄
    // ─▐▌░░░░░░░░▄▄▄▄▄▄▄░░░░░░░▐▌
    // ─█░░░░░░░░░░░▄▄▄▄░░▀▀▀▀▀░░█
    // ▐▌░░░░░░░▀▀▀▀░░░░░▀▀▀▀▀░░░▐▌
    // █░░░░░░░░░▄▄▀▀▀▀▀░░░░▀▀▀▀▄░█
    // █░░░░░░░░░░░░░░░░▀░░░▐░░░░░▐▌
    // ▐▌░░░░░░░░░▐▀▀██▄░░░░░░▄▄▄░▐▌
    // ─█░░░░░░░░░░░▀▀▀░░░░░░▀▀██░░█
    // ─▐▌░░░░▄░░░░░░░░░░░░░▌░░░░░░█
    // ──▐▌░░▐░░░░░░░░░░░░░░▀▄░░░░░█
    // ───█░░░▌░░░░░░░░▐▀░░░░▄▀░░░▐▌
    // ───▐▌░░▀▄░░░░░░░░▀░▀░▀▀░░░▄▀
    // ───▐▌░░▐▀▄░░░░░░░░░░░░░░░░█
    // ───▐▌░░░▌░▀▄░░░░▀▀▀▀▀▀░░░█
    // ───█░░░▀░░░░▀▄░░░░░░░░░░▄▀
    // ──▐▌░░░░░░░░░░▀▄░░░░░░▄▀
    // ─▄▀░░░▄▀░░░░░░░░▀▀▀▀█▀
    // ▀░░░▄▀░░░░░░░░░░▀░░░▀▀▀▀▄▄▄▄▄
}
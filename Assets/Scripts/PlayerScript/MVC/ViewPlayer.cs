using UnityEditor.Animations;
using UnityEngine;

public class ViewPlayer
{
    Player _player;
    public Animator _animatorController;
    public Material hookMaterial;

    public ViewPlayer(Player p)
    {
        _player = p;
        _animatorController = _player._anim;
        hookMaterial = _player._bandage.GetComponent<LineRenderer>().material;
    }

    public void PLAY_PUFF()
    {
        _player._puffFX.Play();
    }

    public void PLAY_ANIM(string anim)
    {
        _animatorController.SetTrigger(anim);
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
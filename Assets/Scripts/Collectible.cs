using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    private LevelManager _levelManager;
    [SerializeField] private ParticleSystem _addPlayer;
    [SerializeField] private GameObject _FXShinning;
    [SerializeField] private GameObject _view;

    private void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<Player>()) return;

        _levelManager.AddCollectible(this);

        _addPlayer.Play();

        _view.SetActive(false);
        _FXShinning.SetActive(false);
        Destroy(gameObject, 3f);
    }
}
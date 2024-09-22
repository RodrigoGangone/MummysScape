using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private GameObject _congratsParticles;

    [SerializeField] private CollectibleNumber _collectibleNumber;

    private void Start()
    {
        _levelManager = FindObjectOfType<LevelManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerFather")) return;

        _levelManager.CollectibleCount(1, _collectibleNumber);

        Instantiate(_congratsParticles, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}

public enum CollectibleNumber
{
    One,
    Two,
    Three
}
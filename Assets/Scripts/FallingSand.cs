using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.UI;
using UnityEngine.UIElements;

public class FallingSand : MonoBehaviour
{
    private Player _player;
    private LevelManager _levelManager;
    [SerializeField] private Material _material;

    private float value;
    private const string TOP_TRESHOLD = "_TopThreshold";


    [SerializeField] private float speed = 1;
    [SerializeField] private float timeToIncrease;
    [SerializeField] private float timeToDecrease;

    private void Start()
    {
        _player = FindObjectOfType<Player>();
        _material = GetComponent<Renderer>().material;

        _levelManager = FindObjectOfType<LevelManager>();

        StartCoroutine(HandleValue());
    }

    //TODO: CUANDO GOLPEA AL PLAYER, TIENE QUE SACARLO DEL ESTADO DE HOOK
    //TODO: HAY QUE HACER POR UPDATE //O CORRUTINA// UN SINE PARA QUE LA VARIABLE VAYA DE 0 A 1.
    //TODO: SI TRIGGEREA AL PLAYER CUANDO ESTA EN 1, TIENE QUE TIRARLO DEL HOOK [CHANGE STATE A FALL]

    IEnumerator HandleValue()
    {
        while (true)
        {
            yield return StartCoroutine(IncreaseValue());

            yield return new WaitForSeconds(timeToDecrease);

            yield return StartCoroutine(DecreaseValue());

            yield return new WaitForSeconds(timeToIncrease);
        }
    }

    IEnumerator IncreaseValue()
    {
        value = 0;

        while (value < 1f)
        {
            value += Time.deltaTime * speed;
            value = Mathf.Clamp01(value);
            _material.SetFloat(TOP_TRESHOLD, value);
            yield return null;
        }
    }

    IEnumerator DecreaseValue()
    {
        while (value > 0f)
        {
            value -= Time.deltaTime * speed;
            value = Mathf.Clamp01(value);
            _material.SetFloat(TOP_TRESHOLD, value);
            yield return null;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.gameObject.CompareTag("PlayerFather") || value > 0.4f)
        {
            _player.HitFalling = false;
        }
        else
        {
            _levelManager.OnPlayerFall.Invoke();
            _player.HitFalling = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _player.HitFalling = false;
    }
}
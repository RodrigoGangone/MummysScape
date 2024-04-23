using System;
using UnityEngine;

public class BandageBullet : MonoBehaviour
{
    private ModelPlayer _model;
    private Player _player;
    public ViewPlayer _view;

    private GameObject indicatorObject;

    private void Start()
    {
        indicatorObject = GameObject.Find("IndicatorSkill");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground")) 
        { 
            //_view.IndicatorAimOff();
            indicatorObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}

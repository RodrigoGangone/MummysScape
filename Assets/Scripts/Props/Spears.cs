using UnityEngine;
using UnityEngine.Serialization;

public class Spears : MonoBehaviour
{
    private Animator _animatorController;
    private BoxCollider _collider;
    
    private void Start()
    {
        _animatorController = GetComponent<Animator>();
        _collider = GetComponent<BoxCollider>();
    }

    public void Up()
    {
        _animatorController.SetBool("Up", true);
        _animatorController.SetBool("Down", false);
        
        _collider.enabled = true;
    }

    public void Down()
    {
        _animatorController.SetBool("Up", false);
        _animatorController.SetBool("Down", true);
        _collider.enabled = false;
    }
}
using UnityEngine;

public class Spears : MonoBehaviour
{
    Animator animatorController;

    private void Start() => animatorController = GetComponent<Animator>();

    private void Up() => animatorController.SetTrigger("Up");

    private void Down() => animatorController.SetTrigger("Down");
}
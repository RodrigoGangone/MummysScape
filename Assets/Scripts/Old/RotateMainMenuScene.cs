using UnityEngine;

public class RotateMainMenuScene : MonoBehaviour
{
    [SerializeField] private float speed = 1f; 
    
    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}

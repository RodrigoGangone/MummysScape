using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateObjectsBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platformsAll;
    [SerializeField] private ParticleSystem _shiningParticles;
    private Animator _animator;
    Material material;
    private InteractableOutline _interactableOutline;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        material = gameObject.GetComponentInChildren<Renderer>().material;
        _interactableOutline = GetComponent<InteractableOutline>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Bullet")) return;

        _animator.SetBool("IsActive", !_animator.GetBool("IsActive"));

        StartCoroutine(SineIntensity());

        foreach (GameObject platform in _platformsAll)
        {
            MoveHorizontalPlatform moveHorizontalPlatform = platform.GetComponent<MoveHorizontalPlatform>();
            MoveVerticalPlatform moveVerticalPlatform = platform.GetComponent<MoveVerticalPlatform>();
            
            if (moveHorizontalPlatform != null)
                moveHorizontalPlatform.StartAction();
            
            if(moveVerticalPlatform != null)  
                moveVerticalPlatform.StartAction();
        }
    }

    IEnumerator SineIntensity()
    {
        yield return StartCoroutine(ChangeIntensity(0f, 1f, 0.8f));
        yield return StartCoroutine(ChangeIntensity(1f, 0f, 0.8f));
    }

    IEnumerator ChangeIntensity(float startValue, float endValue, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float intensity = Mathf.Lerp(startValue, endValue, elapsedTime / duration);
            material.SetFloat("_intensity", intensity);
            yield return null;
        }

        material.SetFloat("_intensity", endValue);
    }
    
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateObjectsBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platformsAll;
    private Animator _animator;
    private Material _material;
    private BoxCollider _boxCollider;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _material = gameObject.GetComponentInChildren<Renderer>().material;
        _boxCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Bullet")) return;

        
        _boxCollider.enabled = false;
        AudioManager.Instance.PlaySFX(NameSounds.ActivateInteractable);
        _animator.SetBool("IsActive", !_animator.GetBool("IsActive"));

        StartCoroutine(SineIntensity());

        foreach (GameObject platform in _platformsAll)
        {
            MoveHorizontalPlatform moveHorizontalPlatform = platform.GetComponent<MoveHorizontalPlatform>();
            MoveVerticalPlatform moveVerticalPlatform = platform.GetComponent<MoveVerticalPlatform>();

            if (moveHorizontalPlatform != null)
                moveHorizontalPlatform.StartAction();

            if (moveVerticalPlatform != null)
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
            _material.SetFloat("_intensity", intensity);
            yield return null;
        }

        _material.SetFloat("_intensity", endValue);
    }
}
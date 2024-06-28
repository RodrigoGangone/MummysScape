using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateObjectsBullet : MonoBehaviour
{
    [SerializeField] private List<GameObject> _platformsAll;
    [SerializeField] private ParticleSystem _sparkles;
    Material material;

    private void Start()
    {
        material = gameObject.GetComponent<Renderer>().material;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Bullet")) return;

        _sparkles.Play();
        StartCoroutine(SineIntensity());

        foreach (GameObject platform in _platformsAll)
        {
            MovePlatform movePlatform = platform.GetComponent<MovePlatform>();

            if (movePlatform != null)
                movePlatform.StartAction();

            //Quicksand quicksand = platform.GetComponent<Quicksand>();

            /*if (quicksand != null)
                quicksand.ActivateSand();*/
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
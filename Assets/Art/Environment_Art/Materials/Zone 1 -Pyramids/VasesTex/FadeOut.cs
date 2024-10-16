using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    public float fadeDuration = 1.0f;
    private Material material;
    private Color initialColor;

    void Start()
    {
        material = GetComponent<Renderer>().material;
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        float elapsedTime = 0f;
        float initialAlpha = material.GetFloat("_alpha");

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(initialAlpha, 0f, elapsedTime / fadeDuration);
            material.SetFloat("_alpha", alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}

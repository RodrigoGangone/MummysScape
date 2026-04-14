using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOut : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1.0f;
    private List<Material> materials = new();
    private Color initialColor;

    void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                materials.Add(mat);
            }
        }

        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        float elapsedTime = 0f;

        List<float> initialAlphas = new List<float>();

        foreach (var material in materials)
        {
            if (material.HasProperty("_alpha"))
                initialAlphas.Add(material.GetFloat("_alpha"));
        }

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i].HasProperty("_alpha"))
                    materials[i].SetFloat("_alpha", Mathf.Lerp(initialAlphas[i], 0f, t));
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
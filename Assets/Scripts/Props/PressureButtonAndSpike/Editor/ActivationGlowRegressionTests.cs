using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ActivationGlowRegressionTests
{
    [TestCase("Assets/Prefabs/Obstacles/Spears.prefab")]
    [TestCase("Assets/Prefabs/Props/Movables/MovableHorizontalPlatform.prefab")]
    [TestCase("Assets/Prefabs/Props/Movables/MovableHorizontalPlatform2.prefab")]
    [TestCase("Assets/Prefabs/Props/Movables/MovableVerticalPlatform.prefab")]
    [TestCase("Assets/Prefabs/Props/Movables/MovableLittlePlatform.prefab")]
    [TestCase("Assets/Prefabs/Props/Movables/MovableLittlePlatform2.prefab")]
    public void PrefabSurfacesSupportActivationEmission(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null);
        var glows = prefab.GetComponentsInChildren<ActivationGlow>(true);
        Assert.That(glows, Is.Not.Empty);
        foreach (var glow in glows)
        {
            var list = new SerializedObject(glow).FindProperty("renderers");
            Assert.That(list.arraySize, Is.GreaterThan(0));
            for (int i = 0; i < list.arraySize; i++)
            {
                var renderer = list.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;
                Assert.That(renderer, Is.Not.Null);
                Assert.That(ActivationGlow.IsSurface(renderer), Is.True);
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(material.HasProperty(ActivationGlow.IntensityProperty), Is.True, material.name);
                    Assert.That(material.HasProperty(ActivationGlow.ColorProperty), Is.True, material.name);
                    Assert.That(material.GetFloat(ActivationGlow.IntensityProperty), Is.Zero, "El reposo debe conservar su aspecto.");
                    Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False, material.shader.name);
                }
            }
        }
    }

    [Test]
    public void MultipleSlotsGlowWithoutChangingSharedMaterialsAndRestoreTheirBlocks()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Art/Environment/Modules/Materials/Zone 1 -Pyramids/Spears/Mat_Spears.mat");
        var root = new GameObject("Glowing instance");
        var other = new GameObject("Unrelated instance");
        try
        {
            var mesh = root.AddComponent<MeshRenderer>();
            mesh.sharedMaterials = new[] { material, material };
            var outsider = other.AddComponent<MeshRenderer>();
            outsider.sharedMaterial = material;
            var prior = new MaterialPropertyBlock();
            prior.SetFloat("_Intensity", 0.75f);
            mesh.SetPropertyBlock(prior, 1);
            var glow = root.AddComponent<ActivationGlow>();
            glow.Configure(new Renderer[] { mesh, mesh });
            glow.SetIntensity(2f);
            var result = new MaterialPropertyBlock();
            for (int slot = 0; slot < 2; slot++)
            {
                mesh.GetPropertyBlock(result, slot);
                Assert.That(result.GetFloat(ActivationGlow.IntensityProperty), Is.EqualTo(2f));
            }
            outsider.GetPropertyBlock(result, 0);
            Assert.That(result.isEmpty, Is.True);
            Assert.That(material.GetFloat(ActivationGlow.IntensityProperty), Is.Zero);
            glow.enabled = false;
            mesh.GetPropertyBlock(result, 0);
            Assert.That(result.isEmpty, Is.True);
            mesh.GetPropertyBlock(result, 1);
            Assert.That(result.GetFloat(ActivationGlow.IntensityProperty), Is.Zero);
            Assert.That(result.GetFloat("_Intensity"), Is.EqualTo(0.75f));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(other);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Save;

/// <summary> 
/// Monitor de Progreso: Sincroniza los materiales de la interfaz de gemas con los datos de 
/// persistencia, actualizando el estado de "recogido" en tiempo real durante la partida. 
/// </summary>

public class UIGemManager : MonoBehaviour
{
    [SerializeField] private Material[] _gemMaterials;
    [SerializeField] private Transform[] _gemTargets;
    [SerializeField] private ParticleSystem _arrivalVfxPrefab;
    [SerializeField] private Camera _gemCamera;
    [SerializeField] private UIGemArrivalImpact _arrivalImpact;

    private readonly List<ParticleSystem> _liveEffects = new();
    private bool _paused;
    
    private static readonly int IsPickedProp = Shader.PropertyToID("_IsPicked");


    private void Start()
    {
        if (_gemMaterials == null)
            return;

        for (int i = 0; i < _gemMaterials.Length; i++)
        {
            int gemNum = i + 1;
            SetGemUI(gemNum, WasGemPicked(gemNum));
        }
    }

    private void OnGemReachedUI(int gemNum)
    {
        if (GetMaterialByGemNum(gemNum) == null)
            return;

        Transform target = GetGemTarget(gemNum);

        SetGemUI(gemNum, true);
        PlayArrivalVfx(gemNum, target);
        _arrivalImpact?.Play(gemNum, target);
    }
    
    private Transform GetGemTarget(int gemNum)
    {
        int index = gemNum - 1;

        return _gemTargets != null &&
               index >= 0 &&
               index < _gemTargets.Length
            ? _gemTargets[index]
            : null;
    }

    private void SetGemUI(int gemNum, bool picked)
    {
        var mat = GetMaterialByGemNum(gemNum);
        if (!mat) return;  

        if (mat.HasProperty(IsPickedProp))
        {
            mat.SetFloat(IsPickedProp, picked ? 1 : 0);
        }
    }

    private Material GetMaterialByGemNum(int gemNum)
    {
        if (_gemMaterials == null || gemNum < 1 || gemNum > _gemMaterials.Length) return null;
        return _gemMaterials[gemNum - 1];
    }

    private void PlayArrivalVfx(int gemNum, Transform target)
    {
        if (_arrivalVfxPrefab == null || target == null)
            return;

        // Sólo usamos la cámara para orientar el plano del efecto.
        // La posición pertenece exclusivamente a la gema impactada.
        Quaternion rotation = _gemCamera != null
            ? _gemCamera.transform.rotation
            : target.rotation;

        Vector3 position = target.position;

        ParticleSystem instance = Instantiate(
            _arrivalVfxPrefab,
            position,
            rotation,
            target.parent);

        SetLayerRecursively(instance.gameObject, target.gameObject.layer);

        instance
            .GetComponent<GemArrivalVfx>()
            ?.SetGemMaterial(GetMaterialByGemNum(gemNum));

        _liveEffects.Add(instance);

        instance.Play(true);

        if (_paused)
            instance.Pause(true);

        StartCoroutine(ReleaseEffect(instance));
    }
    
    private IEnumerator ReleaseEffect(ParticleSystem instance)
    {
        // Incluye hijos, delays y pausa; no corta los fragmentos al terminar el flash.
        while (instance != null && instance.IsAlive(true))
            yield return null;

        _liveEffects.Remove(instance);
        if (instance != null)
            Destroy(instance.gameObject);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void OnPauseChanged(bool paused)
    {
        _paused = paused;
        _arrivalImpact?.SetPaused(paused);
        foreach (ParticleSystem effect in _liveEffects)
        {
            if (effect == null) continue;
            foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
            {
                if (paused && system.isPlaying) system.Pause(false);
                else if (!paused && system.isPaused) system.Play(false);
            }
        }
    }

    private void OnEnable()
    {
        _paused = false;
        GameEventManager.Instance.levelEvents.OnGemReachedUI.Register<int>(OnGemReachedUI);
        GameEventManager.Instance.levelEvents.OnPauseChanged.Register<bool>(OnPauseChanged);
    }

    private void OnDisable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.levelEvents.OnGemReachedUI.Unregister<int>(OnGemReachedUI);
            GameEventManager.Instance.levelEvents.OnPauseChanged.Unregister<bool>(OnPauseChanged);
        }

        StopAllCoroutines();
        foreach (ParticleSystem effect in _liveEffects)
            if (effect != null)
                Destroy(effect.gameObject);
        _liveEffects.Clear();
    }
}

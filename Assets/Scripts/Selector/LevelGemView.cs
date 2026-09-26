using System.Collections;
using UnityEngine;

public class LevelGemView : MonoBehaviour
{
    [System.Serializable]
    private class GemSlot
    {
        public GameObject collectedVisual;
        public GameObject missingVisual;
        public Transform pulseTarget;
    }

    [Header("Gem Slots")]
    [SerializeField]
    private GemSlot[] slots = new GemSlot[3];

    [Header("Visibility")]
    [SerializeField]
    private bool hideWhileLevelLocked = true;

    [Header("Pulse")]
    [SerializeField]
    private float pulseScale = 1.5f;

    [SerializeField]
    private float pulseDuration = 0.5f;

    public int SlotCount =>
        slots != null
            ? slots.Length
            : 0;

    public void ApplyState(LevelTileState state)
    {
        bool hideAll =
            hideWhileLevelLocked &&
            state.Availability == LevelAvailability.Locked;

        hideAll |= state.RevealPending;

        for (int i = 0; i < SlotCount; i++)
        {
            if (hideAll)
            {
                SetSlotVisible(
                    i,
                    false,
                    false
                );

                continue;
            }

            bool collected =
                state.Gems != null &&
                i < state.Gems.Length &&
                state.Gems[i];

            SetSlotVisible(
                i,
                true,
                collected
            );
        }
    }

    public void HideAll()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            SetSlotVisible(
                i,
                false,
                false
            );
        }
    }

    public void RevealCollectedGem(int index)
    {
        if (!IsValid(index))
            return;

        SetSlotVisible(
            index,
            true,
            true
        );

        StartCoroutine(
            PulseRoutine(index)
        );
    }

    private void SetSlotVisible(
        int index,
        bool visible,
        bool collected)
    {
        if (!IsValid(index))
            return;

        GemSlot slot = slots[index];

        if (slot.collectedVisual != null)
        {
            slot.collectedVisual.SetActive(
                visible && collected
            );
        }

        if (slot.missingVisual != null)
        {
            slot.missingVisual.SetActive(
                visible && !collected
            );
        }
    }

    private IEnumerator PulseRoutine(int index)
    {
        GemSlot slot = slots[index];

        Transform target =
            slot.pulseTarget;

        if (target == null &&
            slot.collectedVisual != null)
        {
            target =
                slot.collectedVisual.transform;
        }

        if (target == null)
            yield break;

        Vector3 originalScale =
            target.localScale;

        Vector3 targetScale =
            originalScale * pulseScale;

        float halfDuration =
            Mathf.Max(
                0.01f,
                pulseDuration * 0.5f
            );

        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            target.localScale =
                Vector3.Lerp(
                    originalScale,
                    targetScale,
                    elapsed / halfDuration
                );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            target.localScale =
                Vector3.Lerp(
                    targetScale,
                    originalScale,
                    elapsed / halfDuration
                );

            yield return null;
        }

        target.localScale =
            originalScale;
    }

    private bool IsValid(int index)
    {
        return slots != null &&
               index >= 0 &&
               index < slots.Length &&
               slots[index] != null;
    }
}
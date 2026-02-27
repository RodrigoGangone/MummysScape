using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[RequireComponent(typeof(PlayableDirector))]
public class CinematicObserver : MonoBehaviour
{
    [SerializeField] private string cinematicId;

    [Header("UI TUTORIAL / PROMPT")] [SerializeField]
    private GameObject interaction;

    [SerializeField] private Image interactionBtn;
    [SerializeField] private Sprite btnA;
    [SerializeField] private Sprite btnY;

    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();

    private void Start() => CheckStatus();

    private void CheckStatus()
    {
        // Si ya fue vista, activamos el prompt visual para indicar que se puede saltar
        if (Save.IsCinematicSeen(cinematicId))
        {
            Debug.Log($"[Cinematic: {cinematicId}] Se puede saltear");
            // Mostramos el prompt (Botón Y) para avisar que el skip está disponible
            ShowInteractionInput(true, buttonType.Y);
        }
        else
        {
            Debug.Log($"[Cinematic: {cinematicId}] Primera vez; no se puede saltear todavía");
            ShowInteractionInput(false, buttonType.Y);
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Accept") && Save.IsCinematicSeen(cinematicId))
            Transition.FadeInAndLoadScene(1);
    }

    public void MarkAsFinished()
    {
        if (!Save.IsCinematicSeen(cinematicId))
            Save.MarkCinematicSeen(cinematicId);

        Transition.FadeInAndLoadScene(1);
    }

    private void ShowInteractionInput(bool value, buttonType button)
    {
        if (interactionBtn == null) return;
        interactionBtn.sprite =
            button switch { buttonType.A => btnA, buttonType.Y => btnY, _ => interactionBtn.sprite };
        interaction.SetActive(value);
    }
}
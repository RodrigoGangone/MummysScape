using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[RequireComponent(typeof(PlayableDirector))]
public class CinematicObserver : MonoBehaviour
{
    [SerializeField] private string cinematicId;
    
    [Header("SETTINGS")]
    [Tooltip("Si es verdadero, permitirá saltar la cinemática aunque no se haya visto antes.")]
    [SerializeField] private bool isSkippable;

    [Header("UI TUTORIAL / PROMPT")] 
    [SerializeField] private GameObject interaction;
    [SerializeField] private Image interactionBtn;
    [SerializeField] private Sprite btnA;
    [SerializeField] private Sprite btnY;

    private SceneTransitionManager Transition => GetComponent<SceneTransitionManager>();

    private void Start() => CheckStatus();

    private void CheckStatus()
    {
        // Se mantiene tu condición original, sumando la variable isSkippable
        if (Save.IsCinematicSeen(cinematicId) || isSkippable)
        {
            Debug.Log($"[Cinematic: {cinematicId}] Se puede saltear");
            ShowInteractionInput(true, ButtonType.Y);
        }
        else
        {
            Debug.Log($"[Cinematic: {cinematicId}] Primera vez; no se puede saltear todavía");
            ShowInteractionInput(false, ButtonType.Y);
        }
    }

    private void Update()
    {
        // Se mantiene tu condición original explícita, sumando isSkippable
        if (Input.GetButtonDown("Accept") && (Save.IsCinematicSeen(cinematicId) || isSkippable))
            Transition.FadeInAndLoadScene(1);
    }

    public void MarkAsFinished()
    {
        if (!Save.IsCinematicSeen(cinematicId))
            Save.MarkCinematicSeen(cinematicId);

        Transition.FadeInAndLoadScene(3);
    }

    private void ShowInteractionInput(bool value, ButtonType button)
    {
        if (interactionBtn == null) return;
        interactionBtn.sprite =
            button switch { ButtonType.A => btnA, ButtonType.Y => btnY, _ => interactionBtn.sprite };
        interaction.SetActive(value);
    }
}
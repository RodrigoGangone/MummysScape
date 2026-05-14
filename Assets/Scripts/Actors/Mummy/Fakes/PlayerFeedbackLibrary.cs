using UnityEngine;
using static PlayerEnum;

[CreateAssetMenu(menuName = "Fakes/Feedback/Library")]
public class PlayerFeedbackLibrary : ScriptableObject
{
    [System.Serializable]
    public struct FeedbackEntry
    {
        public PlayerStateId state;
        public PlayerSize size;
        public FeedbackAction[] actions;
    }

    public FeedbackEntry[] entries;

    // --- NUEVO MÉTODO AUXILIAR ---
    public bool HasFeedback(PlayerStateId state, PlayerSize size)
    {
        if (entries == null) return false;
        return System.Array.Exists(entries, e => e.state == state && e.size == size);
    }

    public void Execute(PlayerStateId state, PlayerSize size, PlayerContext ctx)
    {
        var entry = System.Array.Find(entries, e => e.state == state && e.size == size);
        if (entry.actions != null)
        {
            foreach (var action in entry.actions)
            {
                if (action != null) action.Play(ctx);
            }
        }
    }
}

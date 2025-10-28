using UnityEngine;

namespace Sounds
{
    public class FindAudioListeners : MonoBehaviour
    {
        [ContextMenu("Find Audio Listeners")]
        void FindListeners()
        {
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            foreach (AudioListener listener in listeners)
            {
                Debug.Log($"Audio Listener found on GameObject: {listener.gameObject.name}", listener.gameObject);
            }
        }
    }
}


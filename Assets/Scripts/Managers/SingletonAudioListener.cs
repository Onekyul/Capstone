using UnityEngine;

public class SingletonAudioListener : MonoBehaviour
{
    private void Awake()
    {
        var all = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        if (all.Length > 1)
            Destroy(GetComponent<AudioListener>());
    }
}

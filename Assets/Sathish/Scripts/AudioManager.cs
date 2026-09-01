using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource audioSource;

    public void PlayAudio(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
}

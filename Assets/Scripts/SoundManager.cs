using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;   // Gắn sẵn trong Inspector
    public AudioSource sfxSource;     // Gắn sẵn trong Inspector

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip hitSound;
    private bool isMusicEnabled = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource != null && backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                if (isMusicEnabled)
                    musicSource.Play();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ToggleMusic()
    {
        isMusicEnabled = !isMusicEnabled;

        if (musicSource == null) return;

        if (isMusicEnabled)
            musicSource.UnPause();
        else
            musicSource.Pause();
    }

    public bool IsMusicPlaying()
    {
        return isMusicEnabled && musicSource != null && musicSource.isPlaying;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // Gọi nhanh các SFX định sẵn
    public void PlayHitSound() => PlaySFX(hitSound);
}

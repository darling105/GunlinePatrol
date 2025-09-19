using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button playButton;
    public Button musicToggleButton;
    public Button quitButton;

    [Header("Music Button Sprites")]
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    [Header("Scene Names")]
    public string gameSceneName = "GameScene";

    private void Start()
    {
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(StartGame);

        if (musicToggleButton != null)
            musicToggleButton.onClick.AddListener(ToggleMusic);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Cập nhật icon nhạc theo trạng thái hiện tại
        UpdateMusicButtonSprite();
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ToggleMusic()
    {
        SoundManager.Instance?.ToggleMusic();
        UpdateMusicButtonSprite();
    }

    private void UpdateMusicButtonSprite()
    {
        if (SoundManager.Instance != null && musicToggleButton != null)
        {
            bool isOn = SoundManager.Instance.IsMusicPlaying();
            musicToggleButton.image.sprite = isOn ? musicOnSprite : musicOffSprite;
        }
    }

    public void QuitGame()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

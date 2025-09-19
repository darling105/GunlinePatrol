using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("Refs")]
    public PlayerController player;
    public GameObject winPanel;
    public GameObject losePanel;   // thêm panel thua
    public GameObject pausePanel;

    private readonly HashSet<Enemy> alive = new HashSet<Enemy>();
    public int totalEnemies;
    private bool won;
    private bool lost;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);

        // theo dõi số bullet trong scene
        Bullet.OnBulletDestroyed += CheckLoseCondition;
    }

    void OnDestroy()
    {
        Bullet.OnBulletDestroyed -= CheckLoseCondition;
    }

    public void RegisterEnemy(Enemy e)
    {
        if (alive.Add(e)) totalEnemies = alive.Count;
    }

    public void UnregisterEnemy(Enemy e)
    {
        if (alive.Remove(e)) CheckWin();
    }

    public void NotifyEnemyDied(Enemy e)
    {
        if (alive.Remove(e)) CheckWin();
    }

    void CheckWin()
    {
        if (won || lost) return;
        if (alive.Count == 0 && totalEnemies > 0)
        {
            won = true;
            if (player) player.PlayDance();
            if (winPanel) winPanel.SetActive(true);
            Debug.Log("YOU WIN!");
        }
    }

    // gọi khi bullet bị Destroy -> check lose
    void CheckLoseCondition(Bullet b)
    {
        if (won || lost) return;

        // Nếu không còn bullet đang bay và player đã hết đạn nhưng còn Enemy
        if (Bullet.ActiveCount == 0 && player.GetBulletsLeft() <= 0 && alive.Count > 0)
        {
            lost = true;
            if (losePanel) losePanel.SetActive(true);
            Debug.Log("YOU LOSE!");
        }
    }

    public void NotifyOutOfAmmo()
    {
        if (won || lost) return;

        // Nếu hết đạn nhưng còn đạn đang bay thì đợi đến khi đạn cuối Destroy mới check
        if (player.GetBulletsLeft() <= 0 && alive.Count > 0)
        {
            // khi viên cuối Destroy → CheckLoseCondition sẽ xử lý
            if (Bullet.ActiveCount == 0)
                CheckLoseCondition(null);
        }
    }

    public void NextLevel()
    {
        Time.timeScale = 1f; // đảm bảo không bị pause

        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            // nếu hết level thì quay về level 1
            next = 1;
        }

        SceneManager.LoadScene(next);
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }
}

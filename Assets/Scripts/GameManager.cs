using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("Refs")]
    public PlayerController player;   // kéo Player vào
    public GameObject winPanel;       // panel Win (ẩn sẵn)

    private readonly HashSet<Enemy> alive = new HashSet<Enemy>();
    public int totalEnemies;
    private bool won;

    public GameObject losePanel;   // kéo Panel Lose (ẩn sẵn) vào

    private bool lost;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
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
        if (won) return;
        // chỉ thắng nếu đã có spawn ít nhất 1 enemy và hiện còn 0
        if (alive.Count == 0 && totalEnemies > 0)
        {
            won = true;
            if (player) player.PlayDance();   // Player idle -> dance
            if (winPanel) winPanel.SetActive(true); // chỉ hiện panel, KHÔNG pause game
            Debug.Log("YOU WIN!");
        }
    }

    public void LoseGame()
    {
        if (won || lost) return;
        lost = true;

        // Có thể cho player dừng bắn/đứng idle
        if (player) player.SetIdle();

        if (losePanel) losePanel.SetActive(true);    // chỉ bật panel, không pause
        Debug.Log("YOU LOSE!");
    }
}

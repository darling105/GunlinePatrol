using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum State { Idle, Die }
    public State state = State.Idle;

    [Header("Animation")]
    public Animator animator;          // set trong Inspector
    public string dieTrigger = "Die";  // tên trigger trong Animator

    [Header("Death Setup")]
    public float destroyDelay = 2f;    // thời gian chờ sau khi chết
    Collider[] cols;
    bool died;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        cols = GetComponentsInChildren<Collider>();
    }

    void Start()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.RegisterEnemy(this);
    }
    void OnDisable()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.UnregisterEnemy(this);
    }

    // Bullet sẽ gọi hàm này khi trúng
    public void HitByBullet()
    {
        if (died || state == State.Die) return;

        state = State.Die;
        died = true;

        if (animator && !string.IsNullOrEmpty(dieTrigger))
            animator.SetTrigger(dieTrigger);

        // tắt va chạm & chuyển kinematic để không còn tương tác
        foreach (var c in cols) c.enabled = false;
        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.detectCollisions = false; }

        GameManager.Instance.NotifyEnemyDied(this);

        // tuỳ bạn muốn phá hủy hay ẩn đi
        Destroy(gameObject, destroyDelay);
    }
}

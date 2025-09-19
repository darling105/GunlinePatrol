using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Shooting (UI Button sẽ gọi FireButtonPressed)")]
    public Transform muzzle;                // điểm bắn (đầu súng)
    public GameObject bulletPrefab;
    public float bulletSpeed = 40f;
    public float fireCooldown = 0.15f;
    public int maxBullets = 20;
    public Button shootButton;              // gán nút UI Shoot nếu muốn disable/enable

    [Header("UI")]
    public TMP_Text bulletsText;            // TextMeshPro hiển thị số đạn (X/Y bullet)

    [Header("Rotate on Player Click")]
    public float rotateTime = 0.15f;

    Camera cam;
    Collider myCol;

    bool rotating;
    float nextFireAllowed;
    int bulletsLeft;

    [Header("Animation")]
    public Animator animator;               // set trong Inspector
    public string danceTrigger = "Dance";
    public string idleStateName = "Idle";   // tên state idle trong Animator (tuỳ chọn)

    void Awake()
    {
        cam = Camera.main;
        myCol = GetComponentInChildren<Collider>();
        bulletsLeft = maxBullets;
    }

    void OnEnable()
    {
        Bullet.OnBulletSpawned += _ => UpdateShootButton();
        Bullet.OnBulletDestroyed += _ => UpdateShootButton();
        UpdateShootButton();
        UpdateBulletsUI();
    }

    void OnDisable()
    {
        Bullet.OnBulletSpawned -= _ => UpdateShootButton();
        Bullet.OnBulletDestroyed -= _ => UpdateShootButton();
    }

    void Update()
    {
        // cập nhật trạng thái nút theo cooldown/rotate/đạn
        UpdateShootButton();

        // Xoay player khi click trúng player
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            if (hit.collider == myCol || hit.collider.transform.IsChildOf(transform))
            {
                if (!rotating) StartCoroutine(Rotate90CCW());
            }
        }
    }

    // HÀM NÀY ĐƯỢC GÁN CHO BUTTON OnClick()
    public void FireButtonPressed()
    {
        if (rotating) return;                       // đang xoay -> không bắn
        if (Bullet.ActiveCount > 0) return;         // chỉ cho phép 1 viên đạn tồn tại
        if (Time.time < nextFireAllowed) return;    // cooldown
        if (bulletsLeft <= 0) return;               // hết đạn

        if (!muzzle || !bulletPrefab)
        {
            Debug.LogWarning("Missing muzzle/bulletPrefab");
            return;
        }

        nextFireAllowed = Time.time + fireCooldown;

        var go = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(muzzle.forward));
        var b = go.GetComponent<Bullet>();
        b.Fire(muzzle.forward * bulletSpeed);

        bulletsLeft--;
        UpdateShootButton();
        UpdateBulletsUI();

        // Nếu vừa bắn xong mà hết đạn -> thua game
        if (bulletsLeft <= 0)
        {
            // khóa nút bắn (đảm bảo)
            if (shootButton) shootButton.interactable = false;

            if (GameManager.HasInstance)
                GameManager.Instance.NotifyOutOfAmmo();

            Debug.Log("Out of ammo -> LOSE");
        }
    }

    IEnumerator Rotate90CCW()
    {
        rotating = true;
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.AngleAxis(90f, Vector3.up) * start;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateTime;
            transform.rotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        transform.rotation = end;
        rotating = false;
    }

    // --- Helpers ---
    void UpdateShootButton()
    {
        bool canShoot = !rotating
                        && bulletsLeft > 0
                        && Time.time >= nextFireAllowed
                        && Bullet.ActiveCount == 0;

        if (shootButton) shootButton.interactable = canShoot;
    }

    void UpdateBulletsUI()
    {
        if (bulletsText) bulletsText.text = $"{bulletsLeft}/{maxBullets}";
    }

    public int GetBulletsLeft() => bulletsLeft;

    public void RefillBullets(int amount = -1)
    {
        bulletsLeft = (amount < 0) ? maxBullets : amount;
        UpdateShootButton();
        UpdateBulletsUI();
    }

    public void PlayDance()
    {
        if (!animator) return;
        if (!string.IsNullOrEmpty(danceTrigger))
            animator.SetTrigger(danceTrigger);
    }

    public void SetIdle()
    {
        if (!animator) return;
        if (!string.IsNullOrEmpty(idleStateName))
            animator.Play(idleStateName, 0, 0f);
    }
}

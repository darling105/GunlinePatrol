using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Lifetime & Physics")]
    public float lifeTime = 6f;

    // Đếm toàn cục + sự kiện cho UI button
    public static int ActiveCount { get; private set; } = 0;
    public static Action<Bullet> OnBulletSpawned;
    public static Action<Bullet> OnBulletDestroyed;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Thiết lập physics an toàn cho đạn nhanh + trigger
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnEnable()
    {
        ActiveCount++;
        OnBulletSpawned?.Invoke(this);
    }

    void OnDisable()
    {
        ActiveCount = Mathf.Max(0, ActiveCount - 1);
        OnBulletDestroyed?.Invoke(this);
    }

    public void Fire(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
        AlignToVelocity();

        CancelInvoke(nameof(Timeout));
        Invoke(nameof(Timeout), lifeTime);
    }

    public void SetVelocityAndAlign(Vector3 newVelocity)
    {
        rb.linearVelocity = newVelocity;
        AlignToVelocity();
    }

    void Timeout()
    {
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        AlignToVelocity();
    }

    void AlignToVelocity()
    {
        Vector3 v = rb.linearVelocity;
        if (v.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(v.normalized, Vector3.up);
    }

    // ====== CONTACT (va chạm) với Enemy / Wall / TubeSide ======
    void OnCollisionEnter(Collision col)
    {
        // Lấy 1 contact (nếu cần dùng cho hiệu ứng/decals)
        // var contact = col.GetContact(0); // giữ lại nếu bạn cần normal/point

        // Enemy: chuyển Enemy sang Die + huỷ đạn
        if (col.collider.CompareTag("Enemy"))
        {
            var enemy = col.collider.GetComponentInParent<Enemy>();
            if (enemy != null) enemy.HitByBullet();

            Destroy(gameObject);
            return;
        }

        // Wall hoặc TubeSide: bắn trúng tường/cạnh ống thì huỷ đạn
        if (col.collider.CompareTag("Wall") || col.collider.CompareTag("TubeSide"))
        {
            Destroy(gameObject);
            return;
        }

        // Nếu bạn có Mirror hoặc các bề mặt phản xạ, có thể thêm ở đây:
        // if (col.collider.CompareTag("Mirror"))
        // {
        //     Vector3 n = col.GetContact(0).normal;
        //     Vector3 reflected = Vector3.Reflect(rb.velocity, n);
        //     SetVelocityAndAlign(reflected);
        //     return;
        // }

        // Mặc định: nếu không thuộc các tag trên, bạn muốn đạn tồn tại hay huỷ?
        // Ở đây mình để "không làm gì" để đạn còn bay tiếp nếu chạm những thứ khác (triggered props).
        // Nếu muốn huỷ tất cả, bỏ comment dòng dưới:
        // Destroy(gameObject);
    }
}

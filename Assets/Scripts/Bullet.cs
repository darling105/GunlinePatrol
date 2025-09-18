using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public float lifeTime = 6f;

    public static int ActiveCount { get; private set; } = 0;     // << đếm toàn cục
    public static Action<Bullet> OnBulletSpawned;
    public static Action<Bullet> OnBulletDestroyed;

    Rigidbody rb;
    TrailRenderer trail;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnEnable()
    {
        ActiveCount++;
        OnBulletSpawned?.Invoke(this);
        if (trail) { trail.Clear(); trail.emitting = true; }
    }

    void OnDisable()
    {
        if (trail) trail.emitting = false;
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

    void Timeout() => Destroy(gameObject);

    void FixedUpdate() => AlignToVelocity();

    void AlignToVelocity()
    {
        var v = rb.linearVelocity;
        if (v.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(v.normalized, Vector3.up);
    }

    // ví dụ va chạm → tiêu hủy
    void OnCollisionEnter(Collision _)
    {
        Destroy(gameObject);
    }
}

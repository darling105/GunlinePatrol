using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class TubeEntrance
{
    public string name;
    public Collider entranceCollider;   // IsTrigger = true
    public Transform exitPoint;         // vị trí & hướng ra tương ứng
    [Tooltip("Hướng IN của entrance (local). Ví dụ: entrance.forward = hướng vào trong ống")]
    public Vector3 localInward = Vector3.forward;
    [Range(0f, 1f)] public float minEnterDot = 0.5f;
}

public class TubeRedirectorMulti : MonoBehaviour
{
    [Header("Entrances (setup at least 1)")]
    public TubeEntrance[] entrances;

    [Header("Redirect Settings")]
    public bool teleportToExit = true;
    public bool preserveSpeed = true;
    public bool clampToXZ = true;
    [Range(0.01f, 0.3f)] public float exitOffset = 0.05f;

    [Header("Safety")]
    public float perBulletTeleportCooldown = 0.12f;

    // --- ROTATE ON CLICK ---
    [Header("Rotate On Click")]
    public float rotateTime = 0.15f;                      // thời gian xoay 90°
    [Tooltip("Điểm pivot để xoay (không nên là con của Tube). Nếu để trống sẽ xoay quanh chính Tube.")]
    public Transform rotationPivot;
    private bool rotating;
    private Camera cam;

    private readonly Dictionary<int, float> lastTeleportTime = new();

    void Awake() => cam = Camera.main;

    void Start()
    {
        foreach (var e in entrances)
        {
            if (e == null) continue;
            if (!e.entranceCollider) Debug.LogWarning($"[TubeRedirectorMulti] entrance '{e.name}' missing collider.", this);
            if (!e.exitPoint) Debug.LogWarning($"[TubeRedirectorMulti] entrance '{e.name}' missing exitPoint.", this);
            if (e.entranceCollider && !e.entranceCollider.isTrigger)
                Debug.LogWarning($"[TubeRedirectorMulti] entrance '{e.name}' collider should be IsTrigger.", e.entranceCollider);
        }
    }

    void OnEnable()
    {
        for (int i = 0; i < entrances.Length; i++)
        {
            var e = entrances[i];
            if (e == null || e.entranceCollider == null) continue;

            var go = e.entranceCollider.gameObject;
            var fwd = go.GetComponent<EntranceForwarder>();
            if (fwd == null) fwd = go.AddComponent<EntranceForwarder>();
            fwd.owner = this;
            fwd.entranceIndex = i;
        }
    }

    void Update()
    {
        if (rotating) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam != null ? cam.ScreenPointToRay(Input.mousePosition)
                                  : Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
                    StartCoroutine(Rotate90CW_AroundPivot());
            }
        }
    }

    public void TriggerRotateCW()
    {
        if (!rotating) StartCoroutine(Rotate90CW_AroundPivot());
    }

    // ====== XOAY ỔN ĐỊNH QUANH PIVOT ======
    IEnumerator Rotate90CW_AroundPivot()
    {
        rotating = true;

        // chốt pivot ở tọa độ thế giới ngay lúc bắt đầu (tránh pivot di chuyển khi là con của object khác)
        Vector3 pivotWorld = rotationPivot ? rotationPivot.position : transform.position;

        // số độ cần xoay và tốc độ xoay
        float targetDegrees = 90f;               // 90° mỗi lần
        float degPerSec = targetDegrees / Mathf.Max(0.0001f, rotateTime);
        float rotated = 0f;

        // để xoay theo chiều kim đồng hồ quanh trục Y (thế giới), dùng góc âm
        Vector3 worldAxis = Vector3.up;

        while (rotated < targetDegrees)
        {
            float step = Mathf.Min(targetDegrees - rotated, degPerSec * Time.deltaTime);
            transform.RotateAround(pivotWorld, worldAxis, -step);   // CW = góc âm
            rotated += step;
            yield return null;
        }

        // snap lại một lần nữa để tránh sai số tích lũy
        float remainder = targetDegrees - rotated;
        if (Mathf.Abs(remainder) > 0.001f)
            transform.RotateAround(pivotWorld, worldAxis, -remainder);

        rotating = false;
    }

    // ====== Redirect bullet giữ nguyên ======
    public void HandleEntranceTrigger(int entranceIndex, Collider other)
    {
        if (entranceIndex < 0 || entranceIndex >= entrances.Length) return;
        var e = entrances[entranceIndex];
        if (e == null) return;
        if (!other.CompareTag("Bullet")) return;

        var bullet = other.GetComponent<Bullet>();
        var rb = other.GetComponent<Rigidbody>();
        if (bullet == null || rb == null || e.exitPoint == null) return;

        int id = other.gameObject.GetInstanceID();
        float now = Time.time;
        if (lastTeleportTime.TryGetValue(id, out float last) &&
            now - last < perBulletTeleportCooldown) return;

        Vector3 inDir = rb.linearVelocity;  // FIX: dùng velocity
        if (inDir.sqrMagnitude < 1e-6f)
            inDir = (e.entranceCollider.bounds.center - other.transform.position).normalized;
        inDir = inDir.normalized;

        Vector3 inwardWorld = e.entranceCollider.transform.TransformDirection(e.localInward).normalized;

        float dot = Vector3.Dot(inDir, inwardWorld);
        if (dot < e.minEnterDot) return;

        float speed = rb.linearVelocity.magnitude;
        Vector3 outDir = e.exitPoint.forward.normalized;
        if (clampToXZ) outDir = new Vector3(outDir.x, 0f, outDir.z).normalized;
        Vector3 newVel = outDir * (preserveSpeed ? Mathf.Max(speed, 0.01f) : speed);

        if (teleportToExit)
            other.transform.position = e.exitPoint.position + outDir * exitOffset;
        else
            other.transform.position += outDir * exitOffset;

        if (other.TryGetComponent(out Bullet b) && b)
        {
            var mi = typeof(Bullet).GetMethod("SetVelocityAndAlign");
            if (mi != null) b.SendMessage("SetVelocityAndAlign", newVel, SendMessageOptions.DontRequireReceiver);
            else b.Fire(newVel);
        }
        else
        {
            rb.linearVelocity = newVel; // FIX
        }

        lastTeleportTime[id] = now;
    }

    void OnDrawGizmos()
    {
        if (entrances != null)
        {
            for (int i = 0; i < entrances.Length; i++)
            {
                var e = entrances[i];
                if (e == null) continue;

                if (e.exitPoint)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 dir = e.exitPoint.forward;
                    if (clampToXZ) dir = new Vector3(dir.x, 0f, dir.z);
                    Gizmos.DrawLine(e.exitPoint.position, e.exitPoint.position + dir.normalized * 0.4f);
                    Gizmos.DrawSphere(e.exitPoint.position, 0.02f);
                }
                if (e.entranceCollider)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 inward = e.entranceCollider.transform.TransformDirection(e.localInward).normalized;
                    if (clampToXZ) inward = new Vector3(inward.x, 0f, inward.z).normalized;
                    Vector3 p = e.entranceCollider.bounds.center;
                    Gizmos.DrawLine(p, p + inward * 0.4f);
                }
            }
        }

        if (rotationPivot)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(rotationPivot.position, 0.06f);
        }
    }
}

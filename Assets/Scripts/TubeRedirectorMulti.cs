using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TubeEntrance
{
    public string name;
    public Collider entranceCollider;                // IsTrigger = true
    public Transform exitPoint;                      // vị trí & hướng ra
    [Tooltip("Hướng IN (local). VD: forward = hướng vào trong ống")]
    public Vector3 localInward = Vector3.forward;
    [Range(0f,1f)] public float minEnterDot = 0.35f; // nới nhẹ cho chuỗi ống
}

public class TubeRedirectorMulti : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("ID để phân biệt các tube; trống thì tự sinh theo name#InstanceID")]
    public string tubeId;

    [Header("Entrances")]
    public TubeEntrance[] entrances;

    [Header("Redirect Settings")]
    public bool teleportToExit = true;
    public bool preserveSpeed  = true;
    public bool clampToXZ      = true;
    [Range(0.01f, 0.3f)] public float exitOffset = 0.07f;

    [Header("Safety")]
    [Tooltip("Chặn quay LẠI cùng 1 entrance trong khoảng thời gian này (chỉ entrance đó)")]
    public float perEntranceCooldown = 0.12f;
    [Tooltip("Độ dày 'vùng trong' sau mặt phẳng miệng ống (m) để coi là đã vào")]
    public float insideEpsilon = 0.03f;

    [Header("Rotate On Click")]
    public float rotateTime = 0.15f;
    [Tooltip("Pivot xoay (không nên là con của Tube). Nếu trống sẽ xoay quanh chính Tube.")]
    public Transform rotationPivot;

    [Header("Debug")]
    public bool debugLog;

    bool   rotating;
    Camera cam;

    void Awake()
    {
        cam = Camera.main;
        if (string.IsNullOrEmpty(tubeId))
            tubeId = $"{gameObject.name}#{GetInstanceID()}";
    }

    void Start()
    {
        foreach (var e in entrances)
        {
            if (e == null) continue;
            if (!e.entranceCollider)
                Debug.LogWarning($"[Tube] entrance '{e.name}' missing collider.", this);
            if (!e.exitPoint)
                Debug.LogWarning($"[Tube] entrance '{e.name}' missing exitPoint.", this);
            if (e.entranceCollider && !e.entranceCollider.isTrigger)
                Debug.LogWarning($"[Tube] entrance '{e.name}' collider should be IsTrigger.", e.entranceCollider);
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
            if (!fwd) fwd = go.AddComponent<EntranceForwarder>();
            fwd.owner = this;
            fwd.entranceIndex = i;
        }
    }

    void Update()
    {
        if (rotating) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = cam ? cam.ScreenPointToRay(Input.mousePosition)
                      : Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider && hit.collider.transform.IsChildOf(transform))
                StartCoroutine(Rotate90CW_AroundPivot());
        }
    }

    public void TriggerRotateCW()
    {
        if (!rotating) StartCoroutine(Rotate90CW_AroundPivot());
    }

    IEnumerator Rotate90CW_AroundPivot()
    {
        rotating = true;
        Vector3 pivotWorld = rotationPivot ? rotationPivot.position : transform.position;

        float targetDegrees = 90f;
        float degPerSec = targetDegrees / Mathf.Max(0.0001f, rotateTime);
        float rotated = 0f;

        while (rotated < targetDegrees)
        {
            float step = Mathf.Min(targetDegrees - rotated, degPerSec * Time.deltaTime);
            transform.RotateAround(pivotWorld, Vector3.up, -step); // CW = góc âm
            rotated += step;
            yield return null;
        }

        float remainder = targetDegrees - rotated;
        if (Mathf.Abs(remainder) > 0.001f)
            transform.RotateAround(pivotWorld, Vector3.up, -remainder);

        rotating = false;
    }

    // ====== BẮT ĐẠN ỔN ĐỊNH: plane-crossing + dot + cooldown per-entrance ======
    public void HandleEntranceTrigger(int entranceIndex, Collider other)
    {
        if (entranceIndex < 0 || entranceIndex >= entrances.Length) return;
        var e = entrances[entranceIndex];
        if (e == null || !other.CompareTag("Bullet")) return;

        if (!other.TryGetComponent(out Bullet bullet)) return;
        if (!other.TryGetComponent(out Rigidbody rb)) return;
        if (!other.TryGetComponent(out Collider bCol)) return;
        if (!e.exitPoint) return;

        if (!other.TryGetComponent(out BulletTransitStamp stamp))
            stamp = other.gameObject.AddComponent<BulletTransitStamp>();

        string entranceKey = $"{tubeId}#{entranceIndex}";
        float now = Time.time;

        // CHỈ chặn quay LẠI đúng entrance này trong khoảng cooldown
        if (stamp.lastEntranceKey == entranceKey && (now - stamp.lastTime) < perEntranceCooldown)
            return;

        // ---- Plane of gate (origin ~ bounds.center, normal = inwardWorld) ----
        Vector3 inwardWorld = e.entranceCollider.transform.TransformDirection(e.localInward).normalized;
        Vector3 gateOrigin  = e.entranceCollider.bounds.center;
        Plane   gatePlane   = new Plane(inwardWorld, gateOrigin);

        // Signed distance: >0 = phía ngoài (theo normal), <0 = phía trong
        float curDist = gatePlane.GetDistanceToPoint(other.transform.position);
        stamp.lastSignedDistance.TryGetValue(entranceKey, out float prevDist);

        // Hướng vào bằng velocity (nếu có), fallback tolerant
        Vector3 v = rb.linearVelocity;
        float v2 = v.sqrMagnitude;
        float dot = (v2 > 1e-6f) ? Vector3.Dot(v.normalized, inwardWorld) : 1f;

        // ---- Điều kiện NHẬN đạn (bất kỳ 1 trong 3) ----
        bool crossedFromOutside = (prevDist > 0f && curDist <= 0f);        // cắt mặt phẳng ngoài→trong
        bool deepInside         = (curDist <= -insideEpsilon);             // đã ở khá sâu bên trong
        bool movingInwardOK     = (dot >= e.minEnterDot && curDist <= 0f); // đang hướng vào & đã trong/đúng mặt

        if (!(crossedFromOutside || deepInside || movingInwardOK))
        {
            // cập nhật prevDist để lần sau detect crossing
            stamp.lastSignedDistance[entranceKey] = curDist;
            if (debugLog) Debug.Log($"[Tube] Reject {entranceKey} dot={dot:F2} curDist={curDist:F3}", this);
            return;
        }

        // ===== Redirect =====
        float speed = Mathf.Sqrt(v2);
        Vector3 outDir = e.exitPoint.forward.normalized;
        if (clampToXZ) outDir = new Vector3(outDir.x, 0f, outDir.z).normalized;

        Vector3 newVel = outDir * (preserveSpeed ? Mathf.Max(speed, 0.01f) : speed);
        other.transform.position = (teleportToExit ? e.exitPoint.position : other.transform.position) + outDir * exitOffset;

        var setAlign = typeof(Bullet).GetMethod("SetVelocityAndAlign");
        if (setAlign != null) bullet.SendMessage("SetVelocityAndAlign", newVel, SendMessageOptions.DontRequireReceiver);
        else rb.linearVelocity = newVel;

        // Lưu dấu vết + tránh dính lại entrance vừa đi qua
        stamp.lastEntranceKey = entranceKey;
        stamp.lastTime = now;
        stamp.lastSignedDistance[entranceKey] = gatePlane.GetDistanceToPoint(other.transform.position);

        StartCoroutine(TempIgnoreCollision(bCol, e.entranceCollider, perEntranceCooldown));

        if (debugLog) Debug.Log($"[Tube] OK -> {entranceKey}  speed={speed:F2}", this);
    }

    IEnumerator TempIgnoreCollision(Collider bulletCol, Collider entranceCol, float duration)
    {
        if (bulletCol && entranceCol)
        {
            Physics.IgnoreCollision(bulletCol, entranceCol, true);
            yield return new WaitForSeconds(duration);
            if (bulletCol && entranceCol)
                Physics.IgnoreCollision(bulletCol, entranceCol, false);
        }
    }

    void OnDrawGizmos()
    {
        if (entrances != null)
        {
            foreach (var e in entrances)
            {
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

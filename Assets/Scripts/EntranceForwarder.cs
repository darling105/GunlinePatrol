using UnityEngine;

class EntranceForwarder : MonoBehaviour
{
    public TubeRedirectorMulti owner;
    public int entranceIndex = -1;

    void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.HandleEntranceTrigger(entranceIndex, other);
    }
    void OnTriggerStay(Collider other)
    {
        if (owner != null) owner.HandleEntranceTrigger(entranceIndex, other);
    }

    void OnDrawGizmosSelected()
    {
        if (owner == null || entranceIndex < 0 || owner.entrances == null) return;
        var e = owner.entrances[entranceIndex];
        if (e == null || e.entranceCollider == null) return;
        Gizmos.color = Color.yellow;
        Vector3 worldIn = e.entranceCollider.transform.TransformDirection(e.localInward).normalized;
        Gizmos.DrawLine(e.entranceCollider.transform.position, e.entranceCollider.transform.position + worldIn * 0.4f);
    }
}
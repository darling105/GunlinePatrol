using UnityEngine;
using System.Collections.Generic;

// Dấu vết teleport + trạng thái plane-crossing cho từng entrance
public class BulletTransitStamp : MonoBehaviour
{
    public string lastEntranceKey = "";
    public float lastTime = -999f;

    // Lưu "khoảng cách có dấu" tới mặt phẳng miệng ống theo key (tubeId#entrance)
    public Dictionary<string, float> lastSignedDistance = new Dictionary<string, float>();
}

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // ตัวผู้เล่น (Player)

    [Header("Follow Settings")]
    public float smoothSpeed = 0.15f; // ความนุ่มนวลในการติดตาม
    public Vector3 offset;            // ระยะห่างจากผู้เล่น

    [Header("Stage Boundaries")]
    public Vector2 minBounds; // ขอบซ้าย-ล่าง ของ stage
    public Vector2 maxBounds; // ขอบขวา-บน ของ stage

    private Camera cam;
    private float halfHeight;
    private float halfWidth;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // คำนวณครึ่งหนึ่งของกล้องเพื่อจำกัดขอบเขตได้แม่นยำ
        halfHeight = cam.orthographicSize;
        halfWidth = cam.aspect * halfHeight;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ตำแหน่งเป้าหมายของกล้อง (ตามผู้เล่น)
        Vector3 desiredPosition = target.position + offset;

        // จำกัดไม่ให้เกินขอบ stage
        float clampedX = Mathf.Clamp(desiredPosition.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        float clampedY = Mathf.Clamp(desiredPosition.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        Vector3 clampedPosition = new Vector3(clampedX, clampedY, desiredPosition.z);

        // เคลื่อนที่อย่างนุ่มนวล
        transform.position = Vector3.Lerp(transform.position, clampedPosition, smoothSpeed);
    }

#if UNITY_EDITOR
    // แสดงกรอบของขอบเขตใน Scene View เพื่อปรับได้ง่าย
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}


using UnityEngine;

public class PlayerColorEffect : MonoBehaviour
{
    [Header("Player Life Settings")]
    public MyPlayerScript player; // อ้างอิงถึงสคริปต์ HP ของผู้เล่น

    [Header("Color Effect Settings")]
    public SpriteRenderer spriteRenderer; // Renderer ของตัว Player
    public Color lowHealthColor = new Color(1f, 0f, 0f, 1f); // สีแดง (R=255,G=0,B=0)
    public float flashSpeed = 3f; // ความเร็วในการกระพริบ (ปรับได้)

    private Color originalColor;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (player == null)
        {
#if UNITY_2023_1_OR_NEWER
            player = FindFirstObjectByType<MyPlayerScript>();
#else
            player = FindObjectOfType<MyPlayerScript>();
#endif
        }
    }

    void Update()
    {
        if (player == null || spriteRenderer == null) return;

        // 🔹 ถ้า Player ตาย → โปร่งใส (A=0)
        if (player.GetCurrentLife() <= 0)
        {
            Color c = lowHealthColor;
            c.a = 0f;
            spriteRenderer.color = c;
            return;
        }

        // 🔹 ถ้า HP = 1 → กระพริบสีแดง
        if (player.GetCurrentLife() == 1)
        {
            float alpha = (Mathf.Sin(Time.time * flashSpeed) + 1f) / 2f; // ค่าระหว่าง 0–1
            Color c = lowHealthColor;
            c.a = Mathf.Lerp(0f, 1f, alpha);
            spriteRenderer.color = c;
        }
        else
        {
            // 🔹 ถ้า HP มากกว่า 1 → กลับเป็นสีเดิม
            spriteRenderer.color = originalColor;
        }
    }
}


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyBossAppearAfterAllDead : MonoBehaviour
{
    [Header("Boss Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("UI Settings")]
    public GameObject heartPrefab;     // รูปหัวใจ 1 ดวง (UI Image)
    public Transform heartContainer;   // จุดวางหัวใจ (อยู่บนหัว Boss)
    private Image[] hearts;

    [Header("Appearance Settings")]
    public float checkInterval = 1.0f; // เช็คทุกๆ 1 วิ
    public string enemyTag = "Enemy";  // tag ของศัตรูทั่วไป
    public Animator animator;

    private bool hasAppeared = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // ซ่อนตัวเองก่อนเริ่ม
        spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        // ตั้งค่าพลังชีวิต
        currentHealth = maxHealth;

        // สร้าง UI หัวใจ
        if (heartContainer != null && heartPrefab != null)
        {
            hearts = new Image[maxHealth];
            for (int i = 0; i < maxHealth; i++)
            {
                GameObject heart = Instantiate(heartPrefab, heartContainer);
                hearts[i] = heart.GetComponent<Image>();
            }
        }

        StartCoroutine(CheckEnemies());
    }

    private IEnumerator CheckEnemies()
    {
        while (!hasAppeared)
        {
            yield return new WaitForSeconds(checkInterval);

            GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            if (enemies.Length == 0)
            {
                Appear();
            }
        }
    }

    private void Appear()
    {
        hasAppeared = true;

        // แสดงตัว
        spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;

        if (animator != null)
            animator.SetTrigger("Appear");
        EnemyCharged ai = GetComponent<EnemyCharged>();
        if (ai != null)
            ai.ActivateBehavior();

    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        UpdateHearts();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < currentHealth)
                hearts[i].enabled = true;
            else
                hearts[i].enabled = false;
        }
    }

    private void Die()
    {
        Debug.Log("💀 Boss ถูกทำลาย!");
        if (animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, 1.5f);
    }
}

using UnityEngine;
using System.Collections;

public class EnemyGiantFollow : MonoBehaviour
{
    private Animator animator;

    [Header("移動設定")]
    public float speed = 2f;
    public float stopDistance = 0.5f;

    [Header("スピード増加設定")]
    public float speedIncreaseRate = 0.5f;
    public float maxSpeed = 4.4f;

    [Header("プレイヤーのHPが1になった時の巨大化設定")]
    public float enlargeDelay = 0.5f;
    public float scaleMultiplier = 1.5f;
    public float enlargeSpeed = 2f;
    public float shrinkSpeed = 2f;

    [Header("攻撃アニメーション設定")]
    public float DestroyDelay = 1.2f;

    [Header("音設定")]
    private AudioSource audioSource;
    public AudioClip DieSound;
    public AudioClip AttackSound;

    [Header("敵タイプ（重複可能？）")]
    public bool canOverlap = false;

    private Transform target;
    private bool isAttacking = false;
    private bool isEnlarged = false;
    private bool isSpeedIncreasing = false;
    private float originalSpeed;
    private Vector3 originalScale;
    private bool isTriggerDelayedDestroy3 = false;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = gameObject.AddComponent<AudioSource>();
        originalSpeed = speed;
        originalScale = transform.localScale;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning("⚠️ Player not found for EnemyGiantFollow");
        }
    }

    void Update()
    {
        if (target == null) return;

        // 🔹 ป้องกันศัตรูซ้อนกัน
        if (!canOverlap)
            AvoidOtherEnemies();

        // 🔹 เร่งความเร็วเมื่อ Player HP ต่ำ
        if (isSpeedIncreasing)
        {
            speed += speedIncreaseRate * Time.deltaTime;
            speed = Mathf.Min(speed, maxSpeed);
        }

        Vector2 direction = (target.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, target.position);

        if (distance > stopDistance)
        {
            transform.position += (Vector3)direction * speed * Time.deltaTime;
        }

        if (direction.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(direction.x) * Mathf.Abs(originalScale.x);
            transform.localScale = scale;
        }

        animator.SetFloat("X", direction.x);
        animator.SetFloat("Y", direction.y);

        if (speed == maxSpeed)
        {
            if (!isAttacking)
            {
                animator.SetBool("Idle", false);
                animator.SetBool("Attack", false);
                animator.SetBool("Walk", false);
                animator.SetBool("Jump", true);
            }
        }
        else
        {
            if (!isAttacking)
                animator.SetBool("Jump", false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isAttacking)
        {
            StartCoroutine(Destroy());
        }
    }

    public void TriggerDelayedDestroy()
    {
        if (!isAttacking)
        {
            StartCoroutine(Destroy());
        }
    }

    public void TriggerDelayedDestroy3()
    {
        isTriggerDelayedDestroy3 = true;
        if (!isAttacking)
        {
            StartCoroutine(Destroy());
        }
    }

    private IEnumerator Destroy()
    {
        isAttacking = true;

        if (isSpeedIncreasing && !isTriggerDelayedDestroy3)
        {
            animator.SetBool("Attack", true);
            animator.SetBool("Idle", false);
            animator.SetBool("Walk", false);
            animator.SetBool("Die", false);
            if (AttackSound != null)
            {
                audioSource.PlayOneShot(AttackSound);
            }
        }
        else
        {
            animator.SetBool("Attack", false);
            animator.SetBool("Idle", false);
            animator.SetBool("Walk", false);
            animator.SetBool("Die", true);
            if (DieSound != null)
            {
                audioSource.PlayOneShot(DieSound);
            }
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            Physics2D.SyncTransforms();
        }

        yield return new WaitForSeconds(DestroyDelay);
        Destroy(gameObject);
    }

    public void OnPlayerLowHealth()
    {
        if (isEnlarged) return;
        StartCoroutine(EnlargeWithDelay());
        isSpeedIncreasing = true;

        if (!isAttacking)
        {
            animator.SetBool("Idle", false);
            animator.SetBool("Attack", false);
            animator.SetBool("Walk", true);
        }
    }

    public void OnPlayerNormalHealth()
    {
        if (!isEnlarged) return;
        StartCoroutine(ShrinkBackToNormal());
        isSpeedIncreasing = false;
        speed = originalSpeed;

        if (!isAttacking)
        {
            animator.SetBool("Idle", true);
            animator.SetBool("Attack", false);
            animator.SetBool("Walk", false);
        }
    }

    private IEnumerator EnlargeWithDelay()
    {
        yield return new WaitForSeconds(enlargeDelay);

        isEnlarged = true;
        float elapsed = 0f;
        Vector3 targetScale = originalScale * scaleMultiplier;

        while (elapsed < 1f)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed);
            elapsed += Time.deltaTime * enlargeSpeed;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private IEnumerator ShrinkBackToNormal()
    {
        float elapsed = 0f;
        Vector3 currentScale = transform.localScale;

        while (elapsed < 1f)
        {
            transform.localScale = Vector3.Lerp(currentScale, originalScale, elapsed);
            elapsed += Time.deltaTime * shrinkSpeed;
            yield return null;
        }

        transform.localScale = originalScale;
        isEnlarged = false;
    }
    private void AvoidOtherEnemies() // ป้องกันศัตรูซ้อนกัน
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float minDistance = 0.8f;

        foreach (GameObject other in enemies)
        {
            if (other == gameObject) continue;

            EnemyGiantFollow otherEnemy = other.GetComponent<EnemyGiantFollow>();
            if (otherEnemy != null && otherEnemy.canOverlap) continue; // 🔹 ถ้าอีกตัวอนุญาตให้ซ้อน → ข้าม

            float distance = Vector2.Distance(transform.position, other.transform.position);
            if (distance < minDistance && distance > 0.0001f)
            {
                Vector2 pushDir = (transform.position - other.transform.position).normalized;
                float pushStrength = (minDistance - distance) * 2f;
                transform.position += (Vector3)(pushDir * pushStrength * Time.deltaTime);
            }
        }
    }
}

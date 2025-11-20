using UnityEngine;
using System.Collections;

public class EnemyCharged : MonoBehaviour
{
    private Animator animator;
    private Transform target;

    [Header("Movement Settings")]
    public float speed = 2f;
    public float stopDistance = 0.5f;

    [Header("Charged Attack Settings")]
    public float chargedAttackInterval = 3f;
    public float chargedAttackDuration = 2.2f;
    public float expandHitDelay = 1.9f;
    public float attackRadius = 3f;
    public LayerMask playerLayer;

    [Header("Animation States")]
    public string walkAnim = "Walk";
    public string attackAnim = "Attack";
    public string chargedAnim = "ChargedAttack";
    public string dieAnim = "Die";
    public float Adelay = 0.5f;

    [Header("Visual Settings")]
    public Sprite circleSprite;
    public Color CircleColor = new Color(1f, 0f, 0f, 0.3f);
    public float circleVisibleTime = 0.25f;

    [Header("Explosion Effect")]
    public GameObject explosionEffectPrefab;
    public GameObject AttackPrefab;
    public Color attackColor = new Color(1f, 1f, 1f, 1f);
    public float delay = 5.0f;

    [Header("Audio Settings")]
    private AudioSource audioSource;
    public AudioClip AttackSound;
    public AudioClip ChargedAttackSound;
    public AudioClip DieSound;
    public AudioClip ExplosionSound;

    [Header("Enemy type (Can it be duplicated?)")]
    public bool canOverlap = false;

    private bool isAttacking = false;
    private bool isCharging = false;
    private bool isInvincible = false;
    private bool isMarkedForDestroy = false;
    private bool isTriggerDelayedDestroy2 = false;
    private Vector3 startScale;

    public void ActivateBehavior()
    {
        enabled = true;
        StartCoroutine(ChargedAttackLoop());
    }
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        startScale = transform.localScale;
        audioSource = gameObject.AddComponent<AudioSource>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
        enabled = false;
        StartCoroutine(ChargedAttackLoop());
    }

    void Update()
    {
        if (target == null || isCharging || isAttacking) return;

        Vector2 direction = (target.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, target.position);

        AvoidOtherEnemies();

        if (distance > stopDistance)
        {
            transform.position += (Vector3)direction * speed * Time.deltaTime;
            animator.SetBool(walkAnim, true);
            animator.SetBool(attackAnim, false);
        }

        if (direction.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Sign(direction.x) * Mathf.Abs(startScale.x);
            transform.localScale = scale;
        }

        animator.SetFloat("X", direction.x);
        animator.SetFloat("Y", direction.y);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCharging || isInvincible || isAttacking || isMarkedForDestroy) return;

        if (collision.CompareTag("Player"))
        {
            StartCoroutine(AttackAndDestroy());
        }
    }

    public void TriggerDelayedDestroy1()
    {
        if (isInvincible || isCharging || isAttacking || isMarkedForDestroy) return;
        StartCoroutine(AttackAndDestroy());
    }
    public void TriggerDelayedDestroy2()
    {
        isTriggerDelayedDestroy2 = true;
        if (isInvincible || isCharging || isAttacking || isMarkedForDestroy) return;
        StartCoroutine(AttackAndDestroy());
    }

    private IEnumerator AttackAndDestroy()
    {
        isAttacking = true;
        isMarkedForDestroy = true;

        if (DieSound != null)
            audioSource.PlayOneShot(DieSound);

        // เปลี่ยน collider ให้ไม่เป็น trigger เพื่อไม่ให้เกิด trigger ซ้ำขณะตาย
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            Physics2D.SyncTransforms();
        }

        if (!isTriggerDelayedDestroy2)
        {
            animator.SetBool(walkAnim, false);
            animator.SetBool(attackAnim, true);
        }
        else
        {
            Adelay = 1.25f;
            animator.SetBool(walkAnim, false);
            animator.SetBool(attackAnim, false);
            animator.SetBool(dieAnim, true);
        }

        if (AttackSound != null)
            audioSource.PlayOneShot(AttackSound);

        yield return new WaitForSeconds(Adelay);
        yield return new WaitForSeconds(0.3f);

        Destroy(gameObject);
    }

    private IEnumerator ChargedAttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(chargedAttackInterval);
            yield return StartCoroutine(DoChargedAttack());
        }
    }

    private IEnumerator DoChargedAttack()
    {
        isCharging = true;
        isInvincible = true;
        animator.SetBool(walkAnim, false);
        animator.SetBool(attackAnim, false);
        animator.SetTrigger(chargedAnim);

        if (ChargedAttackSound != null)
        {
            audioSource.PlayOneShot(ChargedAttackSound);
            // เล่น AttackSound หลัง delay (ปรับค่า delay ตามที่คุณต้องการ)
            StartCoroutine(PlayAttackAfterDelay(0.2f));
        }

        float timer = 0f;
        bool expanded = false;

        while (timer < chargedAttackDuration)
        {
            timer += Time.deltaTime;

            if (!expanded && timer >= expandHitDelay)
            {
                expanded = true;
                PerformChargedDamage();
            }

            yield return null;
        }

        isInvincible = false;
        isCharging = false;
        animator.ResetTrigger(chargedAnim);
        animator.SetBool(walkAnim, true);
    }

    private IEnumerator PlayAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (AttackSound != null)
            audioSource.PlayOneShot(AttackSound);
    }

    private void PerformChargedDamage()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRadius, playerLayer);
        if (hit != null && hit.CompareTag("Player"))
        {
            Debug.Log("💥 Player ถูกทำลายโดย Charged Attack!");
            // TODO: เรียกฟังก์ชันลด HP หรือทำลาย Player
        }

        StartCoroutine(ShowAttackRadiusEffect());
    }

    private IEnumerator ShowAttackRadiusEffect()
    {
        if (circleSprite == null)
        {
            yield break;
        }

        GameObject circle = new GameObject("AttackRadiusEffect");
        circle.transform.position = transform.position;

        SpriteRenderer renderer = circle.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = CircleColor;
        renderer.sortingOrder = 100;

        float spriteSize = renderer.sprite.bounds.size.x;
        float scaleFactor = (attackRadius * 2f) / spriteSize;
        circle.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

        yield return new WaitForSeconds(circleVisibleTime);

        // 🔹 แก้ไขส่วนนี้
        if (explosionEffectPrefab != null || AttackPrefab != null)
        {
            if (explosionEffectPrefab != null)
            {
                GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                Destroy(explosion, 0.65f);
            }

            if (AttackPrefab != null)
            {
                GameObject attackEffect = Instantiate(AttackPrefab, transform.position, Quaternion.identity);

                // ✅ ใช้ attackColor กับ SpriteRenderer ของ AttackPrefab
                SpriteRenderer attackRenderer = attackEffect.GetComponent<SpriteRenderer>();
                if (attackRenderer != null)
                {
                    attackRenderer.color = attackColor;
                }
                else
                {
                    // ถ้า AttackPrefab มีลูก (child) ให้ลองหาจากข้างใน
                    SpriteRenderer childRenderer = attackEffect.GetComponentInChildren<SpriteRenderer>();
                    if (childRenderer != null)
                        childRenderer.color = attackColor;
                }

                Destroy(attackEffect, delay);
            }
        }

        if (ExplosionSound != null)
            audioSource.PlayOneShot(ExplosionSound);

        Destroy(circle);
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    private void AvoidOtherEnemies()
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


    public bool IsInvincible => isInvincible;
}

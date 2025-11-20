using UnityEngine;
using TMPro;

namespace Minifantasy
{
    public class MyPlayerAttack : MonoBehaviour
    {
        [Header("UI 設定 / Kill Count UI")]
        public TextMeshProUGUI killTextTMP;
        public TextMeshProUGUI killText;

        private PlayerDirectionController directionController;

        [Header("攻撃設定 / Attack Settings")]
        public GameObject explosionEffect;
        public float AttackRange = 1.5f;
        public float HitboxRadius = 0.8f;
        public KeyCode attackKey = KeyCode.Return;

        public Senserzyusin sensor;
        public Animator animator;

        [Header("音設定 / Sound Settings")]
        private AudioSource audioSource;
        public AudioClip AttackSound;
        public AudioClip EnemyDieSound;

        private void Start()
        {
            directionController = GetComponent<PlayerDirectionController>() ?? GetComponentInChildren<PlayerDirectionController>();
            audioSource = gameObject.AddComponent<AudioSource>();
            UpdateKillUI();
        }

        void Update()
        {
            // โจมตีเมื่อกดปุ่ม, เซนเซอร์, หรือได้รับ ATTACK จาก ESP
            if (Input.GetKeyDown(attackKey) ||
                (sensor != null && sensor.ConsumeAttackTrigger()) ||
                (SerialManager.Instance != null && SerialManager.Instance.attackTriggered))
            {
                if (SerialManager.Instance != null)
                    SerialManager.Instance.ResetTrigger();

                TriggerAttack();

                if (SerialManager.Instance != null)
                    SerialManager.Instance.SendAttackToMaikon();
            }
        }

        void TriggerAttack()
        {
            if (AttackSound != null)
                audioSource.PlayOneShot(AttackSound);

            if (animator != null)
                animator.SetTrigger("Attack");

            Attack();
        }

        private void Attack()
        {
            if (directionController == null)
                return;

            Vector3 attackDir = GetAttackDirection(directionController.GetFacingDirection());
            if (attackDir == Vector3.zero)
                return;

            Vector3 attackPos = transform.position + attackDir * AttackRange;

            // เอฟเฟกต์ระเบิด
            if (explosionEffect != null)
                Instantiate(explosionEffect, attackPos, Quaternion.identity);

            // ตรวจศัตรูในรัศมีโจมตี
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, HitboxRadius);
            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Enemy") || hit.CompareTag("Enemy1"))
                {
                    if (EnemyDieSound != null)
                        audioSource.PlayOneShot(EnemyDieSound);

                    // ตรวจชนิดของ Enemy แล้วทำลาย
                    EnemyChargedFollow charged = hit.GetComponent<EnemyChargedFollow>();
                    EnemyGiantFollow giant = hit.GetComponent<EnemyGiantFollow>();

                    if (charged != null)
                        charged.TriggerDelayedDestroy2();
                    else if (giant != null)
                        giant.TriggerDelayedDestroy3();
                    else
                        Destroy(hit.gameObject);

                    // เพิ่มจำนวน kill
                    if (KillManager.Instance != null)
                    {
                        KillManager.Instance.AddKill();
                        UpdateKillUI();
                    }
                }
            }
        }

        private Vector3 GetAttackDirection(PlayerDirectionController.FacingDirection facing)
        {
            switch (facing)
            {
                case PlayerDirectionController.FacingDirection.Up: return Vector3.up;
                case PlayerDirectionController.FacingDirection.Down: return Vector3.down;
                case PlayerDirectionController.FacingDirection.Left: return Vector3.left;
                case PlayerDirectionController.FacingDirection.Right: return Vector3.right;
                case PlayerDirectionController.FacingDirection.UpRight: return (Vector3.up + Vector3.right).normalized;
                case PlayerDirectionController.FacingDirection.UpLeft: return (Vector3.up + Vector3.left).normalized;
                case PlayerDirectionController.FacingDirection.DownRight: return (Vector3.down + Vector3.right).normalized;
                case PlayerDirectionController.FacingDirection.DownLeft: return (Vector3.down + Vector3.left).normalized;
                default: return Vector3.zero;
            }
        }

        private void UpdateKillUI()
        {
            int kills = KillManager.Instance != null ? KillManager.Instance.TotalKills : 0;

            if (killTextTMP != null)
                killTextTMP.text = $"Kills: {kills}";
            if (killText != null)
                killText.text = $"You Kills: {kills}";
        }

        private void OnDrawGizmosSelected()
        {
            if (directionController == null) return;
            Vector3 dir = GetAttackDirection(directionController.GetFacingDirection());
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + dir * AttackRange, HitboxRadius);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy"))
            {
                if (KillManager.Instance != null)
                {
                    KillManager.Instance.AddKill();
                    UpdateKillUI();
                }
            }
        }
    }
}


using UnityEngine;

public class EnemyGuard : MonoBehaviour
{
    public Transform player;
    public float detectionRange = 5f;    // 通常の探知範囲
    public float moveSpeed = 3f;         // 移動速度
    public float chaseDuration = 10f;    // 追跡の最大時間（秒）
    public float reducedDetectionMultiplier = 0.5f; // 探知範囲の減少倍率（0.5 = 半分）

    private Vector2 startPosition;       // 敵の初期位置
    private bool isChasing = false;      // プレイヤーを追跡中かどうか
    private bool isReturning = false;    // 元の位置へ戻る中かどうか
    private float chaseTimer = 0f;       // 追跡時間のカウント
    private float originalDetectionRange; // 元の探知範囲を保存

    private void Start()
    {
        // Inspector で Player が未設定の場合、Tag から自動で検索する
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }

        startPosition = transform.position;
        originalDetectionRange = detectionRange;
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // プレイヤーが探知範囲内に入り、まだ追跡していない場合 → 追跡を開始
        if (!isChasing && !isReturning && distanceToPlayer <= detectionRange)
        {
            isChasing = true;
            chaseTimer = chaseDuration;

            //  一時的に探知範囲を減少させる
            detectionRange = originalDetectionRange * reducedDetectionMultiplier;
        }

        if (isChasing)
        {
            ChasePlayer();
            chaseTimer -= Time.deltaTime;

            if (chaseTimer <= 0)
            {
                isChasing = false;
                isReturning = true;
            }
        }
        else if (isReturning)
        {
            ReturnToStart();
        }
    }

    private void ChasePlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position,
                                                 player.position,
                                                 moveSpeed * Time.deltaTime);
    }

    private void ReturnToStart()
    {
        transform.position = Vector2.MoveTowards(transform.position,
                                                 startPosition,
                                                 moveSpeed * Time.deltaTime * 0.5f);

        // 元の位置に戻ったら → 探知範囲を通常に戻す
        if (Vector2.Distance(transform.position, startPosition) < 0.05f)
        {
            isReturning = false;
            detectionRange = originalDetectionRange;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

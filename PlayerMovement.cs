using UnityEngine;

namespace Minifantasy
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("移動設定")]
        public float speed = 5f;

        [Header("ステージの境界（CameraFollow があれば自動で取得）")]
        public Vector2 minBounds;
        public Vector2 maxBounds;

        private Vector3 startingPosition;
        private Vector2 motion;

        private Minifantasy.PlayerDirectionController directionController;
        private float colliderOffsetX = 0f;
        private float colliderOffsetY = 0f;

        void Start()
        {
            startingPosition = transform.position;
            directionController = GetComponent<Minifantasy.PlayerDirectionController>();

            //  コライダーのサイズを計算
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                colliderOffsetX = col.bounds.extents.x; // 幅の半分
                colliderOffsetY = col.bounds.extents.y; // 高さの半分
            }

            //  CameraFollow から境界を取得（存在する場合）
            CameraFollow cam = Camera.main?.GetComponent<CameraFollow>();
            if (cam != null)
            {
                minBounds = cam.minBounds;
                maxBounds = cam.maxBounds;
            }
        }

        void Update()
        {
            // --- キーボード入力を取得 ---
            Vector2 keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            // --- 移動量を計算 ---
            motion = keyboardInput.normalized * speed * Time.deltaTime;
            Vector3 newPosition = transform.position + (Vector3)motion;

            // --- コライダーがステージ外に出ないように制限 ---
            newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x + colliderOffsetX, maxBounds.x - colliderOffsetX);
            newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y + colliderOffsetY, maxBounds.y - colliderOffsetY);

            transform.position = newPosition;

            // --- 方向を更新 ---
            if (directionController != null && keyboardInput.magnitude > 0.1f)
                directionController.UpdateFacingFromExternal(keyboardInput.normalized);
        }

        public void ResetCamera()
        {
            transform.position = startingPosition;
        }
       
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}

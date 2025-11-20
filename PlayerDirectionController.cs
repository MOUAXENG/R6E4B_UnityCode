using UnityEngine;

namespace Minifantasy
{
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerDirectionController : MonoBehaviour
    {
        private PlayerMovement movement;
        private Vector2 lastMoveDir = Vector2.zero;

        [Header("Sprite หรือวัตถุที่ต้องหมุน (SpriteRenderer อยู่ในนี้)")]
        public Transform visualTransform;

        private int RotationAngle = 0;

        public enum FacingDirection
        {
            Up, Down, Left, Right,
            UpRight, UpLeft, DownRight, DownLeft,
            Idle
        }

        public FacingDirection currentDirection = FacingDirection.Down;

        private void Start()
        {
            movement = GetComponent<PlayerMovement>();

            if (visualTransform == null)
                visualTransform = transform;
        }

        private void Update()
        {
            // อ่านคีย์บอร์ด
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 moveDir = new Vector2(h, v).normalized;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                lastMoveDir = moveDir;
                UpdateFacing(moveDir);
            }
            else if (RotationAngle != 0)
            {
                // ไม่มีคีย์บอร์ด → ใช้ค่า Arduino
                UpdateFacingByAngle(RotationAngle);
            }
        }

        public void SetRotationAngle(int angle)
        {
            RotationAngle = angle;
            UpdateFacingByAngle(angle);
        }

        public void UpdateFacingFromExternal(Vector2 moveDir)
        {
            if (moveDir.sqrMagnitude > 0.01f)
            {
                lastMoveDir = moveDir;
                UpdateFacing(moveDir);
            }
        }

        private void UpdateFacing(Vector2 moveDir)
        {
            if (visualTransform == null) return;

            Quaternion targetRotation = Quaternion.identity;

            if (moveDir.x == 0 && moveDir.y == 0)
                currentDirection = FacingDirection.Idle;
            else if (moveDir.x > 0 && moveDir.y > 0)
                currentDirection = FacingDirection.UpRight;
            else if (moveDir.x > 0 && moveDir.y == 0)
                currentDirection = FacingDirection.Right;
            else if (moveDir.x > 0 && moveDir.y < 0)
                currentDirection = FacingDirection.DownRight;
            else if (moveDir.x == 0 && moveDir.y < 0)
                currentDirection = FacingDirection.Down;
            else if (moveDir.x < 0 && moveDir.y < 0)
                currentDirection = FacingDirection.DownLeft;
            else if (moveDir.x < 0 && moveDir.y == 0)
                currentDirection = FacingDirection.Left;
            else if (moveDir.x < 0 && moveDir.y > 0)
                currentDirection = FacingDirection.UpLeft;
            else if (moveDir.x == 0 && moveDir.y > 0)
                currentDirection = FacingDirection.Up;

            targetRotation = GetRotationForDirection(currentDirection);
            visualTransform.rotation = targetRotation;
        }

        private void UpdateFacingByAngle(int angle)
        {
            if (visualTransform == null) return;

            Quaternion targetRotation = Quaternion.identity;

            switch (angle)
            {
                case 1: currentDirection = FacingDirection.UpRight; targetRotation = Quaternion.Euler(0, 0, 0); break;
                case 2: currentDirection = FacingDirection.Right; targetRotation = Quaternion.Euler(0, 0, -45); break;
                case 3: currentDirection = FacingDirection.DownRight; targetRotation = Quaternion.Euler(0, 0, -90); break;
                case 4: currentDirection = FacingDirection.Down; targetRotation = Quaternion.Euler(0, 0, -135); break;
                case 5: currentDirection = FacingDirection.DownLeft; targetRotation = Quaternion.Euler(0, 0, -180); break;
                case 6: currentDirection = FacingDirection.Left; targetRotation = Quaternion.Euler(0, 0, 135); break;
                case 7: currentDirection = FacingDirection.UpLeft; targetRotation = Quaternion.Euler(0, 0, 90); break;
                case 8: currentDirection = FacingDirection.Up; targetRotation = Quaternion.Euler(0, 0, 45); break;
                default: currentDirection = FacingDirection.Idle; break;
            }

            visualTransform.rotation = targetRotation;
        }

        private Quaternion GetRotationForDirection(FacingDirection direction)
        {
            return direction switch
            {
                FacingDirection.UpRight => Quaternion.Euler(0, 0, 0),
                FacingDirection.Right => Quaternion.Euler(0, 0, -45),
                FacingDirection.DownRight => Quaternion.Euler(0, 0, -90),
                FacingDirection.Down => Quaternion.Euler(0, 0, -135),
                FacingDirection.DownLeft => Quaternion.Euler(0, 0, -180),
                FacingDirection.Left => Quaternion.Euler(0, 0, 135),
                FacingDirection.UpLeft => Quaternion.Euler(0, 0, 90),
                FacingDirection.Up => Quaternion.Euler(0, 0, 45),
                _ => visualTransform.rotation
            };
        }

        public FacingDirection GetFacingDirection() => currentDirection;
    }
}

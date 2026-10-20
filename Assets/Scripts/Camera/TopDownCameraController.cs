using UnityEngine;

namespace VoxelSouls.Camera
{
    /// <summary>
    /// Camera góc nhìn từ trên xuống (Top-Down / Isometric Action RPG):
    /// - Tự động bám theo Dũng Sĩ và căn khung hình bao quát cả Boss khi bật Lock-On
    /// - Hỗ trợ lăn chuột (Scroll Wheel) để phóng to / thu nhỏ góc nhìn
    /// - Hiệu ứng rung chấn màn hình (Screen Shake) khi Parry chuẩn hoặc tung Đòn Kết Liễu
    /// </summary>
    public class TopDownCameraController : MonoBehaviour
    {
        public static TopDownCameraController Instance { get; private set; }

        public Transform playerTarget;
        public Transform bossTarget;

        [Header("Top-Down Offset & Framing")]
        public Vector3 baseOffset = new Vector3(0f, 14.5f, -11.5f);
        public float followSmoothSpeed = 9f;
        public float zoomMultiplier = 1.0f;
        public float minZoom = 0.65f;
        public float maxZoom = 1.45f;

        private float _shakeTimer;
        private float _shakeIntensity;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            FindTargetsIfNeeded();
        }

        private void FindTargetsIfNeeded()
        {
            if (playerTarget == null)
            {
                Player.PlayerController pc = FindFirstObjectByType<Player.PlayerController>();
                if (pc != null) playerTarget = pc.transform;
            }

            if (bossTarget == null)
            {
                Boss.BossController bc = FindFirstObjectByType<Boss.BossController>();
                if (bc != null) bossTarget = bc.transform;
            }
        }

        private void LateUpdate()
        {
            FindTargetsIfNeeded();
            if (playerTarget == null) return;

            // Phóng to / thu nhỏ bằng con lăn chuột
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                zoomMultiplier = Mathf.Clamp(zoomMultiplier - scroll * 0.6f, minZoom, maxZoom);
            }

            Vector3 focusPoint = playerTarget.position;

            // Nếu đang giao chiến với Boss và khoảng cách gần, chia sẻ khung hình giữa Dũng Sĩ và Boss (25% về phía Boss)
            Player.PlayerController pc = playerTarget.GetComponent<Player.PlayerController>();
            if (pc != null && pc.isLockedOn && bossTarget != null)
            {
                float dist = Vector3.Distance(playerTarget.position, bossTarget.position);
                if (dist < 24f)
                {
                    focusPoint = Vector3.Lerp(playerTarget.position, bossTarget.position, 0.24f);
                }
            }

            Vector3 desiredPosition = focusPoint + baseOffset * zoomMultiplier;

            // Hiệu ứng rung chấn Camera (Screen Shake)
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                desiredPosition += Random.insideUnitSphere * _shakeIntensity;
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothSpeed * Time.unscaledDeltaTime);

            Vector3 lookDir = (focusPoint + Vector3.up * 0.9f) - transform.position;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 12f * Time.unscaledDeltaTime);
            }
        }

        public void ShakeCamera(float intensity = 0.25f, float duration = 0.18f)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }
    }
}

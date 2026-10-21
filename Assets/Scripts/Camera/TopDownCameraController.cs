using UnityEngine;

namespace VoxelSouls.Camera
{
    /// <summary>
    /// Camera Góc Nhìn Thứ Ba (Third-Person Over-the-Shoulder / Souls-like Camera chuẩn Sekiro & Elden Ring):
    /// - Đặt góc máy phía sau lưng Dũng Sĩ, giúp nhìn rõ tầm vóc khổng lồ và từng nhịp vung đại đao của Boss.
    /// - Khi bật Lock-On (Phím Tab / Chuột Giữa): Camera tự động khóa tâm nhìn từ sau vai Dũng Sĩ hướng thẳng vào Boss.
    /// - Khi tắt Lock-On (Free Look): Xoay chuột tự do 360 độ quanh nhân vật, lăn chuột (Scroll Wheel) để chỉnh khoảng cách.
    /// - Tích hợp chống xuyên sàn/tường và hiệu ứng rung chấn (Screen Shake) khi Parry hoặc Kết Liễu.
    /// </summary>
    public class TopDownCameraController : MonoBehaviour
    {
        public static TopDownCameraController Instance { get; private set; }

        public Transform playerTarget;
        public Transform bossTarget;

        [Header("Third-Person Camera Settings (Góc nhìn thứ ba Sekiro / Elden Ring)")]
        public Vector3 baseOffset = new Vector3(0f, 2.3f, -5.2f);
        public float distance = 5.2f;
        public float minDistance = 3.0f;
        public float maxDistance = 8.5f;
        public float heightOffset = 1.65f;
        public float shoulderOffset = 0.42f;
        public float mouseSensitivity = 3.2f;
        public float followSmoothSpeed = 14f;

        private float _yaw = 0f;
        private float _pitch = 16f;
        private float _shakeTimer;
        private float _shakeIntensity;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            FindTargetsIfNeeded();
            if (playerTarget != null)
            {
                _yaw = playerTarget.eulerAngles.y;
            }
            LockCursor(true);
        }

        private void FindTargetsIfNeeded()
        {
            if (playerTarget == null)
            {
                Player.PlayerController pc = FindObjectOfType<Player.PlayerController>();
                if (pc != null) playerTarget = pc.transform;
            }

            if (bossTarget == null)
            {
                Boss.BossController bc = FindObjectOfType<Boss.BossController>();
                if (bc != null) bossTarget = bc.transform;
            }
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void LateUpdate()
        {
            FindTargetsIfNeeded();
            if (playerTarget == null) return;

            // Cho phép nhấn Escape để hiện con trỏ chuột, hoặc click vào màn hình để khóa chuột lại
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(false);
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }

            // Phóng to / thu nhỏ khoảng cách góc nhìn thứ ba bằng con lăn chuột
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distance = Mathf.Clamp(distance - scroll * 2.5f, minDistance, maxDistance);
            }

            Player.PlayerController pc = playerTarget.GetComponent<Player.PlayerController>();
            Boss.BossController bossCtrl = bossTarget != null ? bossTarget.GetComponent<Boss.BossController>() : null;

            bool isLockedOnBoss = pc != null && pc.isLockedOn && bossCtrl != null && !bossCtrl.Stats.IsDead &&
                                  Vector3.Distance(playerTarget.position, bossTarget.position) < 32f;

            Vector3 pivotPos = playerTarget.position + Vector3.up * heightOffset;
            Quaternion camRotation;
            Vector3 lookTargetPos;

            if (isLockedOnBoss)
            {
                // Chế độ Lock-On chuẩn Sekiro / Elden Ring:
                // Đặt camera phía sau vai Dũng Sĩ và hướng trực diện về phía Boss để thấy rõ mọi đòn thế của Boss!
                Vector3 toBoss = (bossTarget.position + Vector3.up * 1.85f) - pivotPos;
                if (toBoss.sqrMagnitude < 0.01f) toBoss = playerTarget.forward;

                Quaternion targetRot = Quaternion.LookRotation(toBoss.normalized);
                Vector3 euler = targetRot.eulerAngles;
                _yaw = Mathf.LerpAngle(_yaw, euler.y, Time.unscaledDeltaTime * 10f);
                _pitch = Mathf.Lerp(_pitch, Mathf.Clamp(euler.x > 180f ? euler.x - 360f : euler.x, 8f, 28f), Time.unscaledDeltaTime * 8f);

                camRotation = Quaternion.Euler(_pitch, _yaw, 0f);
                lookTargetPos = Vector3.Lerp(pivotPos, bossTarget.position + Vector3.up * 2.1f, 0.55f);
            }
            else
            {
                // Chế độ Xoay Góc Nhìn Thứ Ba Tự Do bằng chuột (Free-Look Third-Person)
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    _yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                    _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                }
                _pitch = Mathf.Clamp(_pitch, -8f, 55f);

                camRotation = Quaternion.Euler(_pitch, _yaw, 0f);
                lookTargetPos = pivotPos + camRotation * Vector3.forward * 10f;
            }

            Vector3 rightShoulder = (camRotation * Vector3.right) * shoulderOffset;
            Vector3 desiredPosition = pivotPos + rightShoulder - (camRotation * Vector3.forward * distance);

            // Chống camera chui xuống dưới mặt sàn
            if (desiredPosition.y < 0.65f)
            {
                desiredPosition.y = 0.65f;
            }

            // Hiệu ứng rung chấn Camera (Screen Shake) khi Parry hoặc trúng đòn mạnh
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                desiredPosition += Random.insideUnitSphere * _shakeIntensity;
            }

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothSpeed * Time.unscaledDeltaTime);

            Vector3 finalLookDir = lookTargetPos - transform.position;
            if (finalLookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalLookDir.normalized), 16f * Time.unscaledDeltaTime);
            }
        }

        public void ShakeCamera(float intensity = 0.22f, float duration = 0.16f)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }
    }
}

using UnityEngine;
using VoxelSouls.Core;
using VoxelSouls.Stats;
using VoxelSouls.Visuals;

namespace VoxelSouls.Player
{
    /// <summary>
    /// Điều khiển Dũng Sĩ Góc Nhìn Thứ Ba (Third-Person Action RPG chuẩn Sekiro & Elden Ring):
    /// - Di chuyển WASD theo hướng góc nhìn Camera thứ ba
    /// - Khi bật Lock-On (Tab / Chuột Giữa): Luôn hướng mặt về phía Boss để sẵn sàng Parry và né đòn
    /// - Khi tắt Lock-On: Nhân vật tự động xoay mặt mượt mà theo hướng chạy (WASD) hoặc hướng Camera khi vung kiếm
    /// - Nhảy (Space) & Đạp Đầu Phá Thế trên không (Sekiro Jump Kick Counter)
    /// - Lộn nhào né đòn có i-frames (Phím V / Left Alt / Left Ctrl)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CombatStats))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 6.4f;
        public float sprintSpeed = 9.8f;
        public float rollSpeed = 13.5f;
        public float rollDuration = 0.36f;
        public float rotationSpeed = 16f;

        [Header("Jump & Gravity (Cơ chế Nhảy & Đạp Đầu Sekiro)")]
        public float jumpForce = 9.2f;
        public float gravity = -24f;
        public bool hasUsedJumpKick = false;

        [Header("Lock-On & References")]
        public bool isLockedOn = true;
        public Transform lockOnTarget;
        public VoxelCharacterVisuals visuals;

        private CharacterController _cc;
        private CombatStats _stats;
        private PlayerCombat _combat;
        private Vector3 _verticalVelocity;
        private bool _isRolling;
        private float _rollTimer;
        private Vector3 _rollDirection;
        private Vector3 _lastMoveInput;

        public bool IsGrounded => _cc != null && _cc.isGrounded;
        public bool IsRolling => _isRolling;
        public bool IsAirborne => !IsGrounded && transform.position.y > 0.35f;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _stats = GetComponent<CombatStats>();
            _combat = GetComponent<PlayerCombat>();

            _cc.height = 1.9f;
            _cc.center = new Vector3(0f, 0.95f, 0f);
            _cc.radius = 0.42f;

            if (visuals == null)
            {
                visuals = GetComponent<VoxelCharacterVisuals>();
                if (visuals == null)
                    visuals = gameObject.AddComponent<VoxelCharacterVisuals>();
            }
        }

        private void Start()
        {
            if (visuals.rootVisual == null)
            {
                visuals.BuildHeroVoxelMesh();
            }

            if (lockOnTarget == null)
            {
                Boss.BossController boss = FindObjectOfType<Boss.BossController>();
                if (boss != null)
                    lockOnTarget = boss.transform;
            }
        }

        private void Update()
        {
            if (_stats.IsDead) return;
            if (GameManager.Instance != null &&
                (GameManager.Instance.currentState == GameState.Victory || GameManager.Instance.currentState == GameState.Defeat))
                return;

            // Bật / tắt Lock-On vào Boss (Phím Tab hoặc Chuột Giữa)
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetMouseButtonDown(2))
            {
                isLockedOn = !isLockedOn;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SpawnFloatingText(
                        transform.position + Vector3.up * 2.2f,
                        isLockedOn ? "LOCK-ON: KHÓA MỤC TIÊU BOSS" : "LOCK-ON: GÓC NHÌN TỰ DO",
                        Color.cyan, 14, 0.8f);
                }
            }

            HandleRollState();
            HandleMovementAndJump();
            HandleRotation();
        }

        private void HandleRollState()
        {
            if (_isRolling)
            {
                _rollTimer -= Time.deltaTime;
                _cc.Move(_rollDirection * (rollSpeed * Time.deltaTime) + _verticalVelocity * Time.deltaTime);

                if (_rollTimer <= 0f)
                {
                    _isRolling = false;
                    _stats.isInvulnerable = false;
                }
                return;
            }

            // Kích hoạt Lộn Nhào Né Đòn (Dodge Roll - Phím V, Left Alt, hoặc Left Control)
            if ((Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.LeftControl))
                && !_stats.isPostureBroken)
            {
                if (_stats.ConsumeStamina(18f))
                {
                    Vector3 inputDir = GetCameraRelativeInput();
                    _rollDirection = inputDir.sqrMagnitude > 0.05f ? inputDir.normalized : transform.forward;
                    transform.rotation = Quaternion.LookRotation(_rollDirection);

                    _isRolling = true;
                    _rollTimer = rollDuration;
                    _stats.isInvulnerable = true; // Bất tử (i-frames) trong khi lộn nhào
                    visuals?.TriggerRollAnimation(rollDuration);
                }
            }
        }

        private void HandleMovementAndJump()
        {
            if (_isRolling) return;

            bool grounded = _cc.isGrounded;
            if (grounded && _verticalVelocity.y < 0f)
            {
                _verticalVelocity.y = -2f;
                hasUsedJumpKick = false;
            }

            Vector3 moveInput = GetCameraRelativeInput();
            _lastMoveInput = moveInput;
            bool isSprinting = Input.GetKey(KeyCode.LeftShift) && moveInput.sqrMagnitude > 0.1f && !_combat.IsBlockingOrParrying;

            float speed = walkSpeed;
            if (_stats.isPostureBroken)
            {
                speed = 0f;
            }
            else if (_combat != null && _combat.IsAttacking)
            {
                speed = walkSpeed * 0.45f;
            }
            else if (_combat != null && _combat.IsBlockingOrParrying)
            {
                speed = walkSpeed * 0.55f;
            }
            else if (isSprinting && _stats.ConsumeStamina(12f * Time.deltaTime))
            {
                speed = sprintSpeed;
            }

            // Nhảy (Space) & Đạp Đầu Phá Thế trên không (Sekiro High Monk / Jump Kick Counter)
            if (Input.GetKeyDown(KeyCode.Space) && !_stats.isPostureBroken)
            {
                if (grounded)
                {
                    if (_stats.ConsumeStamina(12f))
                    {
                        _verticalVelocity.y = jumpForce;
                    }
                }
                else if (!hasUsedJumpKick && lockOnTarget != null)
                {
                    // Kiểm tra nếu đang ở trên không gần Boss -> Đạp vào đầu Boss gây sát thương Thế (Posture) cực mạnh!
                    float distToBoss = Vector3.Distance(
                        new Vector3(transform.position.x, 0f, transform.position.z),
                        new Vector3(lockOnTarget.position.x, 0f, lockOnTarget.position.z));

                    if (distToBoss <= 4.5f)
                    {
                        hasUsedJumpKick = true;
                        _verticalVelocity.y = jumpForce * 0.85f;
                        Boss.BossController boss = lockOnTarget.GetComponent<Boss.BossController>();
                        if (boss != null && !boss.Stats.IsDead)
                        {
                            float bonusPosture = boss.IsDoingPerilousSweep ? 48f : 24f;
                            boss.ReceivePlayerAttack(12f, bonusPosture, true);
                            GameManager.Instance?.SpawnFloatingText(
                                boss.transform.position + Vector3.up * 3.5f,
                                boss.IsDoingPerilousSweep ? "ĐẠP PHÁ HIỂM CẢNH! (+48 THẾ)" : "ĐẠP PHÁ THẾ!",
                                new Color(1f, 0.85f, 0.2f), 18, 1.1f);
                            GameManager.Instance?.SpawnVoxelBurst(transform.position, new Color(1f, 0.85f, 0.25f), 16, 7f, 0.14f);
                            GameManager.Instance?.TriggerHitStop(0.15f, 0.09f);
                        }
                    }
                }
            }

            _verticalVelocity.y += gravity * Time.deltaTime;

            Vector3 finalVelocity = moveInput * speed + _verticalVelocity;
            _cc.Move(finalVelocity * Time.deltaTime);

            float normalizedMove = Mathf.Clamp01(moveInput.magnitude * (isSprinting ? 1.35f : 0.85f));
            visuals?.UpdateProceduralAnimation(normalizedMove, grounded);
        }

        private void HandleRotation()
        {
            if (_isRolling || _stats.isPostureBroken) return;

            // 1. Khi bật Lock-On và đang ở trong phạm vi chiến đấu với Boss -> Luôn hướng mặt về phía Boss
            if (isLockedOn && lockOnTarget != null)
            {
                Boss.BossController boss = lockOnTarget.GetComponent<Boss.BossController>();
                if (boss != null && !boss.Stats.IsDead && Vector3.Distance(transform.position, lockOnTarget.position) < 32f)
                {
                    Vector3 dirToBoss = lockOnTarget.position - transform.position;
                    dirToBoss.y = 0f;
                    if (dirToBoss.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dirToBoss.normalized);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                        return;
                    }
                }
            }

            // 2. Ở góc nhìn thứ ba tự do: Khi đang tấn công hoặc đỡ đòn, xoay theo hướng nhìn của Camera
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (_combat != null && (_combat.IsAttacking || _combat.IsBlockingOrParrying) && cam != null)
            {
                Vector3 camForward = cam.transform.forward;
                camForward.y = 0f;
                if (camForward.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camForward.normalized), rotationSpeed * 1.4f * Time.deltaTime);
                    return;
                }
            }

            // 3. Khi chạy tự do: Xoay mặt mượt mà theo hướng di chuyển WASD (chuẩn Elden Ring / Sekiro)
            if (_lastMoveInput.sqrMagnitude > 0.02f)
            {
                Quaternion moveRot = Quaternion.LookRotation(_lastMoveInput.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, moveRot, rotationSpeed * Time.deltaTime);
            }
        }

        public Vector3 GetCameraRelativeInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 raw = new Vector3(h, 0f, v);
            if (raw.sqrMagnitude < 0.01f) return Vector3.zero;

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) return raw.normalized;

            Vector3 camFwd = cam.transform.forward;
            Vector3 camRight = cam.transform.right;
            camFwd.y = 0f;
            camRight.y = 0f;
            camFwd.Normalize();
            camRight.Normalize();

            return (camFwd * v + camRight * h).normalized;
        }
    }
}

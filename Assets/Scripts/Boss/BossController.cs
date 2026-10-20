using System.Collections;
using UnityEngine;
using VoxelSouls.Combat;
using VoxelSouls.Core;
using VoxelSouls.Stats;
using VoxelSouls.Visuals;

namespace VoxelSouls.Boss
{
    public enum BossState
    {
        IdleDormant,
        Chasing,
        Attacking,
        PostureBroken,
        Dead
    }

    /// <summary>
    /// AI Boss đầu tiên: "HỘ VỆ HẮC THẠCH (OBSIDIAN WARDEN)"
    /// Thiết kế kết hợp tinh hoa của Sekiro & Elden Ring:
    /// - Có Thanh Máu (HP) và Thanh Thế Chịu Đựng (Posture Bar) + 2 Mạng Kết Liễu (Phase 1 & Phase 2).
    /// - Các đòn đánh có nhịp cảnh báo rõ ràng để người chơi Parry (Chuột Phải), Nhảy Đạp Đầu (Space) hoặc Lộn Né (V/Alt/Ctrl):
    ///   1. Combo 3 Nhịp Đại Đao (Đỡ đòn / Parry liên hoàn Keng-Keng-Keng!)
    ///   2. Đòn Đâm Xung Kích (Shadow Thrust)
    ///   3. [危 HIỂM CẢNH] Đòn Quét Ngang 360 Độ (Unblockable Sweep — Nhảy lên bằng Space và bấm Space lần 2 để Đạp Đầu Boss!)
    ///   4. [危 HIỂM CẢNH] Đập Búa Dung Nham (Unblockable Slam — Lộn nhào né khỏi vùng nổ!)
    ///   5. [Phase 2] Phép Liên Hoàn Hắc Tinh (Có thể Parry bật ngược đạn phép về phía Boss!)
    /// </summary>
    [RequireComponent(typeof(CombatStats))]
    public class BossController : MonoBehaviour
    {
        [Header("Boss Identity")]
        public string bossName = "HỘ VỆ HẮC THẠCH — OBSIDIAN WARDEN";
        public int currentPhase = 1;
        public int maxPhases = 2; // 2 chấm đỏ kiểu Sekiro (Hoặc Sang Phase 2 khi hết cây máu 1)

        [Header("Movement & Detection")]
        public float moveSpeed = 4.6f;
        public float aggroRange = 19f;
        public float attackRange = 3.8f;

        [Header("Combat Alerts")]
        public bool showPerilousKanji = false; // Hiển thị chữ 危 (Hiểm Cảnh) màu đỏ rực
        public string currentTelegraphText = "";
        public bool IsDoingPerilousSweep { get; private set; }

        public BossState state = BossState.IdleDormant;
        public VoxelCharacterVisuals visuals;

        private CombatStats _stats;
        private Player.PlayerCombat _player;
        private float _actionCooldown = 1.0f;
        private int _patternCounter = 0;
        private Coroutine _activeAttackRoutine;

        public CombatStats Stats => _stats;

        private void Awake()
        {
            _stats = GetComponent<CombatStats>();
            _stats.maxHP = 450f;
            _stats.currentHP = 450f;
            _stats.maxPosture = 120f;
            _stats.currentPosture = 0f;
            _stats.postureDecayRate = 7.5f;
            _stats.postureBrokenDuration = 3.8f;

            _stats.OnPostureBroken += HandlePostureBroken;
            _stats.OnPostureRecovered += HandlePostureRecovered;
            _stats.OnDeath += HandleBossDeath;

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
                visuals.BuildBossVoxelMesh();
            }
            _player = FindObjectOfType<Player.PlayerCombat>();
        }

        private void Update()
        {
            if (state == BossState.Dead) return;
            if (_player == null || _player.Stats.IsDead) return;

            float distToPlayer = Vector3.Distance(transform.position, _player.transform.position);

            if (state == BossState.IdleDormant)
            {
                visuals?.UpdateProceduralAnimation(0f, true);
                if (distToPlayer <= aggroRange)
                {
                    WakeUpBoss();
                }
                return;
            }

            if (state == BossState.PostureBroken)
            {
                visuals?.SetPostureBrokenStance(true);
                return;
            }

            if (state == BossState.Attacking)
            {
                visuals?.UpdateProceduralAnimation(0.1f, true);
                return;
            }

            // Trạng thái Chasing (Áp sát Dũng Sĩ)
            _actionCooldown -= Time.deltaTime;
            FacePlayer(8f);

            if (distToPlayer > attackRange * 0.88f)
            {
                Vector3 dir = (_player.transform.position - transform.position).normalized;
                dir.y = 0f;
                float speedMult = (currentPhase == 2) ? 1.22f : 1.0f;
                transform.position += dir * (moveSpeed * speedMult * Time.deltaTime);
                visuals?.UpdateProceduralAnimation(0.8f, true);

                // Nếu ở xa và đã hồi chiêu, Boss có thể tung đòn Lao Đâm hoặc Chưởng Phép
                if (_actionCooldown <= 0f && distToPlayer <= 12f && distToPlayer >= 5.5f)
                {
                    StartAttackPattern(chooseLongRange: true);
                }
            }
            else
            {
                visuals?.UpdateProceduralAnimation(0f, true);
                if (_actionCooldown <= 0f)
                {
                    StartAttackPattern(chooseLongRange: false);
                }
            }
        }

        public void WakeUpBoss()
        {
            if (state != BossState.IdleDormant) return;
            state = BossState.Chasing;
            GameManager.Instance?.TriggerBossFight();
            GameManager.Instance?.ShowAreaBanner(
                bossName,
                "HÃY DÙNG [CHUỘT PHẢI] ĐỂ PARRY HOẶC [SPACE] NHẢY QUA ĐÒN HIỂM CẢNH (危)",
                new Color(1f, 0.35f, 0.2f));
        }

        private void FacePlayer(float turnSpeed)
        {
            if (_player == null) return;
            Vector3 dir = _player.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), turnSpeed * Time.deltaTime);
            }
        }

        private void StartAttackPattern(bool chooseLongRange)
        {
            if (_activeAttackRoutine != null)
                StopCoroutine(_activeAttackRoutine);

            _patternCounter++;

            if (chooseLongRange)
            {
                if (currentPhase == 2 && (_patternCounter % 2 == 0))
                    _activeAttackRoutine = StartCoroutine(PatternArcaneVolley());
                else
                    _activeAttackRoutine = StartCoroutine(PatternShadowThrust());
                return;
            }

            int choice = _patternCounter % 4;
            if (choice == 0)
                _activeAttackRoutine = StartCoroutine(PatternThreeHitCombo());
            else if (choice == 1)
                _activeAttackRoutine = StartCoroutine(PatternPerilousSweep());
            else if (choice == 2)
                _activeAttackRoutine = StartCoroutine(PatternThreeHitCombo());
            else
                _activeAttackRoutine = StartCoroutine(PatternPerilousMagmaSlam());
        }

        /// <summary>
        /// Chiêu 1: Combo 3 Nhịp Đại Đao — Cực kỳ lý tưởng để người chơi luyện nhịp Parry liên hoàn!
        /// </summary>
        private IEnumerator PatternThreeHitCombo()
        {
            state = BossState.Attacking;
            currentTelegraphText = "Combo Đại Đao (Hãy bấm Chuột Phải theo nhịp!)";

            for (int hit = 1; hit <= 3; hit++)
            {
                FacePlayer(18f);
                float windup = (hit == 3) ? 0.48f : 0.35f;
                visuals?.TriggerAttackAnimation(hit, windup + 0.15f);

                // Tia sáng báo hiệu sắp vung kiếm
                GameManager.Instance?.SpawnVoxelBurst(
                    transform.position + Vector3.up * 2.4f + transform.forward * 1.2f,
                    new Color(1f, 0.75f, 0.25f), 6, 3.5f, 0.1f);

                yield return new WaitForSeconds(windup);

                // Tiến nhẹ về phía trước khi chém
                transform.position += transform.forward * 0.85f;
                CheckMeleeHitOnPlayer(
                    hpDmg: (hit == 3) ? 34f : 22f,
                    postureDmg: (hit == 3) ? 32f : 20f,
                    range: 4.3f,
                    isUnblockable: false);

                yield return new WaitForSeconds(0.22f);
            }

            currentTelegraphText = "";
            state = BossState.Chasing;
            _actionCooldown = (currentPhase == 2) ? 1.15f : 1.55f;
        }

        /// <summary>
        /// Chiêu 2: [危 HIỂM CẢNH] Quét Ngang 360 Độ — Không thể Đỡ! Phải Nhảy (Space) qua lưỡi kiếm!
        /// </summary>
        private IEnumerator PatternPerilousSweep()
        {
            state = BossState.Attacking;
            showPerilousKanji = true;
            IsDoingPerilousSweep = true;
            currentTelegraphText = "危 ĐÒN QUÉT HIỂM CẢNH — Nhấn [SPACE] Nhảy lên & [SPACE] Đạp Đầu!";

            // Cảnh báo đỏ rực chuẩn Sekiro
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 3.2f, new Color(1f, 0.12f, 0.12f), 18, 6.5f, 0.16f);
            FacePlayer(15f);

            yield return new WaitForSeconds(0.68f);

            visuals?.TriggerAttackAnimation(5, 0.45f);
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 0.5f, new Color(1f, 0.25f, 0.1f), 22, 8f, 0.18f);

            // Nếu Dũng Sĩ đang nhảy trên không (IsAirborne), đòn quét sát đất sẽ trượt hoàn toàn!
            Player.PlayerController pc = _player.GetComponent<Player.PlayerController>();
            if (pc != null && !pc.IsAirborne)
            {
                float dist = Vector3.Distance(transform.position, _player.transform.position);
                if (dist <= 4.7f)
                {
                    _player.ReceiveIncomingAttack(42f, 45f, transform.position, isUnblockablePerilous: true, this);
                }
            }
            else if (pc != null && pc.IsAirborne)
            {
                GameManager.Instance?.SpawnFloatingText(
                    _player.transform.position + Vector3.up * 2.2f,
                    "NHẢY NÉ ĐÒN QUÉT! (Nhấn [SPACE] để Đạp Đầu Boss!)",
                    new Color(0.35f, 1f, 0.55f), 15, 1.0f);
            }

            yield return new WaitForSeconds(0.45f);
            showPerilousKanji = false;
            IsDoingPerilousSweep = false;
            currentTelegraphText = "";
            state = BossState.Chasing;
            _actionCooldown = 1.6f;
        }

        /// <summary>
        /// Chiêu 3: [危 HIỂM CẢNH] Đập Búa Dung Nham — Nổ diện rộng, phải Lộn Nhào (V/Alt/Ctrl) hoặc chạy thoát!
        /// </summary>
        private IEnumerator PatternPerilousMagmaSlam()
        {
            state = BossState.Attacking;
            showPerilousKanji = true;
            currentTelegraphText = "危 ĐẬP BÚA DUNG NHAM — Nhấn [V / Alt / Ctrl] Lộn Nhào Né!";

            FacePlayer(16f);
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 3.4f, new Color(1f, 0.3f, 0.05f), 16, 6f, 0.16f);

            yield return new WaitForSeconds(0.72f);

            visuals?.TriggerAttackAnimation(3, 0.45f);
            Vector3 slamPos = transform.position + transform.forward * 2.4f;
            GameManager.Instance?.SpawnVoxelBurst(slamPos, new Color(1f, 0.35f, 0.05f), 28, 9.5f, 0.22f);
            Camera.TopDownCameraController.Instance?.ShakeCamera(0.35f, 0.25f);

            float dist = Vector3.Distance(slamPos, _player.transform.position);
            if (dist <= 3.6f)
            {
                _player.ReceiveIncomingAttack(48f, 50f, slamPos, isUnblockablePerilous: true, this);
            }

            showPerilousKanji = false;
            yield return new WaitForSeconds(0.45f);
            currentTelegraphText = "";
            state = BossState.Chasing;
            _actionCooldown = 1.75f;
        }

        /// <summary>
        /// Chiêu 4: Đâm Xung Kích Tầm Xa (Có thể Parry để làm Boss khựng lại!)
        /// </summary>
        private IEnumerator PatternShadowThrust()
        {
            state = BossState.Attacking;
            currentTelegraphText = "Đâm Xung Kích (Chuột Phải để Parry!)";
            FacePlayer(20f);

            yield return new WaitForSeconds(0.42f);

            visuals?.TriggerAttackAnimation(4, 0.38f);
            Vector3 dashDir = (_player.transform.position - transform.position).normalized;
            dashDir.y = 0f;

            float elapsed = 0f;
            bool hitChecked = false;
            while (elapsed < 0.26f)
            {
                elapsed += Time.deltaTime;
                transform.position += dashDir * (16f * Time.deltaTime);
                if (!hitChecked && Vector3.Distance(transform.position, _player.transform.position) <= 3.6f)
                {
                    hitChecked = true;
                    CheckMeleeHitOnPlayer(28f, 26f, 4.0f, isUnblockable: false);
                }
                yield return null;
            }

            currentTelegraphText = "";
            state = BossState.Chasing;
            _actionCooldown = 1.35f;
        }

        /// <summary>
        /// Chiêu 5 (Phase 2): Bắn 3 Cầu Năng Lượng Hắc Ám (Người chơi có thể Parry bật ngược lại!)
        /// </summary>
        private IEnumerator PatternArcaneVolley()
        {
            state = BossState.Attacking;
            currentTelegraphText = "Phép Hắc Tinh (Có thể Parry bật ngược đạn!)";

            for (int i = 0; i < 3; i++)
            {
                FacePlayer(18f);
                visuals?.TriggerAttackAnimation(1 + (i % 2), 0.3f);

                GameObject orb = new GameObject("BossDarkOrb");
                orb.transform.position = transform.position + transform.forward * 1.8f + Vector3.up * 1.4f;
                MagicProjectile proj = orb.AddComponent<MagicProjectile>();
                Vector3 dir = (_player.transform.position - transform.position).normalized;
                proj.Initialize(dir, SpellType.GlintstoneComet, fromEnemy: true, dmgMultiplier: 1.15f, target: _player.transform);

                yield return new WaitForSeconds(0.32f);
            }

            currentTelegraphText = "";
            state = BossState.Chasing;
            _actionCooldown = 1.5f;
        }

        private void CheckMeleeHitOnPlayer(float hpDmg, float postureDmg, float range, bool isUnblockable)
        {
            if (_player == null || _player.Stats.IsDead) return;

            float dist = Vector3.Distance(transform.position, _player.transform.position);
            if (dist <= range)
            {
                _player.ReceiveIncomingAttack(hpDmg, postureDmg, transform.position, isUnblockable, this);
            }
        }

        public void OnParriedByPlayer(float postureDmgToBoss)
        {
            _stats.ApplyDamage(0f, postureDmgToBoss, canBreakPosture: true);
        }

        public void ReceivePlayerAttack(float hpDamage, float postureDamage, bool isHeavyOrStagger)
        {
            if (state == BossState.Dead) return;
            if (state == BossState.IdleDormant) WakeUpBoss();

            _stats.ApplyDamage(hpDamage, postureDamage, canBreakPosture: true);

            GameManager.Instance?.SpawnVoxelBurst(
                transform.position + Vector3.up * 1.8f,
                new Color(1f, 0.45f, 0.15f), 10, 5.5f, 0.14f);

            GameManager.Instance?.SpawnFloatingText(
                transform.position + Vector3.up * 3.1f,
                $"-{Mathf.RoundToInt(hpDamage)} HP  (+{Mathf.RoundToInt(postureDamage)} Thế)",
                new Color(1f, 0.72f, 0.25f), 15, 0.85f);

            // Kiểm tra chuyển sang Phase 2 khi máu dưới 55%
            if (currentPhase == 1 && _stats.currentHP <= _stats.maxHP * 0.55f && !_stats.IsDead)
            {
                EnterPhaseTwo();
            }
        }

        private void EnterPhaseTwo()
        {
            currentPhase = 2;
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 2.0f, new Color(1f, 0.2f, 0.05f), 32, 10f, 0.22f);
            Camera.TopDownCameraController.Instance?.ShakeCamera(0.4f, 0.3f);
            GameManager.Instance?.ShowAreaBanner(
                "GIAI ĐOẠN 2: HẮC HỎA CUỒNG NỘ",
                "Hộ Vệ Hắc Thạch thức tỉnh sức mạnh Cổ Thần!",
                new Color(1f, 0.28f, 0.1f));
        }

        private void HandlePostureBroken()
        {
            if (_activeAttackRoutine != null)
                StopCoroutine(_activeAttackRoutine);

            showPerilousKanji = false;
            IsDoingPerilousSweep = false;
            state = BossState.PostureBroken;
            currentTelegraphText = "◉ BOSS BỊ PHÁ THẾ! NHẤN [CHUỘT TRÁI] ĐỂ KẾT LIỄU (DEATHBLOW)!";
            visuals?.SetPostureBrokenStance(true);

            GameManager.Instance?.TriggerHitStop(0.18f, 0.14f);
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 2.2f, new Color(1f, 0.85f, 0.2f), 24, 8f, 0.18f);
            GameManager.Instance?.SpawnFloatingText(
                transform.position + Vector3.up * 3.6f,
                "💥 POSTURE BROKEN! [CHUỘT TRÁI] KẾT LIỄU!",
                new Color(1f, 0.25f, 0.25f), 20, 2.2f);
        }

        private void HandlePostureRecovered()
        {
            if (state == BossState.Dead) return;
            visuals?.SetPostureBrokenStance(false);
            state = BossState.Chasing;
            currentTelegraphText = "";
            _actionCooldown = 0.9f;
        }

        public void ExecuteDeathblowOnBoss(float massiveDamage)
        {
            _stats.ConsumePostureBreakOnDeathblow(massiveDamage);
            if (currentPhase == 1 && !_stats.IsDead)
            {
                EnterPhaseTwo();
            }
        }

        private void HandleBossDeath()
        {
            if (state == BossState.Dead) return;
            if (_activeAttackRoutine != null)
                StopCoroutine(_activeAttackRoutine);

            showPerilousKanji = false;
            IsDoingPerilousSweep = false;
            state = BossState.Dead;
            currentTelegraphText = "";

            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 2.0f, new Color(1f, 0.82f, 0.25f), 40, 11f, 0.24f);
            if (visuals != null && visuals.rootVisual != null)
            {
                visuals.rootVisual.localRotation = Quaternion.Euler(-85f, 0f, 0f);
            }

            GameManager.Instance?.OnBossDefeated();
        }
    }
}

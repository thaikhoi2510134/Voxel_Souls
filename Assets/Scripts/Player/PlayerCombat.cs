using System.Collections;
using UnityEngine;
using VoxelSouls.Combat;
using VoxelSouls.Core;
using VoxelSouls.Stats;
using VoxelSouls.Visuals;

namespace VoxelSouls.Player
{
    /// <summary>
    /// Hệ thống Chiến Đấu Dũng Sĩ kết hợp Sekiro & Elden Ring:
    /// - Chuột Trái (LMB): Combo 3 đòn kiếm HOẶC Đòn Kết Liễu (Deathblow) khi Boss bị Phá Thế
    /// - Chuột Phải (RMB): Đỡ đòn (Block) & Đỡ Đòn Hoàn Hảo (Perfect Parry / Deflect chuẩn Sekiro trong khung 0.25s)
    /// - Phím 1 / 2 / 3: Đổi phép thuật Elden Ring (Cung Sao Băng, Hỏa Cầu, Lôi Thương)
    /// - Phím Q hoặc E: Tung phép thuật hiện tại (tiêu hao FP)
    /// - Phím R: Uống Bình Máu Đỏ (Crimson Tear Flask)
    /// - Phím F: Uống Bình Năng Lượng Xanh (Cerulean Tear Flask)
    /// </summary>
    [RequireComponent(typeof(CombatStats))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Melee Attack (Kiếm Thuật)")]
        public float baseAttackDamage = 24f;
        public float basePostureDamage = 22f;
        public float attackRange = 3.1f;
        public float deathblowDamage = 220f;

        [Header("Sekiro Deflect / Parry Settings")]
        public float perfectParryWindow = 0.25f;
        public float deflectBossPostureDamage = 38f;

        [Header("Elden Ring Spells & Flasks")]
        public SpellType currentSpell = SpellType.GlintstoneComet;
        public int crimsonFlasks = 4;
        public int maxCrimsonFlasks = 4;
        public int ceruleanFlasks = 3;
        public int maxCeruleanFlasks = 3;
        public int runesCollected = 0;

        private CombatStats _stats;
        private PlayerController _controller;
        private VoxelCharacterVisuals _visuals;

        private int _comboStep = 0;
        private float _lastAttackTime;
        private float _attackCooldownTimer;
        private bool _isBlocking;
        private float _parryPressTime = -10f;

        public bool IsAttacking => _attackCooldownTimer > 0f;
        public bool IsBlockingOrParrying => _isBlocking;
        public bool IsInPerfectParryWindow => _isBlocking && (Time.time - _parryPressTime <= perfectParryWindow);
        public CombatStats Stats => _stats;

        private void Awake()
        {
            _stats = GetComponent<CombatStats>();
            _controller = GetComponent<PlayerController>();
            _visuals = GetComponent<VoxelCharacterVisuals>();

            _stats.maxHP = 140f;
            _stats.currentHP = 140f;
            _stats.maxFP = 100f;
            _stats.currentFP = 100f;
            _stats.maxStamina = 120f;
            _stats.currentStamina = 120f;
            _stats.maxPosture = 100f;

            _stats.OnDeath += HandlePlayerDeath;
        }

        private void Update()
        {
            if (_stats.IsDead) return;
            if (GameManager.Instance != null &&
                (GameManager.Instance.currentState == GameState.Victory || GameManager.Instance.currentState == GameState.Defeat))
                return;

            if (_attackCooldownTimer > 0f)
                _attackCooldownTimer -= Time.deltaTime;

            if (Time.time - _lastAttackTime > 1.1f)
                _comboStep = 0;

            HandleParryAndBlockInput();
            HandleMeleeInput();
            HandleSpellInput();
            HandleFlaskInput();
        }

        private void HandleParryAndBlockInput()
        {
            // Nhấn Chuột Phải để vào thế Đỡ đòn / Căn nhịp Parry (Sekiro Deflect)
            if (Input.GetMouseButtonDown(1) && !_stats.isPostureBroken)
            {
                _isBlocking = true;
                _parryPressTime = Time.time;
                _visuals?.SetParryStance(true);
            }
            else if (Input.GetMouseButton(1) && !_stats.isPostureBroken)
            {
                _isBlocking = true;
                _visuals?.SetParryStance(true);
            }
            else if (_isBlocking)
            {
                _isBlocking = false;
                _visuals?.SetParryStance(false);
            }
        }

        private void HandleMeleeInput()
        {
            if (Input.GetMouseButtonDown(0) && _attackCooldownTimer <= 0f && !_controller.IsRolling && !_stats.isPostureBroken)
            {
                // Kiểm tra xem Boss có đang bị Phá Thế (Posture Broken) để tung ĐÒN KẾT LIỄU (Deathblow) không!
                Boss.BossController boss = FindObjectOfType<Boss.BossController>();
                if (boss != null && !boss.Stats.IsDead && boss.Stats.isPostureBroken)
                {
                    float dist = Vector3.Distance(transform.position, boss.transform.position);
                    if (dist <= 4.6f)
                    {
                        StartCoroutine(ExecuteDeathblowRoutine(boss));
                        return;
                    }
                }

                // Chém Combo 3 đòn thường
                if (!_stats.ConsumeStamina(15f)) return;

                _comboStep = (_comboStep % 3) + 1;
                _lastAttackTime = Time.time;
                _attackCooldownTimer = (_comboStep == 3) ? 0.46f : 0.32f;

                _visuals?.TriggerAttackAnimation(_comboStep, _attackCooldownTimer);
                StartCoroutine(PerformMeleeHitboxRoutine(_comboStep));
            }
        }

        private IEnumerator PerformMeleeHitboxRoutine(int step)
        {
            yield return new WaitForSeconds(0.11f);

            float dmgMult = (step == 3) ? 1.45f : 1.0f;
            float hpDmg = baseAttackDamage * dmgMult * _stats.attackDamageMultiplier;
            float postureDmg = basePostureDamage * dmgMult * _stats.attackDamageMultiplier;

            Vector3 hitCenter = transform.position + transform.forward * 1.65f + Vector3.up * 1.0f;
            Collider[] hits = Physics.OverlapSphere(hitCenter, attackRange * 0.65f);

            foreach (Collider hit in hits)
            {
                Boss.BossController boss = hit.GetComponentInParent<Boss.BossController>();
                if (boss != null && !boss.Stats.IsDead)
                {
                    boss.ReceivePlayerAttack(hpDmg, postureDmg, step == 3);
                    break;
                }

                World.TrainingDummy dummy = hit.GetComponentInParent<World.TrainingDummy>();
                if (dummy != null)
                {
                    dummy.OnHit(hpDmg, postureDmg);
                    break;
                }
            }
        }

        private IEnumerator ExecuteDeathblowRoutine(Boss.BossController boss)
        {
            _attackCooldownTimer = 0.75f;
            _stats.isInvulnerable = true;

            // Lao tới sát trước mặt Boss
            Vector3 dirToBoss = (boss.transform.position - transform.position).normalized;
            dirToBoss.y = 0f;
            transform.rotation = Quaternion.LookRotation(dirToBoss);

            _visuals?.TriggerAttackAnimation(4, 0.6f);

            GameManager.Instance?.TriggerHitStop(0.12f, 0.22f);
            GameManager.Instance?.SpawnVoxelBurst(boss.transform.position + Vector3.up * 1.8f, new Color(1f, 0.15f, 0.15f), 28, 9.5f, 0.22f);
            GameManager.Instance?.SpawnVoxelBurst(boss.transform.position + Vector3.up * 2.0f, new Color(1f, 0.85f, 0.25f), 18, 8.0f, 0.18f);

            Camera.TopDownCameraController.Instance?.ShakeCamera(0.45f, 0.35f);

            float finalDeathblowDmg = deathblowDamage * _stats.attackDamageMultiplier;
            boss.ExecuteDeathblowOnBoss(finalDeathblowDmg);

            GameManager.Instance?.SpawnFloatingText(
                boss.transform.position + Vector3.up * 3.8f,
                $"⚔ ĐÒN KẾT LIỄU! (-{Mathf.RoundToInt(finalDeathblowDmg)} HP)",
                new Color(1f, 0.22f, 0.22f), 22, 1.5f);

            yield return new WaitForSeconds(0.55f);
            _stats.isInvulnerable = false;
        }

        private void HandleSpellInput()
        {
            // Phím 1, 2, 3 để chọn Phép thuật Elden Ring
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSpell(SpellType.GlintstoneComet);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSpell(SpellType.FellFireball);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSpell(SpellType.LightningSpear);

            // Phím Q hoặc E để tung Phép thuật
            if ((Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
                && _attackCooldownTimer <= 0f && !_controller.IsRolling && !_stats.isPostureBroken)
            {
                float fpCost = GetSpellFPCost(currentSpell);
                if (!_stats.ConsumeFP(fpCost))
                {
                    GameManager.Instance?.SpawnFloatingText(
                        transform.position + Vector3.up * 2.2f,
                        "KHÔNG ĐỦ FP (Nhấn [F] uống Bình Xanh)!",
                        new Color(0.35f, 0.7f, 1f), 14, 0.9f);
                    return;
                }

                _attackCooldownTimer = 0.38f;
                _visuals?.TriggerAttackAnimation(4, 0.35f);

                GameObject projObj = new GameObject($"Spell_{currentSpell}");
                projObj.transform.position = transform.position + transform.forward * 1.1f + Vector3.up * 1.15f;
                MagicProjectile proj = projObj.AddComponent<MagicProjectile>();

                Transform target = _controller.lockOnTarget;
                proj.Initialize(transform.forward, currentSpell, false, _stats.attackDamageMultiplier, target);
            }
        }

        private void SelectSpell(SpellType type)
        {
            currentSpell = type;
            GameManager.Instance?.SpawnFloatingText(
                transform.position + Vector3.up * 2.2f,
                $"PHÉP: {GetSpellDisplayName(type)}",
                new Color(0.4f, 0.85f, 1f), 14, 0.85f);
        }

        public static string GetSpellDisplayName(SpellType type)
        {
            switch (type)
            {
                case SpellType.GlintstoneComet: return "1. Cung Sao Băng (14 FP)";
                case SpellType.FellFireball: return "2. Hỏa Cầu Hủy Diệt (24 FP)";
                case SpellType.LightningSpear: return "3. Lôi Thương Cổ Đại (20 FP)";
                default: return type.ToString();
            }
        }

        public static float GetSpellFPCost(SpellType type)
        {
            switch (type)
            {
                case SpellType.GlintstoneComet: return 14f;
                case SpellType.FellFireball: return 24f;
                case SpellType.LightningSpear: return 20f;
                default: return 15f;
            }
        }

        private void HandleFlaskInput()
        {
            // Phím R: Uống Bình Máu Đỏ (Crimson Tears)
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (crimsonFlasks > 0 && _stats.currentHP < _stats.maxHP)
                {
                    crimsonFlasks--;
                    _stats.HealHP(65f);
                    GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 1.0f, new Color(1f, 0.25f, 0.3f), 12, 4.5f, 0.12f);
                    GameManager.Instance?.SpawnFloatingText(transform.position + Vector3.up * 2.2f, "+65 HP (Bình Máu Đỏ)", new Color(1f, 0.35f, 0.35f), 15);
                }
                else if (crimsonFlasks <= 0)
                {
                    GameManager.Instance?.SpawnFloatingText(transform.position + Vector3.up * 2.2f, "HẾT BÌNH MÁU ĐỎ!", Color.red, 14);
                }
            }

            // Phím F: Uống Bình Năng Lượng Xanh (Cerulean Tears)
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (ceruleanFlasks > 0 && _stats.currentFP < _stats.maxFP)
                {
                    ceruleanFlasks--;
                    _stats.RestoreFP(55f);
                    GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 1.0f, new Color(0.2f, 0.65f, 1f), 12, 4.5f, 0.12f);
                    GameManager.Instance?.SpawnFloatingText(transform.position + Vector3.up * 2.2f, "+55 FP (Bình Năng Lượng)", new Color(0.3f, 0.75f, 1f), 15);
                }
                else if (ceruleanFlasks <= 0)
                {
                    GameManager.Instance?.SpawnFloatingText(transform.position + Vector3.up * 2.2f, "HẾT BÌNH NĂNG LƯỢNG!", Color.cyan, 14);
                }
            }
        }

        /// <summary>
        /// Xử lý đòn tấn công từ Boss vào Dũng Sĩ.
        /// Trả về true nếu người chơi thực hiện PERFECT PARRY (Đỡ đòn hoàn hảo chuẩn Sekiro).
        /// </summary>
        public bool ReceiveIncomingAttack(float hpDamage, float postureDamage, Vector3 attackerPos, bool isUnblockablePerilous, Boss.BossController attackerBoss = null)
        {
            if (_stats.IsDead || _stats.isInvulnerable) return false;

            // 1. Kiểm tra Đỡ đòn hoàn hảo (Perfect Parry / Deflect) — Không áp dụng cho đòn Hiểm Cảnh Quét Đất (Unblockable)
            if (!isUnblockablePerilous && IsInPerfectParryWindow)
            {
                // Sekiro Perfect Deflect!
                _stats.ApplyDamage(0f, postureDamage * 0.35f, canBreakPosture: false);
                _visuals?.SetParryStance(true, isPerfectFlash: true);

                // Phản sát thương Thế (Posture) cực mạnh ngược lại cho Boss!
                if (attackerBoss != null)
                {
                    attackerBoss.OnParriedByPlayer(deflectBossPostureDamage * _stats.attackDamageMultiplier);
                }

                Vector3 sparkPos = transform.position + transform.forward * 0.9f + Vector3.up * 1.25f;
                GameManager.Instance?.SpawnVoxelBurst(sparkPos, new Color(1f, 0.88f, 0.22f), 20, 8.5f, 0.15f);
                GameManager.Instance?.TriggerHitStop(0.12f, 0.08f);
                Camera.TopDownCameraController.Instance?.ShakeCamera(0.22f, 0.16f);

                GameManager.Instance?.SpawnFloatingText(
                    transform.position + Vector3.up * 2.4f,
                    "⚡ PERFECT PARRY! (PHÁ THẾ BOSS)",
                    new Color(1f, 0.90f, 0.25f), 18, 1.0f);

                // Reset cửa sổ Parry để cho phép bấm nhịp tiếp theo trong chuỗi combo của Boss
                _parryPressTime = -10f;
                return true;
            }

            // 2. Kiểm tra Đỡ đòn thường (Hold Block)
            if (!isUnblockablePerilous && _isBlocking)
            {
                float chipDamage = hpDamage * 0.20f;
                float guardPosture = postureDamage * 0.85f;
                _stats.ApplyDamage(chipDamage, guardPosture, canBreakPosture: true);

                Vector3 blockPos = transform.position + transform.forward * 0.8f + Vector3.up * 1.2f;
                GameManager.Instance?.SpawnVoxelBurst(blockPos, new Color(0.75f, 0.8f, 0.9f), 8, 5f, 0.10f);
                GameManager.Instance?.SpawnFloatingText(
                    transform.position + Vector3.up * 2.1f,
                    $"ĐỠ ĐÒN (-{Mathf.RoundToInt(chipDamage)} HP)",
                    new Color(0.8f, 0.85f, 0.95f), 14, 0.8f);
                return false;
            }

            // 3. Trúng đòn trực diện
            _stats.ApplyDamage(hpDamage, postureDamage, canBreakPosture: true);
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 1.1f, new Color(0.9f, 0.15f, 0.15f), 14, 6f, 0.14f);
            Camera.TopDownCameraController.Instance?.ShakeCamera(0.28f, 0.20f);

            GameManager.Instance?.SpawnFloatingText(
                transform.position + Vector3.up * 2.2f,
                $"-{Mathf.RoundToInt(hpDamage)} HP",
                new Color(1f, 0.28f, 0.28f), 16, 0.9f);

            return false;
        }

        public void RestoreAllAtGrace()
        {
            _stats.ResetStats();
            crimsonFlasks = maxCrimsonFlasks;
            ceruleanFlasks = maxCeruleanFlasks;
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 1f, new Color(1f, 0.85f, 0.3f), 20, 6f, 0.15f);
            GameManager.Instance?.SpawnFloatingText(transform.position + Vector3.up * 2.4f, "ĐÃ HỒI PHỤC TOÀN BỘ TẠI ĐIỂM ÂN ĐIỂN!", new Color(1f, 0.88f, 0.3f), 16, 1.4f);
        }

        private void HandlePlayerDeath()
        {
            GameManager.Instance?.OnPlayerDied();
        }
    }
}

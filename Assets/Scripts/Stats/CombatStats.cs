using System;
using UnityEngine;

namespace VoxelSouls.Stats
{
    /// <summary>
    /// Quản lý các chỉ số kết hợp giữa Elden Ring (HP, FP/Mana, Stamina)
    /// và Sekiro (Posture / Thanh thế chịu đựng & Trạng thái Phá Thế để Kết Liễu).
    /// </summary>
    public class CombatStats : MonoBehaviour
    {
        [Header("Health (Máu - HP)")]
        public float maxHP = 100f;
        public float currentHP = 100f;

        [Header("Focus Points (Năng lượng phép - FP)")]
        public float maxFP = 80f;
        public float currentFP = 80f;

        [Header("Stamina (Thể lực)")]
        public float maxStamina = 100f;
        public float currentStamina = 100f;
        public float staminaRegenRate = 28f;
        public float staminaRegenDelay = 0.65f;

        [Header("Posture (Thanh Chịu Đựng - Cơ chế Sekiro)")]
        public float maxPosture = 100f;
        public float currentPosture = 0f;
        public float postureDecayRate = 12f;
        public float postureDecayDelay = 2.0f;
        public bool isPostureBroken = false;
        public float postureBrokenDuration = 3.5f;

        [Header("Buffs & Modifiers")]
        public float attackDamageMultiplier = 1f;
        public bool isInvulnerable = false;

        private float _lastStaminaUseTime;
        private float _lastPostureHitTime;
        private float _postureBrokenTimer;

        public event Action<float, float> OnDamaged; // damage, postureDamage
        public event Action OnPostureBroken;
        public event Action OnPostureRecovered;
        public event Action OnDeath;

        public bool IsDead => currentHP <= 0f;

        private void Awake()
        {
            ResetStats();
        }

        public void ResetStats()
        {
            currentHP = maxHP;
            currentFP = maxFP;
            currentStamina = maxStamina;
            currentPosture = 0f;
            isPostureBroken = false;
            _postureBrokenTimer = 0f;
        }

        private void Update()
        {
            if (IsDead) return;

            // Hồi Stamina tự động sau khi ngừng hành động
            if (Time.time - _lastStaminaUseTime >= staminaRegenDelay && currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            }

            // Xử lý trạng thái Phá Thế (Posture Broken - Quỳ gối chờ Kết Liễu)
            if (isPostureBroken)
            {
                _postureBrokenTimer -= Time.deltaTime;
                if (_postureBrokenTimer <= 0f)
                {
                    isPostureBroken = false;
                    currentPosture = maxPosture * 0.35f; // Hồi lại 65% thanh thế sau khi thoát choáng
                    OnPostureRecovered?.Invoke();
                }
                return;
            }

            // Giảm thanh Posture dần khi không bị đánh (Máu càng thấp, Posture hồi càng chậm giống Sekiro!)
            if (Time.time - _lastPostureHitTime >= postureDecayDelay && currentPosture > 0f)
            {
                float hpRatio = Mathf.Clamp01(currentHP / maxHP);
                float effectiveDecay = postureDecayRate * Mathf.Lerp(0.25f, 1.0f, hpRatio);
                currentPosture = Mathf.Max(0f, currentPosture - effectiveDecay * Time.deltaTime);
            }
        }

        public bool ConsumeStamina(float amount)
        {
            if (currentStamina < amount * 0.3f) return false;
            currentStamina = Mathf.Max(0f, currentStamina - amount);
            _lastStaminaUseTime = Time.time;
            return true;
        }

        public bool ConsumeFP(float amount)
        {
            if (currentFP < amount) return false;
            currentFP -= amount;
            return true;
        }

        public void HealHP(float amount)
        {
            if (IsDead) return;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
        }

        public void RestoreFP(float amount)
        {
            if (IsDead) return;
            currentFP = Mathf.Min(maxFP, currentFP + amount);
        }

        /// <summary>
        /// Nhận sát thương Máu và Sát thương Thế (Posture).
        /// canBreakPosture = false khi Người chơi thực hiện Perfect Parry (Sekiro: Parry chuẩn không bao giờ bị vỡ thế!).
        /// </summary>
        public void ApplyDamage(float hpDamage, float postureDamage, bool canBreakPosture = true)
        {
            if (IsDead) return;
            if (isInvulnerable && hpDamage > 0f) return;

            if (hpDamage > 0f)
            {
                currentHP = Mathf.Max(0f, currentHP - hpDamage);
            }

            if (postureDamage > 0f)
            {
                _lastPostureHitTime = Time.time;
                currentPosture += postureDamage;

                if (currentPosture >= maxPosture)
                {
                    if (canBreakPosture)
                    {
                        currentPosture = maxPosture;
                        if (!isPostureBroken)
                        {
                            isPostureBroken = true;
                            _postureBrokenTimer = postureBrokenDuration;
                            OnPostureBroken?.Invoke();
                        }
                    }
                    else
                    {
                        // Sekiro rule: Đỡ đòn hoàn hảo (Deflect) giữ thanh Posture ở mức 99% chứ không bị phá thế
                        currentPosture = maxPosture - 1f;
                    }
                }
            }

            OnDamaged?.Invoke(hpDamage, postureDamage);

            if (currentHP <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        /// <summary>
        /// Kích hoạt khi Dũng Sĩ tung đòn Kết Liễu (Deathblow) vào Boss đang bị Phá Thế
        /// </summary>
        public void ConsumePostureBreakOnDeathblow(float massiveHpDamage)
        {
            isPostureBroken = false;
            currentPosture = 0f;
            currentHP = Mathf.Max(0f, currentHP - massiveHpDamage);
            OnPostureRecovered?.Invoke();
            OnDamaged?.Invoke(massiveHpDamage, 0f);

            if (currentHP <= 0f)
            {
                OnDeath?.Invoke();
            }
        }
    }
}

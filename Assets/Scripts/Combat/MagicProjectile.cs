using UnityEngine;
using VoxelSouls.Core;
using VoxelSouls.Stats;

namespace VoxelSouls.Combat
{
    public enum SpellType
    {
        GlintstoneComet = 0, // Cung Sao Băng (Nhanh, tự dẫn đường nhẹ)
        FellFireball = 1,    // Hỏa Cầu Hủy Diệt (Nổ diện rộng, phá Posture mạnh)
        LightningSpear = 2   // Lôi Thương Cổ Đại (Tốc độ cao, làm choáng ngắn)
    }

    /// <summary>
    /// Đạn phép thuật phong cách Elden Ring (hoặc đạn năng lượng của Boss).
    /// </summary>
    public class MagicProjectile : MonoBehaviour
    {
        public SpellType spellType;
        public bool isEnemyProjectile = false;
        public float speed = 16f;
        public float hpDamage = 28f;
        public float postureDamage = 22f;
        public float explosionRadius = 1.6f;
        public float lifeTime = 4.0f;
        public Transform homingTarget;
        public float homingStrength = 2.5f;
        public Color spellColor = new Color(0.25f, 0.8f, 1f);

        private float _spawnTime;
        private Vector3 _moveDir;

        public void Initialize(Vector3 direction, SpellType type, bool fromEnemy, float dmgMultiplier = 1f, Transform target = null)
        {
            _moveDir = direction.normalized;
            _moveDir.y = 0f;
            if (_moveDir.sqrMagnitude < 0.01f) _moveDir = transform.forward;
            transform.rotation = Quaternion.LookRotation(_moveDir);

            spellType = type;
            isEnemyProjectile = fromEnemy;
            homingTarget = target;
            _spawnTime = Time.time;

            if (fromEnemy)
            {
                speed = 13f;
                hpDamage = 22f * dmgMultiplier;
                postureDamage = 20f;
                explosionRadius = 1.4f;
                spellColor = new Color(0.95f, 0.2f, 0.35f);
            }
            else
            {
                switch (type)
                {
                    case SpellType.GlintstoneComet:
                        speed = 18f;
                        hpDamage = 26f * dmgMultiplier;
                        postureDamage = 18f * dmgMultiplier;
                        explosionRadius = 1.3f;
                        homingStrength = 3.2f;
                        spellColor = new Color(0.2f, 0.85f, 1.0f);
                        break;
                    case SpellType.FellFireball:
                        speed = 12.5f;
                        hpDamage = 44f * dmgMultiplier;
                        postureDamage = 36f * dmgMultiplier;
                        explosionRadius = 3.0f;
                        homingStrength = 1.0f;
                        spellColor = new Color(1.0f, 0.38f, 0.08f);
                        break;
                    case SpellType.LightningSpear:
                        speed = 25f;
                        hpDamage = 34f * dmgMultiplier;
                        postureDamage = 32f * dmgMultiplier;
                        explosionRadius = 1.8f;
                        homingStrength = 1.8f;
                        spellColor = new Color(1.0f, 0.92f, 0.25f);
                        break;
                }
            }

            BuildProjectileVisual();
        }

        private void BuildProjectileVisual()
        {
            Vector3 coreScale = (spellType == SpellType.LightningSpear && !isEnemyProjectile)
                ? new Vector3(0.22f, 0.22f, 1.15f)
                : (spellType == SpellType.FellFireball ? Vector3.one * 0.62f : Vector3.one * 0.38f);

            ShaderHelper.CreateVoxelPart("SpellCore", transform, Vector3.zero, coreScale, spellColor, spellColor * 2.8f);
            ShaderHelper.CreateVoxelPart("SpellAura", transform, new Vector3(0f, 0f, -0.25f), coreScale * 0.7f, Color.white, spellColor * 3.2f);
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= lifeTime)
            {
                Explode(transform.position);
                return;
            }

            // Tự dẫn đường nhẹ về phía mục tiêu (Homing)
            if (homingTarget != null && homingStrength > 0f)
            {
                Vector3 toTarget = (homingTarget.position + Vector3.up * 1.1f) - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.2f)
                {
                    _moveDir = Vector3.Slerp(_moveDir, toTarget.normalized, homingStrength * Time.deltaTime).normalized;
                    transform.rotation = Quaternion.LookRotation(_moveDir);
                }
            }

            transform.position += _moveDir * (speed * Time.deltaTime);
            transform.Rotate(0f, 0f, 360f * Time.deltaTime, Space.Self);

            // Kiểm tra va chạm với mục tiêu
            CheckCollision();
        }

        private void CheckCollision()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 0.55f);
            foreach (Collider hit in hits)
            {
                if (isEnemyProjectile)
                {
                    Player.PlayerCombat playerCombat = hit.GetComponentInParent<Player.PlayerCombat>();
                    if (playerCombat != null)
                    {
                        bool deflected = playerCombat.ReceiveIncomingAttack(hpDamage, postureDamage, transform.position, false, this);
                        if (deflected)
                        {
                            // Sekiro Deflect: Phản ngược đạn phép về phía Boss!
                            isEnemyProjectile = false;
                            _moveDir = -_moveDir;
                            speed *= 1.4f;
                            hpDamage *= 1.5f;
                            postureDamage *= 1.8f;
                            spellColor = new Color(1f, 0.88f, 0.25f);
                            _spawnTime = Time.time;
                            return;
                        }
                        Explode(transform.position);
                        return;
                    }
                }
                else
                {
                    Boss.BossController boss = hit.GetComponentInParent<Boss.BossController>();
                    if (boss != null && !boss.Stats.IsDead)
                    {
                        Explode(transform.position);
                        return;
                    }
                }

                if (hit.gameObject.name.Contains("Pillar") || hit.gameObject.name.Contains("Wall"))
                {
                    Explode(transform.position);
                    return;
                }
            }
        }

        private void Explode(Vector3 pos)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SpawnVoxelBurst(pos, spellColor, spellType == SpellType.FellFireball ? 22 : 12, 7.5f, 0.16f);
            }

            if (!isEnemyProjectile)
            {
                Collider[] hits = Physics.OverlapSphere(pos, explosionRadius);
                foreach (Collider hit in hits)
                {
                    Boss.BossController boss = hit.GetComponentInParent<Boss.BossController>();
                    if (boss != null && !boss.Stats.IsDead)
                    {
                        boss.ReceivePlayerAttack(hpDamage, postureDamage, spellType == SpellType.LightningSpear);
                        break;
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}

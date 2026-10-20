using UnityEngine;
using VoxelSouls.Core;
using VoxelSouls.Player;

namespace VoxelSouls.World
{
    public enum RPGItemType
    {
        GoldenSeed,         // +1 Bình Máu Đỏ & +1 Bình Năng Lượng Xanh
        AncientSmithingRune,// +25% Sát thương Kiếm & Phép thuật
        CrimsonMedallion,   // +40 Máu tối đa (Max HP) & Hồi đầy máu
        SiteOfGrace         // Điểm Ân Điển: Hồi phục toàn bộ khi nhấn [G] hoặc chạm vào
    }

    /// <summary>
    /// Vật phẩm RPG trong khu vực Điện Thờ Khởi Đầu để Dũng Sĩ thu thập trước khi bước vào Đấu Trường Boss.
    /// </summary>
    public class WorldItemPickup : MonoBehaviour
    {
        public RPGItemType itemType;
        public string itemName = "Vật Phẩm Cổ Ngữ";
        public string itemDescription = "Tăng sức mạnh cho Dũng Sĩ";
        public Transform floatingVisual;
        public bool isConsumed = false;

        private float _bobTimer;

        private void Update()
        {
            if (isConsumed) return;

            _bobTimer += Time.deltaTime * 2.5f;
            if (floatingVisual != null)
            {
                floatingVisual.localPosition = new Vector3(0f, 1.15f + Mathf.Sin(_bobTimer) * 0.14f, 0f);
                floatingVisual.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
            }

            PlayerCombat player = FindFirstObjectByType<PlayerCombat>();
            if (player == null || player.Stats.IsDead) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= 2.1f)
            {
                if (itemType == RPGItemType.SiteOfGrace)
                {
                    if (Input.GetKeyDown(KeyCode.G))
                    {
                        player.RestoreAllAtGrace();
                    }
                }
                else
                {
                    CollectItem(player);
                }
            }
        }

        private void CollectItem(PlayerCombat player)
        {
            isConsumed = true;
            player.runesCollected++;

            switch (itemType)
            {
                case RPGItemType.GoldenSeed:
                    player.maxCrimsonFlasks++;
                    player.crimsonFlasks++;
                    player.maxCeruleanFlasks++;
                    player.ceruleanFlasks++;
                    GameManager.Instance?.SpawnFloatingText(
                        transform.position + Vector3.up * 2.2f,
                        "NHẶT: HẠT GIỐNG HOÀNG KIM (+1 Bình Máu & +1 Bình FP!)",
                        new Color(1f, 0.88f, 0.25f), 16, 1.8f);
                    break;

                case RPGItemType.AncientSmithingRune:
                    player.Stats.attackDamageMultiplier += 0.30f;
                    GameManager.Instance?.SpawnFloatingText(
                        transform.position + Vector3.up * 2.2f,
                        "NHẶT: ĐÁ CỔ NGỮ RÈN KIẾM (+30% Sức Tấn Công & Phá Thế!)",
                        new Color(0.35f, 0.9f, 1f), 16, 1.8f);
                    break;

                case RPGItemType.CrimsonMedallion:
                    player.Stats.maxHP += 40f;
                    player.Stats.HealHP(100f);
                    GameManager.Instance?.SpawnFloatingText(
                        transform.position + Vector3.up * 2.2f,
                        "NHẶT: BÙA HỘ MỆNH HỒNG NGỌC (+40 Máu Tối Đa!)",
                        new Color(1f, 0.35f, 0.4f), 16, 1.8f);
                    break;
            }

            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.25f), 18, 6.5f, 0.14f);

            if (floatingVisual != null)
                Destroy(floatingVisual.gameObject);
        }
    }

    /// <summary>
    /// Hình Nhân Tập Luyện đặt ở Sân Khởi Đầu để người chơi thử kiếm và phép thuật trước khi vào đánh Boss.
    /// </summary>
    public class TrainingDummy : MonoBehaviour
    {
        private float _shakeTimer;
        private Quaternion _baseRot;

        private void Start()
        {
            _baseRot = transform.rotation;
        }

        private void Update()
        {
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                transform.rotation = _baseRot * Quaternion.Euler(Mathf.Sin(_shakeTimer * 45f) * 14f, 0f, Mathf.Cos(_shakeTimer * 35f) * 10f);
                if (_shakeTimer <= 0f)
                    transform.rotation = _baseRot;
            }
        }

        public void OnHit(float hpDmg, float postureDmg)
        {
            _shakeTimer = 0.35f;
            GameManager.Instance?.SpawnVoxelBurst(transform.position + Vector3.up * 1.3f, new Color(0.85f, 0.65f, 0.35f), 10, 5f, 0.12f);
            GameManager.Instance?.SpawnFloatingText(
                transform.position + Vector3.up * 2.3f,
                $"HÌNH NHÂN: -{Mathf.RoundToInt(hpDmg)} HP | +{Mathf.RoundToInt(postureDmg)} Thế",
                new Color(1f, 0.88f, 0.35f), 14, 0.9f);
        }
    }
}

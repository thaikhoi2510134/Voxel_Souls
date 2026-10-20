using UnityEngine;
using VoxelSouls.Boss;
using VoxelSouls.Camera;
using VoxelSouls.Player;
using VoxelSouls.Stats;
using VoxelSouls.UI;
using VoxelSouls.Visuals;
using VoxelSouls.World;

namespace VoxelSouls.Core
{
    /// <summary>
    /// Trình dựng toàn bộ Màn Chơi, Ánh Sáng, Nhân Vật Dũng Sĩ, Boss Hộ Vệ Hắc Thạch và Giao Diện.
    /// Hoạt động cả khi bấm menu trong Editor lẫn tự động chạy khi nhấn nút Play (▶) trong một Scene trống!
    /// </summary>
    public static class WorldSceneBuilder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnPlay()
        {
            if (Object.FindObjectOfType<GameManager>() == null && Object.FindObjectOfType<PlayerController>() == null)
            {
                Debug.Log("[VoxelSouls] Phát hiện Scene trống khi bấm Play — Tự động khởi tạo toàn bộ Thế Giới & Nhân Vật!");
                BuildCompletePlayableScene();
            }
        }

        public static void BuildCompletePlayableScene()
        {
            // 1. Khởi tạo GameManager & Giao diện SoulsHUD (Elden Ring + Sekiro UI)
            GameManager existingGm = Object.FindObjectOfType<GameManager>();
            if (existingGm == null)
            {
                GameObject gmObj = new GameObject("GameManager_SoulsHUD");
                gmObj.AddComponent<GameManager>();
                gmObj.AddComponent<SoulsHUD>();
            }

            // 2. Thiết lập Ánh sáng Giả tưởng U Tối (Dark Fantasy Lighting)
            SetupLighting();

            // 3. Khởi tạo Thế Giới Nhỏ & Đấu Trường Hắc Thạch
            ArenaGenerator existingArena = Object.FindObjectOfType<ArenaGenerator>();
            if (existingArena == null)
            {
                GameObject worldObj = new GameObject("FantasyVoxelWorld");
                existingArena = worldObj.AddComponent<ArenaGenerator>();
            }
            existingArena.BuildFantasyWorld();

            // 4. Khởi tạo Boss Đầu Tiên: "HỘ VỆ HẮC THẠCH (OBSIDIAN WARDEN)"
            BossController boss = Object.FindObjectOfType<BossController>();
            if (boss == null)
            {
                GameObject bossObj = new GameObject("Boss_ObsidianWarden");
                bossObj.transform.position = new Vector3(0f, 0.05f, 20f);
                bossObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                CapsuleCollider bossCol = bossObj.AddComponent<CapsuleCollider>();
                bossCol.center = new Vector3(0f, 2.0f, 0f);
                bossCol.radius = 1.05f;
                bossCol.height = 4.0f;

                bossObj.AddComponent<CombatStats>();
                VoxelCharacterVisuals bossVis = bossObj.AddComponent<VoxelCharacterVisuals>();
                bossVis.BuildBossVoxelMesh();

                boss = bossObj.AddComponent<BossController>();
                boss.visuals = bossVis;
            }

            // 5. Khởi tạo Nhân Vật Chính: "DŨNG SĨ CỔ NGỮ (VOXEL KNIGHT)"
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                GameObject heroObj = new GameObject("Hero_VoxelKnight");
                heroObj.transform.position = new Vector3(0f, 0.1f, -23f);
                heroObj.transform.rotation = Quaternion.identity;

                heroObj.AddComponent<CharacterController>();
                heroObj.AddComponent<CombatStats>();
                VoxelCharacterVisuals heroVis = heroObj.AddComponent<VoxelCharacterVisuals>();
                heroVis.BuildHeroVoxelMesh();

                player = heroObj.AddComponent<PlayerController>();
                player.visuals = heroVis;
                player.lockOnTarget = boss.transform;

                heroObj.AddComponent<PlayerCombat>();
            }

            // 6. Thiết lập Camera Góc Nhìn Từ Trên Xuống (Top-Down RPG Camera)
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<UnityEngine.Camera>();
                camObj.AddComponent<AudioListener>();
            }

            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.05f, 0.05f, 0.09f);

            TopDownCameraController camCtrl = mainCam.GetComponent<TopDownCameraController>();
            if (camCtrl == null)
                camCtrl = mainCam.gameObject.AddComponent<TopDownCameraController>();

            camCtrl.playerTarget = player.transform;
            camCtrl.bossTarget = boss.transform;
            mainCam.transform.position = player.transform.position + camCtrl.baseOffset;
            mainCam.transform.LookAt(player.transform.position + Vector3.up * 0.9f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowAreaBanner(
                    "ĐIỆN THỜ CỔ NGỮ & ĐẤU TRƯỜNG HẮC THẠCH",
                    "Hãy thu thập 3 Vật Phẩm quanh Điện Thờ rồi tiến lên phía Bắc khiêu chiến Boss!",
                    new Color(0.45f, 0.88f, 1f));
            }
        }

        private static void SetupLighting()
        {
            Light dirLight = Object.FindObjectOfType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            dirLight.color = new Color(0.88f, 0.85f, 0.96f);
            dirLight.intensity = 1.15f;
            dirLight.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.25f, 0.34f);
        }
    }
}

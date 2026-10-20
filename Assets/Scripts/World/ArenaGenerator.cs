using UnityEngine;
using VoxelSouls.Core;

namespace VoxelSouls.World
{
    /// <summary>
    /// Xây dựng Thế Giới Nhỏ phong cách Giả Tưởng Blocky/Voxel gồm 2 khu vực nối liền nhau:
    /// 1. Khu Điện Thờ Khởi Đầu (Sanctuary Courtyard - Phía Nam): Có Điểm Ân Điển (Site of Grace),
    ///    Hình Nhân Tập Luyện, và 3 Bệ Đá chứa Vật Phẩm RPG cường hóa cho Dũng Sĩ.
    /// 2. Đấu Trường Hắc Thạch (Obsidian Boss Colosseum - Phía Bắc): Khu vực đại chiến với Boss Hộ Vệ Hắc Thạch,
    ///    gồm Cổng Cổ Ngữ, 6 Cột Đá Hắc Thạch, Đài Lửa Dung Nham và Ngai Vàng Cổ Thần.
    /// </summary>
    public class ArenaGenerator : MonoBehaviour
    {
        public void BuildFantasyWorld()
        {
            // Xóa các con cũ nếu gọi lại
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }

            Color stoneDark = new Color(0.16f, 0.17f, 0.22f);
            Color stoneLight = new Color(0.23f, 0.25f, 0.31f);
            Color obsidianFloor = new Color(0.11f, 0.10f, 0.15f);
            Color runeCyan = new Color(0.20f, 0.80f, 1.0f);
            Color graceGold = new Color(1.0f, 0.82f, 0.22f);
            Color magmaOrange = new Color(1.0f, 0.32f, 0.08f);

            // 1. KHU ĐIỆN THỜ KHỞI ĐẦU (Sanctuary Courtyard: Z từ -26 đến -8)
            CreateSolidBox("SanctuaryFloor", new Vector3(0f, -0.5f, -18f), new Vector3(22f, 1f, 18f), stoneDark);
            CreateSolidBox("SanctuaryCarpet", new Vector3(0f, 0.02f, -18f), new Vector3(4.5f, 0.05f, 16f), new Color(0.48f, 0.12f, 0.15f));

            // Cầu Đá Cổ Đại nối từ Điện Thờ vào Đấu Trường Boss (Z từ -9 đến -3)
            CreateSolidBox("AncientBridge", new Vector3(0f, -0.45f, -5.5f), new Vector3(7.5f, 1f, 8f), stoneLight);

            // 2. ĐẤU TRƯỜNG HẮC THẠCH (Obsidian Boss Arena: Z từ -2 đến +34)
            CreateSolidBox("BossArenaFloor", new Vector3(0f, -0.5f, 16f), new Vector3(38f, 1f, 36f), obsidianFloor);
            CreateSolidBox("ArenaRuneCircle", new Vector3(0f, 0.02f, 16f), new Vector3(16f, 0.04f, 16f), new Color(0.18f, 0.14f, 0.24f));

            // Tường bao quanh để nhân vật không rơi khỏi bản đồ
            CreateSolidBox("Wall_North", new Vector3(0f, 1.8f, 34.2f), new Vector3(39f, 4.5f, 1.5f), stoneDark);
            CreateSolidBox("Wall_West", new Vector3(-19.2f, 1.8f, 16f), new Vector3(1.5f, 4.5f, 36f), stoneDark);
            CreateSolidBox("Wall_East", new Vector3(19.2f, 1.8f, 16f), new Vector3(1.5f, 4.5f, 36f), stoneDark);
            CreateSolidBox("Wall_SouthWest", new Vector3(-11.5f, 1.5f, -1.8f), new Vector3(16f, 3.8f, 1.4f), stoneDark);
            CreateSolidBox("Wall_SouthEast", new Vector3(11.5f, 1.5f, -1.8f), new Vector3(16f, 3.8f, 1.4f), stoneDark);

            // Tường bao khu Điện Thờ Khởi Đầu
            CreateSolidBox("SanctuaryWall_South", new Vector3(0f, 1.2f, -27.2f), new Vector3(23f, 3f, 1.2f), stoneDark);
            CreateSolidBox("SanctuaryWall_West", new Vector3(-11.2f, 1.2f, -18f), new Vector3(1.2f, 3f, 18f), stoneDark);
            CreateSolidBox("SanctuaryWall_East", new Vector3(11.2f, 1.2f, -18f), new Vector3(1.2f, 3f, 18f), stoneDark);

            // 3. CÁC CỘT ĐÁ CỔ NGỮ & ĐÀI LỬA TRONG ĐẤU TRƯỜNG BOSS
            Vector3[] pillarPositions = new Vector3[]
            {
                new Vector3(-11f, 0f, 6f),
                new Vector3(11f, 0f, 6f),
                new Vector3(-11f, 0f, 16f),
                new Vector3(11f, 0f, 16f),
                new Vector3(-11f, 0f, 26f),
                new Vector3(11f, 0f, 26f)
            };

            for (int i = 0; i < pillarPositions.Length; i++)
            {
                BuildVoxelPillar($"ArenaPillar_{i}", pillarPositions[i], stoneLight, magmaOrange);
            }

            // Hai cột cổng vào Đấu Trường
            BuildVoxelPillar("GatePillar_Left", new Vector3(-4.2f, 0f, -1.5f), stoneLight, runeCyan);
            BuildVoxelPillar("GatePillar_Right", new Vector3(4.2f, 0f, -1.5f), stoneLight, runeCyan);

            // 4. ĐIỂM ÂN ĐIỂN (SITE OF GRACE) & HÌNH NHÂN TẬP LUYỆN
            BuildSiteOfGrace(new Vector3(0f, 0f, -19.5f), graceGold);
            BuildTrainingDummy(new Vector3(4.5f, 0f, -14.5f));

            // 5. 3 VẬT PHẨM RPG CƠ BẢN TRONG THẾ GIỚI NHỎ (Thu thập để mạnh lên trước khi đánh Boss!)
            BuildRPGItemPedestal(
                new Vector3(-6.5f, 0f, -21f),
                RPGItemType.GoldenSeed,
                "Hạt Giống Hoàng Kim (+Bình Máu & FP)",
                graceGold);

            BuildRPGItemPedestal(
                new Vector3(-6.5f, 0f, -14f),
                RPGItemType.AncientSmithingRune,
                "Đá Cổ Ngữ Rèn Kiếm (+30% Công)",
                runeCyan);

            BuildRPGItemPedestal(
                new Vector3(6.5f, 0f, -21f),
                RPGItemType.CrimsonMedallion,
                "Bùa Hộ Mệnh Hồng Ngọc (+40 Máu)",
                new Color(1f, 0.25f, 0.35f));
        }

        private GameObject CreateSolidBox(string name, Vector3 localPos, Vector3 scale, Color color, Color? emission = null)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(transform, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = ShaderHelper.CreateMaterial(color, 0.2f, 0.1f, emission);
            return box;
        }

        private void BuildVoxelPillar(string name, Vector3 basePos, Color stoneColor, Color glowColor)
        {
            GameObject p = new GameObject(name);
            p.transform.SetParent(transform, false);
            p.transform.localPosition = basePos;

            BoxCollider col = p.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 2.2f, 0f);
            col.size = new Vector3(1.8f, 4.4f, 1.8f);

            ShaderHelper.CreateVoxelPart("PillarBase", p.transform, new Vector3(0f, 0.35f, 0f), new Vector3(2.0f, 0.7f, 2.0f), stoneColor);
            ShaderHelper.CreateVoxelPart("PillarShaft", p.transform, new Vector3(0f, 2.3f, 0f), new Vector3(1.4f, 3.4f, 1.4f), stoneColor);
            ShaderHelper.CreateVoxelPart("PillarRuneStrip", p.transform, new Vector3(0f, 2.3f, 0f), new Vector3(1.48f, 0.45f, 1.48f), glowColor, glowColor * 2.2f);
            ShaderHelper.CreateVoxelPart("PillarCap", p.transform, new Vector3(0f, 4.2f, 0f), new Vector3(1.9f, 0.5f, 1.9f), stoneColor);
        }

        private void BuildSiteOfGrace(Vector3 pos, Color goldColor)
        {
            GameObject grace = new GameObject("SiteOfGrace_Shrine");
            grace.transform.SetParent(transform, false);
            grace.transform.localPosition = pos;

            ShaderHelper.CreateVoxelPart("GraceBase", grace.transform, new Vector3(0f, 0.18f, 0f), new Vector3(1.8f, 0.36f, 1.8f), new Color(0.28f, 0.28f, 0.34f));
            GameObject flame = ShaderHelper.CreateVoxelPart("GraceFlame", grace.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.42f, 0.95f, 0.42f), goldColor, goldColor * 3.0f);

            WorldItemPickup pickup = grace.AddComponent<WorldItemPickup>();
            pickup.itemType = RPGItemType.SiteOfGrace;
            pickup.itemName = "Điểm Ân Điển (Nhấn [G] để Hồi Phục)";
            pickup.floatingVisual = flame.transform;
        }

        private void BuildRPGItemPedestal(Vector3 pos, RPGItemType type, string label, Color crystalColor)
        {
            GameObject ped = new GameObject($"ItemPedestal_{type}");
            ped.transform.SetParent(transform, false);
            ped.transform.localPosition = pos;

            ShaderHelper.CreateVoxelPart("PedestalStone", ped.transform, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 1.2f), new Color(0.25f, 0.27f, 0.34f));
            GameObject crystal = ShaderHelper.CreateVoxelPart("ItemCrystal", ped.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.48f, 0.48f, 0.48f), crystalColor, crystalColor * 2.6f);
            crystal.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

            WorldItemPickup pickup = ped.AddComponent<WorldItemPickup>();
            pickup.itemType = type;
            pickup.itemName = label;
            pickup.floatingVisual = crystal.transform;
        }

        private void BuildTrainingDummy(Vector3 pos)
        {
            GameObject dummy = new GameObject("TrainingDummy_Voxel");
            dummy.transform.SetParent(transform, false);
            dummy.transform.localPosition = pos;

            BoxCollider col = dummy.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.1f, 0f);
            col.size = new Vector3(1.0f, 2.2f, 1.0f);

            Color wood = new Color(0.55f, 0.38f, 0.20f);
            Color hay = new Color(0.85f, 0.72f, 0.32f);

            ShaderHelper.CreateVoxelPart("Post", dummy.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.24f, 1.8f, 0.24f), wood);
            ShaderHelper.CreateVoxelPart("Body", dummy.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.62f, 0.75f, 0.42f), hay);
            ShaderHelper.CreateVoxelPart("Arms", dummy.transform, new Vector3(0f, 1.32f, 0f), new Vector3(1.35f, 0.20f, 0.20f), wood);
            ShaderHelper.CreateVoxelPart("Head", dummy.transform, new Vector3(0f, 1.75f, 0f), new Vector3(0.44f, 0.44f, 0.44f), hay);

            dummy.AddComponent<TrainingDummy>();
        }
    }
}

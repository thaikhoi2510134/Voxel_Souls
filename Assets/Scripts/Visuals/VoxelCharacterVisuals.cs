using UnityEngine;
using VoxelSouls.Core;

namespace VoxelSouls.Visuals
{
    /// <summary>
    /// Xây dựng mô hình 3D phong cách Blocky/Voxel cho Dũng Sĩ (Hero) và Boss Hộ Vệ Hắc Thạch
    /// kèm hệ thống khớp xoay (Procedural Rig & Animation) cho các động tác:
    /// Chạy, Nhảy, Lộn nhào (Dodge Roll), Chém Combo 3 đòn, Đỡ đòn / Parry (Sekiro), Chưởng Phép (Elden Ring),
    /// và Quỳ gối khi Vỡ Thế (Posture Broken).
    /// Nếu người dùng đã xuất file .FBX từ Blender vào Assets/Models, hệ thống có thể gắn trực tiếp hoặc dùng Voxel Rig này.
    /// </summary>
    public class VoxelCharacterVisuals : MonoBehaviour
    {
        public bool isBoss = false;

        [Header("Rig Pivots (Khớp chuyển động)")]
        public Transform rootVisual;
        public Transform torsoPivot;
        public Transform headPivot;
        public Transform rightArmPivot;
        public Transform leftArmPivot;
        public Transform rightLegPivot;
        public Transform leftLegPivot;
        public Transform weaponTip;
        public Renderer weaponBladeRenderer;
        public Renderer auraRenderer;

        // Trạng thái hoạt ảnh
        private float _walkCycle;
        private float _attackAnimTimer;
        private int _attackComboType; // 1: Chém ngang, 2: Chém ngược, 3: Bổ dọc, 4: Đâm / Kết liễu, 5: Quét Hiểm Cảnh (Boss)
        private float _attackDuration = 0.35f;
        private bool _isParryingStance;
        private bool _isPostureBrokenStance;
        private bool _isRolling;
        private float _rollTimer;
        private Color _defaultBladeEmission;

        public void BuildHeroVoxelMesh()
        {
            isBoss = false;
            ClearExistingVisuals();

            rootVisual = new GameObject("HeroVoxelRig").transform;
            rootVisual.SetParent(transform, false);
            rootVisual.localPosition = Vector3.zero;

            Color armorSteel = new Color(0.28f, 0.32f, 0.40f);
            Color armorGold = new Color(0.85f, 0.68f, 0.22f);
            Color tunicBlue = new Color(0.14f, 0.25f, 0.48f);
            Color capeCrimson = new Color(0.65f, 0.12f, 0.15f);
            Color skinTone = new Color(0.92f, 0.76f, 0.62f);
            Color visorGlow = new Color(0.25f, 0.85f, 1.0f);
            Color swordBlade = new Color(0.82f, 0.88f, 0.95f);
            _defaultBladeEmission = new Color(0.15f, 0.45f, 0.85f);

            // 1. Thân (Torso) + Áo choàng Dũng Sĩ
            torsoPivot = new GameObject("TorsoPivot").transform;
            torsoPivot.SetParent(rootVisual, false);
            torsoPivot.localPosition = new Vector3(0f, 0.95f, 0f);

            ShaderHelper.CreateVoxelPart("ChestArmor", torsoPivot, Vector3.zero, new Vector3(0.56f, 0.68f, 0.34f), armorSteel);
            ShaderHelper.CreateVoxelPart("ChestTabard", torsoPivot, new Vector3(0f, -0.04f, 0.02f), new Vector3(0.40f, 0.62f, 0.36f), tunicBlue);
            ShaderHelper.CreateVoxelPart("GoldBelt", torsoPivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.58f, 0.12f, 0.37f), armorGold);
            ShaderHelper.CreateVoxelPart("HeroCape", torsoPivot, new Vector3(0f, -0.08f, -0.20f), new Vector3(0.52f, 0.78f, 0.06f), capeCrimson);

            // 2. Đầu (Head) + Mũ giáp Hiệp Sĩ & Chùm lông đỏ
            headPivot = new GameObject("HeadPivot").transform;
            headPivot.SetParent(torsoPivot, false);
            headPivot.localPosition = new Vector3(0f, 0.56f, 0f);

            ShaderHelper.CreateVoxelPart("HeadCube", headPivot, Vector3.zero, new Vector3(0.44f, 0.44f, 0.44f), armorSteel);
            ShaderHelper.CreateVoxelPart("FaceSkin", headPivot, new Vector3(0f, -0.02f, 0.18f), new Vector3(0.34f, 0.18f, 0.10f), skinTone);
            ShaderHelper.CreateVoxelPart("EyesGlow", headPivot, new Vector3(0f, 0.02f, 0.225f), new Vector3(0.28f, 0.06f, 0.04f), visorGlow, visorGlow * 2.0f);
            ShaderHelper.CreateVoxelPart("HelmetCrest", headPivot, new Vector3(0f, 0.28f, -0.04f), new Vector3(0.10f, 0.18f, 0.42f), capeCrimson);

            // 3. Tay Phải + Thanh Kiếm Cổ Ngữ (Katana/Greatsword lai Sekiro & Elden Ring)
            rightArmPivot = new GameObject("RightArmPivot").transform;
            rightArmPivot.SetParent(torsoPivot, false);
            rightArmPivot.localPosition = new Vector3(0.38f, 0.22f, 0f);

            ShaderHelper.CreateVoxelPart("RightPauldron", rightArmPivot, new Vector3(0.04f, 0.06f, 0f), new Vector3(0.26f, 0.24f, 0.28f), armorGold);
            ShaderHelper.CreateVoxelPart("RightArm", rightArmPivot, new Vector3(0.02f, -0.22f, 0f), new Vector3(0.18f, 0.52f, 0.18f), armorSteel);

            // Vũ khí: Thanh Kiếm Ánh Sáng Cổ Ngữ
            ShaderHelper.CreateVoxelPart("SwordHilt", rightArmPivot, new Vector3(0.02f, -0.45f, 0.18f), new Vector3(0.08f, 0.08f, 0.34f), armorGold);
            ShaderHelper.CreateVoxelPart("SwordCrossguard", rightArmPivot, new Vector3(0.02f, -0.45f, 0.32f), new Vector3(0.28f, 0.08f, 0.08f), armorGold);
            GameObject blade = ShaderHelper.CreateVoxelPart("RunicBlade", rightArmPivot, new Vector3(0.02f, -0.45f, 0.92f), new Vector3(0.06f, 0.10f, 1.18f), swordBlade, _defaultBladeEmission);
            weaponBladeRenderer = blade.GetComponent<Renderer>();

            weaponTip = new GameObject("WeaponTip").transform;
            weaponTip.SetParent(rightArmPivot, false);
            weaponTip.localPosition = new Vector3(0.02f, -0.45f, 1.52f);

            // 4. Tay Trái + Khiên Đỡ / Ấn Phép (Parry Buckler & Arcane Catalyst)
            leftArmPivot = new GameObject("LeftArmPivot").transform;
            leftArmPivot.SetParent(torsoPivot, false);
            leftArmPivot.localPosition = new Vector3(-0.38f, 0.22f, 0f);

            ShaderHelper.CreateVoxelPart("LeftPauldron", leftArmPivot, new Vector3(-0.04f, 0.06f, 0f), new Vector3(0.26f, 0.24f, 0.28f), armorGold);
            ShaderHelper.CreateVoxelPart("LeftArm", leftArmPivot, new Vector3(-0.02f, -0.22f, 0f), new Vector3(0.18f, 0.52f, 0.18f), armorSteel);
            ShaderHelper.CreateVoxelPart("ParryBuckler", leftArmPivot, new Vector3(-0.12f, -0.32f, 0.12f), new Vector3(0.08f, 0.36f, 0.36f), armorGold, new Color(0.2f, 0.5f, 0.9f) * 0.5f);

            // 5. Hai Chân (Legs)
            rightLegPivot = new GameObject("RightLegPivot").transform;
            rightLegPivot.SetParent(rootVisual, false);
            rightLegPivot.localPosition = new Vector3(0.15f, 0.58f, 0f);
            ShaderHelper.CreateVoxelPart("RightLeg", rightLegPivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.21f, 0.56f, 0.22f), armorSteel);

            leftLegPivot = new GameObject("LeftLegPivot").transform;
            leftLegPivot.SetParent(rootVisual, false);
            leftLegPivot.localPosition = new Vector3(-0.15f, 0.58f, 0f);
            ShaderHelper.CreateVoxelPart("LeftLeg", leftLegPivot, new Vector3(0f, -0.28f, 0f), new Vector3(0.21f, 0.56f, 0.22f), armorSteel);
        }

        public void BuildBossVoxelMesh()
        {
            isBoss = true;
            ClearExistingVisuals();

            rootVisual = new GameObject("BossVoxelRig").transform;
            rootVisual.SetParent(transform, false);
            rootVisual.localPosition = Vector3.zero;
            // Boss to gấp ~2.1 lần Dũng Sĩ để tạo cảm giác choáng ngợp chuẩn Elden Ring
            rootVisual.localScale = Vector3.one * 2.1f;

            Color obsidianDark = new Color(0.12f, 0.11f, 0.16f);
            Color obsidianTrim = new Color(0.28f, 0.22f, 0.32f);
            Color magmaCrimson = new Color(0.95f, 0.22f, 0.08f);
            Color goldCrown = new Color(0.92f, 0.72f, 0.18f);
            _defaultBladeEmission = magmaCrimson * 1.4f;

            // 1. Thân Boss Hộ Vệ Hắc Thạch
            torsoPivot = new GameObject("TorsoPivot").transform;
            torsoPivot.SetParent(rootVisual, false);
            torsoPivot.localPosition = new Vector3(0f, 1.0f, 0f);

            ShaderHelper.CreateVoxelPart("BossTorso", torsoPivot, Vector3.zero, new Vector3(0.72f, 0.78f, 0.44f), obsidianDark);
            GameObject core = ShaderHelper.CreateVoxelPart("MagmaCore", torsoPivot, new Vector3(0f, 0.06f, 0.04f), new Vector3(0.32f, 0.32f, 0.40f), magmaCrimson, magmaCrimson * 2.2f);
            auraRenderer = core.GetComponent<Renderer>();
            ShaderHelper.CreateVoxelPart("BossMantle", torsoPivot, new Vector3(0f, -0.10f, -0.24f), new Vector3(0.68f, 0.88f, 0.08f), new Color(0.35f, 0.06f, 0.08f));

            // 2. Đầu Boss + Vương Miện Sừng Cổ Thần
            headPivot = new GameObject("HeadPivot").transform;
            headPivot.SetParent(torsoPivot, false);
            headPivot.localPosition = new Vector3(0f, 0.62f, 0f);

            ShaderHelper.CreateVoxelPart("BossHelm", headPivot, Vector3.zero, new Vector3(0.46f, 0.48f, 0.46f), obsidianDark);
            ShaderHelper.CreateVoxelPart("BossVisor", headPivot, new Vector3(0f, 0.02f, 0.22f), new Vector3(0.34f, 0.09f, 0.06f), magmaCrimson, magmaCrimson * 2.5f);
            ShaderHelper.CreateVoxelPart("CrownCenter", headPivot, new Vector3(0f, 0.30f, 0f), new Vector3(0.48f, 0.14f, 0.48f), goldCrown);
            ShaderHelper.CreateVoxelPart("LeftHorn", headPivot, new Vector3(-0.28f, 0.42f, 0.05f), new Vector3(0.12f, 0.38f, 0.12f), goldCrown);
            ShaderHelper.CreateVoxelPart("RightHorn", headPivot, new Vector3(0.28f, 0.42f, 0.05f), new Vector3(0.12f, 0.38f, 0.12f), goldCrown);

            // 3. Tay Phải + Đại Đao Hắc Thạch (Colossal Obsidian Greatsword)
            rightArmPivot = new GameObject("RightArmPivot").transform;
            rightArmPivot.SetParent(torsoPivot, false);
            rightArmPivot.localPosition = new Vector3(0.48f, 0.26f, 0f);

            ShaderHelper.CreateVoxelPart("RightShoulder", rightArmPivot, new Vector3(0.08f, 0.10f, 0f), new Vector3(0.36f, 0.32f, 0.36f), obsidianTrim);
            ShaderHelper.CreateVoxelPart("RightArm", rightArmPivot, new Vector3(0.05f, -0.26f, 0f), new Vector3(0.24f, 0.60f, 0.24f), obsidianDark);

            // Đại Đao của Boss
            ShaderHelper.CreateVoxelPart("BossSwordHandle", rightArmPivot, new Vector3(0.05f, -0.52f, 0.22f), new Vector3(0.10f, 0.10f, 0.48f), goldCrown);
            ShaderHelper.CreateVoxelPart("BossSwordGuard", rightArmPivot, new Vector3(0.05f, -0.52f, 0.44f), new Vector3(0.42f, 0.12f, 0.12f), goldCrown);
            GameObject bossBlade = ShaderHelper.CreateVoxelPart("BossColossalBlade", rightArmPivot, new Vector3(0.05f, -0.52f, 1.25f), new Vector3(0.12f, 0.22f, 1.60f), obsidianDark, _defaultBladeEmission);
            weaponBladeRenderer = bossBlade.GetComponent<Renderer>();

            weaponTip = new GameObject("WeaponTip").transform;
            weaponTip.SetParent(rightArmPivot, false);
            weaponTip.localPosition = new Vector3(0.05f, -0.52f, 2.05f);

            // 4. Tay Trái Boss
            leftArmPivot = new GameObject("LeftArmPivot").transform;
            leftArmPivot.SetParent(torsoPivot, false);
            leftArmPivot.localPosition = new Vector3(-0.48f, 0.26f, 0f);

            ShaderHelper.CreateVoxelPart("LeftShoulder", leftArmPivot, new Vector3(-0.08f, 0.10f, 0f), new Vector3(0.36f, 0.32f, 0.36f), obsidianTrim);
            ShaderHelper.CreateVoxelPart("LeftArm", leftArmPivot, new Vector3(-0.05f, -0.26f, 0f), new Vector3(0.24f, 0.60f, 0.24f), obsidianDark);

            // 5. Chân Boss
            rightLegPivot = new GameObject("RightLegPivot").transform;
            rightLegPivot.SetParent(rootVisual, false);
            rightLegPivot.localPosition = new Vector3(0.20f, 0.60f, 0f);
            ShaderHelper.CreateVoxelPart("RightLeg", rightLegPivot, new Vector3(0f, -0.30f, 0f), new Vector3(0.26f, 0.60f, 0.26f), obsidianDark);

            leftLegPivot = new GameObject("LeftLegPivot").transform;
            leftLegPivot.SetParent(rootVisual, false);
            leftLegPivot.localPosition = new Vector3(-0.20f, 0.60f, 0f);
            ShaderHelper.CreateVoxelPart("LeftLeg", leftLegPivot, new Vector3(0f, -0.30f, 0f), new Vector3(0.26f, 0.60f, 0.26f), obsidianDark);
        }

        private void ClearExistingVisuals()
        {
            if (rootVisual != null)
            {
                if (Application.isPlaying)
                    Destroy(rootVisual.gameObject);
                else
                    DestroyImmediate(rootVisual.gameObject);
            }
        }

        public void TriggerAttackAnimation(int comboIndex, float duration = 0.35f)
        {
            _attackComboType = comboIndex;
            _attackDuration = Mathf.Max(0.15f, duration);
            _attackAnimTimer = _attackDuration;
        }

        public void TriggerRollAnimation(float duration = 0.38f)
        {
            _isRolling = true;
            _rollTimer = duration;
        }

        public void SetParryStance(bool active, bool isPerfectFlash = false)
        {
            _isParryingStance = active;
            if (weaponBladeRenderer != null)
            {
                Color emission = isPerfectFlash
                    ? new Color(1f, 0.9f, 0.25f) * 3.5f
                    : (active ? new Color(0.35f, 0.85f, 1f) * 2.2f : _defaultBladeEmission);
                if (weaponBladeRenderer.material.HasProperty("_EmissionColor"))
                    weaponBladeRenderer.material.SetColor("_EmissionColor", emission);
            }
        }

        public void SetPostureBrokenStance(bool broken)
        {
            _isPostureBrokenStance = broken;
        }

        public void UpdateProceduralAnimation(float moveSpeedNormalized, bool isGrounded)
        {
            if (rootVisual == null || torsoPivot == null) return;

            float dt = Time.deltaTime;

            // Xử lý Lộn Nhào (Dodge Roll 360 độ kiểu Elden Ring)
            if (_isRolling)
            {
                _rollTimer -= dt;
                float rollProgress = 1f - Mathf.Clamp01(_rollTimer / 0.38f);
                torsoPivot.localRotation = Quaternion.Euler(rollProgress * 360f, 0f, 0f);
                if (_rollTimer <= 0f)
                {
                    _isRolling = false;
                    torsoPivot.localRotation = Quaternion.identity;
                }
                return;
            }

            // Xử lý trạng thái Quỳ Gối khi Bị Phá Thế (Posture Broken - chờ Kết Liễu)
            if (_isPostureBrokenStance)
            {
                torsoPivot.localPosition = Vector3.Lerp(torsoPivot.localPosition, new Vector3(0f, 0.65f, 0f), dt * 10f);
                torsoPivot.localRotation = Quaternion.Slerp(torsoPivot.localRotation, Quaternion.Euler(28f, 0f, 0f), dt * 10f);
                rightArmPivot.localRotation = Quaternion.Slerp(rightArmPivot.localRotation, Quaternion.Euler(40f, 0f, -25f), dt * 10f);
                leftArmPivot.localRotation = Quaternion.Slerp(leftArmPivot.localRotation, Quaternion.Euler(35f, 0f, 25f), dt * 10f);
                return;
            }

            // Nhịp thở & Bước chạy (Walk / Sprint Cycle)
            if (moveSpeedNormalized > 0.05f && isGrounded)
            {
                _walkCycle += dt * Mathf.Lerp(8f, 15f, moveSpeedNormalized);
                float legSwing = Mathf.Sin(_walkCycle) * 38f * moveSpeedNormalized;
                float bob = Mathf.Abs(Mathf.Sin(_walkCycle)) * 0.06f * moveSpeedNormalized;

                torsoPivot.localPosition = new Vector3(0f, (isBoss ? 1.0f : 0.95f) + bob, 0f);
                torsoPivot.localRotation = Quaternion.Euler(8f * moveSpeedNormalized, 0f, 0f);

                rightLegPivot.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
                leftLegPivot.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);
            }
            else
            {
                _walkCycle += dt * 2.5f;
                float idleBreath = Mathf.Sin(_walkCycle) * 0.025f;
                torsoPivot.localPosition = new Vector3(0f, (isBoss ? 1.0f : 0.95f) + idleBreath, 0f);
                torsoPivot.localRotation = Quaternion.Slerp(torsoPivot.localRotation, Quaternion.identity, dt * 10f);

                rightLegPivot.localRotation = Quaternion.Slerp(rightLegPivot.localRotation, Quaternion.identity, dt * 10f);
                leftLegPivot.localRotation = Quaternion.Slerp(leftLegPivot.localRotation, Quaternion.identity, dt * 10f);
            }

            // Hoạt ảnh Tay & Vũ Khí (Đánh kiếm, Đỡ đòn Sekiro Parry, hoặc Chạy)
            if (_attackAnimTimer > 0f)
            {
                _attackAnimTimer -= dt;
                float t = 1f - Mathf.Clamp01(_attackAnimTimer / _attackDuration);

                if (_attackComboType == 1) // Đòn 1: Chém ngang từ phải sang trái
                {
                    float swingYaw = Mathf.Lerp(-75f, 65f, SmoothSwing(t));
                    torsoPivot.localRotation = Quaternion.Euler(10f, swingYaw * 0.35f, 0f);
                    rightArmPivot.localRotation = Quaternion.Euler(-15f, swingYaw, -70f);
                    leftArmPivot.localRotation = Quaternion.Euler(-20f, 0f, -20f);
                }
                else if (_attackComboType == 2) // Đòn 2: Chém ngược từ trái sang phải
                {
                    float swingYaw = Mathf.Lerp(70f, -75f, SmoothSwing(t));
                    torsoPivot.localRotation = Quaternion.Euler(10f, swingYaw * 0.35f, 0f);
                    rightArmPivot.localRotation = Quaternion.Euler(-10f, swingYaw, 70f);
                    leftArmPivot.localRotation = Quaternion.Euler(-25f, 0f, -15f);
                }
                else if (_attackComboType == 3) // Đòn 3: Bổ dọc uy lực từ trên xuống
                {
                    float pitch = Mathf.Lerp(-110f, 55f, SmoothSwing(t));
                    torsoPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-15f, 22f, SmoothSwing(t)), 0f, 0f);
                    rightArmPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                    leftArmPivot.localRotation = Quaternion.Euler(pitch * 0.6f, 0f, 0f);
                }
                else if (_attackComboType == 4) // Đòn 4: Kết Liễu (Deathblow) / Chưởng phép
                {
                    float thrust = Mathf.Sin(t * Mathf.PI);
                    torsoPivot.localRotation = Quaternion.Euler(20f * thrust, 25f * thrust, 0f);
                    rightArmPivot.localRotation = Quaternion.Euler(-25f * thrust, -15f, 0f);
                    leftArmPivot.localRotation = Quaternion.Euler(-75f * thrust, 15f, 0f);
                }
                else if (_attackComboType == 5) // Boss Quét Hiểm Cảnh 360 độ (Perilous Sweep)
                {
                    float spin = t * 360f;
                    torsoPivot.localRotation = Quaternion.Euler(12f, spin, 0f);
                    rightArmPivot.localRotation = Quaternion.Euler(0f, 0f, -85f);
                }
            }
            else if (_isParryingStance)
            {
                // Tư thế Đỡ Đòn / Parry chuẩn Sekiro: Nâng ngang thanh kiếm chắn trước ngực
                rightArmPivot.localRotation = Quaternion.Slerp(rightArmPivot.localRotation, Quaternion.Euler(-45f, -55f, -65f), dt * 22f);
                leftArmPivot.localRotation = Quaternion.Slerp(leftArmPivot.localRotation, Quaternion.Euler(-55f, 35f, 25f), dt * 22f);
            }
            else
            {
                float armSwing = (moveSpeedNormalized > 0.05f) ? -Mathf.Sin(_walkCycle) * 24f * moveSpeedNormalized : 0f;
                rightArmPivot.localRotation = Quaternion.Slerp(rightArmPivot.localRotation, Quaternion.Euler(armSwing - 10f, 0f, 0f), dt * 10f);
                leftArmPivot.localRotation = Quaternion.Slerp(leftArmPivot.localRotation, Quaternion.Euler(-armSwing - 5f, 0f, 0f), dt * 10f);
            }
        }

        private float SmoothSwing(float t)
        {
            // Khởi đầu lấy đà nhanh, vung kiếm cực mạnh ở giữa chu kỳ
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.25f));
        }
    }
}

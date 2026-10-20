using UnityEngine;
using VoxelSouls.Boss;
using VoxelSouls.Core;
using VoxelSouls.Player;
using VoxelSouls.World;

namespace VoxelSouls.UI
{
    /// <summary>
    /// Giao diện người dùng (HUD) lấy cảm hứng từ Elden Ring & Sekiro:
    /// - Góc Trên Trái (Elden Ring): Thanh Máu (HP đỏ), Thanh Phép (FP xanh lam), Thanh Thể Lực (Stamina xanh lá) & Buff Cổ Ngữ
    /// - Giữa Phía Dưới (Sekiro): Thanh Thế Chịu Đựng (Posture Bar) của Dũng Sĩ
    /// - Góc Dưới Trái (Elden Ring Quick-Slots): Ô Phép Thuật đang chọn, Bình Máu Đỏ [R], Bình Năng Lượng Xanh [F]
    /// - Giữa Phía Trên (Boss Fight): Tên Boss + Thanh Máu Boss + Thanh Thế (Posture) của Boss tỏa ra từ tâm
    /// - Cảnh báo Thế Giới 3D: Chữ [危 HIỂM CẢNH] màu đỏ rực khi Boss ra đòn không thể đỡ, và Tâm Đỏ [◉ KẾT LIỄU] khi Boss vỡ thế!
    /// </summary>
    public class SoulsHUD : MonoBehaviour
    {
        private PlayerCombat _playerCombat;
        private PlayerController _playerController;
        private BossController _boss;
        private bool _showControlsHelp = true;

        private Texture2D _whiteTex;

        private void Awake()
        {
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                _showControlsHelp = !_showControlsHelp;
            }
        }

        private void FindReferences()
        {
            if (_playerCombat == null)
                _playerCombat = FindObjectOfType<PlayerCombat>();
            if (_playerController == null && _playerCombat != null)
                _playerController = _playerCombat.GetComponent<PlayerController>();
            if (_boss == null)
                _boss = FindObjectOfType<BossController>();
        }

        private void OnGUI()
        {
            FindReferences();
            if (_playerCombat == null) return;

            DrawPlayerTopLeftBars();
            DrawPlayerBottomPostureBar();
            DrawQuickSlotsBottomLeft();
            DrawBossHeaderBars();
            DrawWorldSpaceMarkersAndPopups();
            DrawControlsGuideTopRight();
            DrawVictoryOrDefeatBanner();
        }

        private void DrawPlayerTopLeftBars()
        {
            var stats = _playerCombat.Stats;
            float x = 26f;
            float y = 24f;

            // Khung nền mờ
            DrawRect(new Rect(x - 8f, y - 8f, 340f, 92f), new Color(0.05f, 0.05f, 0.08f, 0.72f));

            // 1. Thanh Máu (HP - Crimson)
            DrawBar(new Rect(x, y, 280f, 18f), stats.currentHP / stats.maxHP,
                new Color(0.78f, 0.14f, 0.16f), new Color(0.18f, 0.05f, 0.05f),
                $"HP  {Mathf.CeilToInt(stats.currentHP)} / {Mathf.CeilToInt(stats.maxHP)}");

            // 2. Thanh Phép (FP - Cerulean)
            DrawBar(new Rect(x, y + 24f, 220f, 15f), stats.currentFP / stats.maxFP,
                new Color(0.16f, 0.48f, 0.85f), new Color(0.05f, 0.10f, 0.22f),
                $"FP  {Mathf.CeilToInt(stats.currentFP)} / {Mathf.CeilToInt(stats.maxFP)}");

            // 3. Thanh Thể Lực (Stamina - Viridian)
            DrawBar(new Rect(x, y + 45f, 250f, 14f), stats.currentStamina / stats.maxStamina,
                new Color(0.18f, 0.68f, 0.32f), new Color(0.05f, 0.18f, 0.08f),
                $"STM {Mathf.CeilToInt(stats.currentStamina)} / {Mathf.CeilToInt(stats.maxStamina)}");

            // Hiển thị Buff Cổ Ngữ
            GUIStyle buffStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.35f) }
            };
            GUI.Label(new Rect(x, y + 62f, 320f, 20f),
                $"⚔ Công Lực: {Mathf.RoundToInt(stats.attackDamageMultiplier * 100f)}%   |   ✦ Cổ Ngữ: {_playerCombat.runesCollected}/3",
                buffStyle);
        }

        private void DrawPlayerBottomPostureBar()
        {
            var stats = _playerCombat.Stats;
            float width = 320f;
            float height = 12f;
            float centerX = Screen.width * 0.5f;
            float y = Screen.height - 58f;

            DrawRect(new Rect(centerX - width * 0.5f - 2f, y - 2f, width + 4f, height + 4f), new Color(0.08f, 0.08f, 0.1f, 0.85f));

            float ratio = Mathf.Clamp01(stats.currentPosture / stats.maxPosture);
            float fillW = width * ratio;
            Color postureColor = _playerCombat.IsBlockingOrParrying
                ? new Color(0.35f, 0.9f, 1f)
                : Color.Lerp(new Color(1f, 0.82f, 0.22f), new Color(1f, 0.22f, 0.15f), ratio);

            // Thanh Posture chuẩn Sekiro mở rộng từ chính giữa sang 2 bên
            DrawRect(new Rect(centerX - fillW * 0.5f, y, fillW, height), postureColor);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.9f, 0.75f) }
            };
            string status = _playerCombat.IsInPerfectParryWindow
                ? "⚡ KHUNG PARRY HOÀN HẢO (DEFLECT READY)"
                : (_playerCombat.IsBlockingOrParrying ? "🛡 ĐANG ĐỠ ĐÒN (GUARD)" : "THẾ CHỊU ĐỰNG DŨNG SĨ (POSTURE)");
            GUI.Label(new Rect(centerX - 200f, y - 20f, 400f, 20f), status, labelStyle);
        }

        private void DrawQuickSlotsBottomLeft()
        {
            float x = 26f;
            float y = Screen.height - 135f;

            DrawRect(new Rect(x - 8f, y - 8f, 345f, 110f), new Color(0.05f, 0.05f, 0.08f, 0.78f));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.88f, 1f) }
            };
            GUIStyle itemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(x, y, 330f, 22f), $"✦ PHÉP [Q / E]: {PlayerCombat.GetSpellDisplayName(_playerCombat.currentSpell)}", titleStyle);
            GUI.Label(new Rect(x, y + 22f, 330f, 20f), "   (Nhấn phím [1] [2] [3] để đổi Phép Thuật Elden Ring)", new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = new Color(0.75f, 0.8f, 0.88f) } });

            itemStyle.normal.textColor = new Color(1f, 0.38f, 0.38f);
            GUI.Label(new Rect(x, y + 48f, 320f, 22f), $"🍷 [Phím R] Bình Máu Đỏ (Crimson Flask): {_playerCombat.crimsonFlasks} / {_playerCombat.maxCrimsonFlasks}", itemStyle);

            itemStyle.normal.textColor = new Color(0.35f, 0.78f, 1f);
            GUI.Label(new Rect(x, y + 72f, 320f, 22f), $"💧 [Phím F] Bình Năng Lượng (Cerulean Flask): {_playerCombat.ceruleanFlasks} / {_playerCombat.maxCeruleanFlasks}", itemStyle);
        }

        private void DrawBossHeaderBars()
        {
            if (_boss == null || _boss.Stats.IsDead) return;
            if (_boss.state == BossState.IdleDormant && Vector3.Distance(_playerCombat.transform.position, _boss.transform.position) > 22f)
                return;

            float width = Mathf.Min(640f, Screen.width * 0.56f);
            float centerX = Screen.width * 0.5f;
            float left = centerX - width * 0.5f;
            float y = 24f;

            DrawRect(new Rect(left - 12f, y - 8f, width + 24f, 88f), new Color(0.04f, 0.04f, 0.07f, 0.82f));

            // Tên Boss + Chấm đỏ Giai Đoạn kiểu Sekiro
            GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.88f, 0.72f) }
            };
            string phaseDots = (_boss.currentPhase == 1) ? "🔴 🔴 (Giai Đoạn 1/2)" : "🔴 (Giai Đoạn 2: Cuồng Nộ)";
            GUI.Label(new Rect(left, y, width, 22f), $"{_boss.bossName}   {phaseDots}", nameStyle);

            // Thanh Máu Boss (HP)
            var bStats = _boss.Stats;
            DrawBar(new Rect(left, y + 25f, width, 18f), bStats.currentHP / bStats.maxHP,
                new Color(0.75f, 0.12f, 0.14f), new Color(0.16f, 0.04f, 0.05f),
                $"HP: {Mathf.CeilToInt(bStats.currentHP)} / {Mathf.CeilToInt(bStats.maxHP)}");

            // Thanh Thế Chịu Đựng của Boss (Sekiro Posture Bar - tỏa ra từ chính giữa!)
            float postureY = y + 48f;
            float postureH = 14f;
            DrawRect(new Rect(left, postureY, width, postureH), new Color(0.14f, 0.11f, 0.06f, 0.95f));

            float pRatio = Mathf.Clamp01(bStats.currentPosture / bStats.maxPosture);
            float pFillW = width * pRatio;
            Color pColor = bStats.isPostureBroken
                ? new Color(1f, 0.18f, 0.18f)
                : Color.Lerp(new Color(1f, 0.78f, 0.18f), new Color(1f, 0.35f, 0.08f), pRatio);
            DrawRect(new Rect(centerX - pFillW * 0.5f, postureY, pFillW, postureH), pColor);

            GUIStyle postureText = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            string pLabel = bStats.isPostureBroken
                ? "💥 BOSS VỠ THẾ! ÁP SÁT NHẤN [CHUỘT TRÁI] ĐỂ KẾT LIỄU (DEATHBLOW)!"
                : $"THẾ CHỊU ĐỰNG BOSS (POSTURE): {Mathf.RoundToInt(pRatio * 100f)}%  (Parry Chuột Phải để làm đầy!)";
            GUI.Label(new Rect(left, postureY - 2f, width, 18f), pLabel, postureText);

            if (!string.IsNullOrEmpty(_boss.currentTelegraphText))
            {
                GUIStyle teleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = _boss.showPerilousKanji ? new Color(1f, 0.25f, 0.25f) : new Color(1f, 0.88f, 0.35f) }
                };
                GUI.Label(new Rect(left, y + 64f, width, 20f), _boss.currentTelegraphText, teleStyle);
            }
        }

        private void DrawWorldSpaceMarkersAndPopups()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) return;

            // 1. Hiển thị biểu tượng Khóa Mục Tiêu (Lock-On), Chữ 危 (Hiểm Cảnh) & Tâm Đỏ Kết Liễu trên người Boss
            if (_boss != null && !_boss.Stats.IsDead)
            {
                Vector3 bossScreen = cam.WorldToScreenPoint(_boss.transform.position + Vector3.up * 2.2f);
                if (bossScreen.z > 0f)
                {
                    float guiX = bossScreen.x;
                    float guiY = Screen.height - bossScreen.y;

                    // Tâm Đỏ Kết Liễu (Sekiro Deathblow Marker)
                    if (_boss.Stats.isPostureBroken)
                    {
                        GUIStyle dbStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 26,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(1f, 0.15f, 0.15f) }
                        };
                        GUI.Label(new Rect(guiX - 150f, guiY - 35f, 300f, 60f), "◉ KẾT LIỄU [LMB]", dbStyle);
                    }
                    else if (_playerController != null && _playerController.isLockedOn)
                    {
                        GUIStyle lockStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 18,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(1f, 0.92f, 0.65f, 0.9f) }
                        };
                        GUI.Label(new Rect(guiX - 25f, guiY - 15f, 50f, 30f), "◈", lockStyle);
                    }

                    // Chữ 危 (HIỂM CẢNH - Đòn Không Thể Đỡ chuẩn Sekiro)
                    if (_boss.showPerilousKanji)
                    {
                        Vector3 kanjiScreen = cam.WorldToScreenPoint(_boss.transform.position + Vector3.up * 4.5f);
                        float ky = Screen.height - kanjiScreen.y;
                        GUIStyle kanjiStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 34,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(1f, 0.12f, 0.12f) }
                        };
                        GUI.Label(new Rect(kanjiScreen.x - 160f, ky - 30f, 320f, 55f), "危 HIỂM CẢNH!", kanjiStyle);
                    }
                }
            }

            // 2. Hiển thị nhãn Vật Phẩm RPG khi đứng gần
            WorldItemPickup[] pickups = FindObjectsOfType<WorldItemPickup>();
            foreach (var item in pickups)
            {
                if (item.isConsumed) continue;
                float dist = Vector3.Distance(_playerCombat.transform.position, item.transform.position);
                if (dist <= 6.5f)
                {
                    Vector3 sPos = cam.WorldToScreenPoint(item.transform.position + Vector3.up * 1.9f);
                    if (sPos.z > 0f)
                    {
                        GUIStyle itemLabel = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(1f, 0.92f, 0.45f) }
                        };
                        GUI.Label(new Rect(sPos.x - 140f, Screen.height - sPos.y - 15f, 280f, 26f), item.itemName, itemLabel);
                    }
                }
            }

            // 3. Hiển thị các thông báo sát thương / Parry nổi (Floating Combat Text)
            if (GameManager.Instance != null)
            {
                foreach (var popup in GameManager.Instance.activePopups)
                {
                    Vector3 sp = cam.WorldToScreenPoint(popup.worldPosition);
                    if (sp.z > 0f)
                    {
                        float alpha = 1f - Mathf.Clamp01(popup.timer / popup.duration);
                        Color c = popup.color;
                        c.a = alpha;
                        GUIStyle pStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = popup.fontSize,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = c }
                        };
                        GUI.Label(new Rect(sp.x - 180f, Screen.height - sp.y - 16f, 360f, 34f), popup.text, pStyle);
                    }
                }
            }
        }

        private void DrawControlsGuideTopRight()
        {
            float w = 310f;
            float x = Screen.width - w - 20f;
            float y = 24f;

            if (!_showControlsHelp)
            {
                DrawRect(new Rect(x + 110f, y, 200f, 28f), new Color(0.05f, 0.05f, 0.08f, 0.7f));
                GUI.Label(new Rect(x + 120f, y + 4f, 190f, 22f), "[Phím H] Hiện Bảng Điều Khiển");
                return;
            }

            DrawRect(new Rect(x - 10f, y - 6f, w + 16f, 218f), new Color(0.05f, 0.05f, 0.08f, 0.78f));

            GUIStyle header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUIStyle body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f) }
            };

            GUI.Label(new Rect(x, y, w, 20f), "🎮 BẢNG ĐIỀU KHIỂN (Nhấn [H] Ẩn/Hiện)", header);
            GUI.Label(new Rect(x, y + 24f, w, 18f), "• WASD + Shift: Di chuyển & Chạy nhanh", body);
            GUI.Label(new Rect(x, y + 44f, w, 18f), "• Chuột Trái (LMB): Chém Combo / Kết Liễu Boss", body);
            GUI.Label(new Rect(x, y + 64f, w, 18f), "• Chuột Phải (RMB): Đỡ Đòn & PARRY (Sekiro)", body);
            GUI.Label(new Rect(x, y + 84f, w, 18f), "• Space (x2 gần Boss): Nhảy & Đạp Đầu Phá Thế", body);
            GUI.Label(new Rect(x, y + 104f, w, 18f), "• Phím V / Alt / Ctrl: Lộn Nhào Né Đòn (i-frame)", body);
            GUI.Label(new Rect(x, y + 124f, w, 18f), "• Phím 1 / 2 / 3: Đổi Phép | Q hoặc E: Chưởng Phép", body);
            GUI.Label(new Rect(x, y + 144f, w, 18f), "• Phím R / F: Uống Bình Máu Đỏ / Bình Phép Xanh", body);
            GUI.Label(new Rect(x, y + 164f, w, 18f), "• Phím Tab: Bật/Tắt Khóa Mục Tiêu (Lock-On)", body);
            GUI.Label(new Rect(x, y + 184f, w, 18f), "• Phím G: Nghỉ tại Điểm Ân Điển | F5: Chơi Lại", body);
        }

        private void DrawVictoryOrDefeatBanner()
        {
            if (GameManager.Instance == null || GameManager.Instance.bannerAlpha <= 0.01f) return;

            float alpha = GameManager.Instance.bannerAlpha;
            float bannerY = Screen.height * 0.42f;

            DrawRect(new Rect(0f, bannerY - 48f, Screen.width, 108f), new Color(0.02f, 0.02f, 0.03f, 0.86f * alpha));

            Color titleColor = GameManager.Instance.bannerColor;
            titleColor.a = alpha;

            GUIStyle mainStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = titleColor }
            };

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f, alpha) }
            };

            GUI.Label(new Rect(0f, bannerY - 34f, Screen.width, 46f), GameManager.Instance.bannerTitle, mainStyle);
            GUI.Label(new Rect(0f, bannerY + 14f, Screen.width, 28f), GameManager.Instance.bannerSubtitle, subStyle);
        }

        private void DrawBar(Rect rect, float ratio, Color fillColor, Color bgColor, string label)
        {
            ratio = Mathf.Clamp01(ratio);
            DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color(0.85f, 0.78f, 0.55f, 0.5f));
            DrawRect(rect, bgColor);
            DrawRect(new Rect(rect.x, rect.y, rect.width * ratio, rect.height), fillColor);

            GUIStyle txt = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(rect.x + 6f, rect.y - 1f, rect.width, rect.height + 2f), label, txt);
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = prev;
        }
    }
}

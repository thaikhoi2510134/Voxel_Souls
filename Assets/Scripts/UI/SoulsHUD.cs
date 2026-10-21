using UnityEngine;
using VoxelSouls.Boss;
using VoxelSouls.Core;
using VoxelSouls.Player;
using VoxelSouls.World;

namespace VoxelSouls.UI
{
    /// <summary>
    /// Giao diện người dùng (HUD) lấy cảm hứng từ Elden Ring & Sekiro (Đã tối ưu cho Góc Nhìn Thứ Ba):
    /// - Góc Trên Trái: Thanh Máu (HP đỏ), Thanh Phép (FP xanh lam), Thanh Thể Lực (Stamina xanh lá) & Buff Cổ Ngữ
    /// - Giữa Phía Dưới: Thanh Thế Chịu Đựng (Posture Bar) của Dũng Sĩ
    /// - Góc Dưới Trái: Ô Phép Thuật đang chọn, Bình Máu Đỏ [R], Bình Năng Lượng Xanh [F]
    /// - Phía Trên (Boss Fight): Tên Boss + Thanh Máu Boss + Thanh Thế (Posture) gọn gàng không che khuất tầm nhìn thứ ba
    /// - Đã sửa lỗi kẹt bảng giới thiệu (Banner tự động tắt mượt mà sau 2 giây và không che giữa màn hình khi đang đánh Boss).
    /// </summary>
    public class SoulsHUD : MonoBehaviour
    {
        private PlayerCombat _playerCombat;
        private PlayerController _playerController;
        private BossController _boss;
        private bool _showControlsHelp = false; // Mặc định thu gọn để màn hình đánh Boss thoáng đãng, nhấn [H] để mở

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
            float x = 22f;
            float y = 20f;

            DrawRect(new Rect(x - 6f, y - 6f, 290f, 84f), new Color(0.04f, 0.04f, 0.07f, 0.65f));

            // 1. Thanh Máu (HP - Crimson)
            DrawBar(new Rect(x, y, 250f, 16f), stats.currentHP / stats.maxHP,
                new Color(0.78f, 0.14f, 0.16f), new Color(0.18f, 0.05f, 0.05f),
                $"HP  {Mathf.CeilToInt(stats.currentHP)} / {Mathf.CeilToInt(stats.maxHP)}");

            // 2. Thanh Phép (FP - Cerulean)
            DrawBar(new Rect(x, y + 21f, 195f, 14f), stats.currentFP / stats.maxFP,
                new Color(0.16f, 0.48f, 0.85f), new Color(0.05f, 0.10f, 0.22f),
                $"FP  {Mathf.CeilToInt(stats.currentFP)} / {Mathf.CeilToInt(stats.maxFP)}");

            // 3. Thanh Thể Lực (Stamina - Viridian)
            DrawBar(new Rect(x, y + 39f, 220f, 13f), stats.currentStamina / stats.maxStamina,
                new Color(0.18f, 0.68f, 0.32f), new Color(0.05f, 0.18f, 0.08f),
                $"STM {Mathf.CeilToInt(stats.currentStamina)} / {Mathf.CeilToInt(stats.maxStamina)}");

            GUIStyle buffStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.35f) }
            };
            GUI.Label(new Rect(x, y + 55f, 280f, 18f),
                $"⚔ Công: {Mathf.RoundToInt(stats.attackDamageMultiplier * 100f)}%  |  ✦ Cổ Ngữ: {_playerCombat.runesCollected}/3",
                buffStyle);
        }

        private void DrawPlayerBottomPostureBar()
        {
            var stats = _playerCombat.Stats;
            float width = 280f;
            float height = 10f;
            float centerX = Screen.width * 0.5f;
            float y = Screen.height - 42f;

            DrawRect(new Rect(centerX - width * 0.5f - 2f, y - 2f, width + 4f, height + 4f), new Color(0.06f, 0.06f, 0.09f, 0.8f));

            float ratio = Mathf.Clamp01(stats.currentPosture / stats.maxPosture);
            float fillW = width * ratio;
            Color postureColor = _playerCombat.IsBlockingOrParrying
                ? new Color(0.35f, 0.9f, 1f)
                : Color.Lerp(new Color(1f, 0.82f, 0.22f), new Color(1f, 0.22f, 0.15f), ratio);

            DrawRect(new Rect(centerX - fillW * 0.5f, y, fillW, height), postureColor);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.9f, 0.75f, 0.9f) }
            };
            string status = _playerCombat.IsInPerfectParryWindow
                ? "⚡ PARRY HOÀN HẢO (DEFLECT)"
                : (_playerCombat.IsBlockingOrParrying ? "🛡 ĐANG ĐỠ ĐÒN" : "THẾ CHỊU ĐỰNG (POSTURE)");
            GUI.Label(new Rect(centerX - 160f, y - 18f, 320f, 18f), status, labelStyle);
        }

        private void DrawQuickSlotsBottomLeft()
        {
            float x = 22f;
            float y = Screen.height - 108f;

            DrawRect(new Rect(x - 6f, y - 6f, 305f, 88f), new Color(0.04f, 0.04f, 0.07f, 0.68f));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.88f, 1f) }
            };
            GUIStyle itemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(x, y, 295f, 20f), $"✦ Phép [Q/E | Đổi 1-3]: {PlayerCombat.GetSpellDisplayName(_playerCombat.currentSpell)}", titleStyle);

            itemStyle.normal.textColor = new Color(1f, 0.42f, 0.42f);
            GUI.Label(new Rect(x, y + 26f, 290f, 20f), $"🍷 [Phím R] Bình Máu Đỏ: {_playerCombat.crimsonFlasks} / {_playerCombat.maxCrimsonFlasks}", itemStyle);

            itemStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);
            GUI.Label(new Rect(x, y + 50f, 290f, 20f), $"💧 [Phím F] Bình Phép Xanh: {_playerCombat.ceruleanFlasks} / {_playerCombat.maxCeruleanFlasks}", itemStyle);
        }

        private void DrawBossHeaderBars()
        {
            if (_boss == null || _boss.Stats.IsDead) return;
            if (_boss.state == BossState.IdleDormant && Vector3.Distance(_playerCombat.transform.position, _boss.transform.position) > 22f)
                return;

            float width = Mathf.Min(540f, Screen.width * 0.46f);
            float centerX = Screen.width * 0.5f;
            float left = centerX - width * 0.5f;
            float y = 18f;

            DrawRect(new Rect(left - 10f, y - 6f, width + 20f, 76f), new Color(0.04f, 0.04f, 0.07f, 0.72f));

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.88f, 0.72f) }
            };
            string phaseDots = (_boss.currentPhase == 1) ? "🔴 🔴 (Giai Đoạn 1)" : "🔴 (Giai Đoạn 2: Cuồng Nộ)";
            GUI.Label(new Rect(left, y, width, 20f), $"{_boss.bossName}   {phaseDots}", nameStyle);

            // Thanh Máu Boss (HP)
            var bStats = _boss.Stats;
            DrawBar(new Rect(left, y + 22f, width, 15f), bStats.currentHP / bStats.maxHP,
                new Color(0.75f, 0.12f, 0.14f), new Color(0.16f, 0.04f, 0.05f),
                $"HP: {Mathf.CeilToInt(bStats.currentHP)} / {Mathf.CeilToInt(bStats.maxHP)}");

            // Thanh Thế Chịu Đựng của Boss (Sekiro Posture Bar)
            float postureY = y + 41f;
            float postureH = 12f;
            DrawRect(new Rect(left, postureY, width, postureH), new Color(0.14f, 0.11f, 0.06f, 0.9f));

            float pRatio = Mathf.Clamp01(bStats.currentPosture / bStats.maxPosture);
            float pFillW = width * pRatio;
            Color pColor = bStats.isPostureBroken
                ? new Color(1f, 0.18f, 0.18f)
                : Color.Lerp(new Color(1f, 0.78f, 0.18f), new Color(1f, 0.35f, 0.08f), pRatio);
            DrawRect(new Rect(centerX - pFillW * 0.5f, postureY, pFillW, postureH), pColor);

            GUIStyle postureText = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            string pLabel = bStats.isPostureBroken
                ? "💥 VỠ THẾ! ÁP SÁT NHẤN [CHUỘT TRÁI] ĐỂ KẾT LIỄU!"
                : $"THẾ BOSS (POSTURE): {Mathf.RoundToInt(pRatio * 100f)}%";
            GUI.Label(new Rect(left, postureY - 2f, width, 16f), pLabel, postureText);

            if (!string.IsNullOrEmpty(_boss.currentTelegraphText))
            {
                GUIStyle teleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = _boss.showPerilousKanji ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.88f, 0.35f) }
                };
                GUI.Label(new Rect(left, y + 54f, width, 18f), _boss.currentTelegraphText, teleStyle);
            }
        }

        private void DrawWorldSpaceMarkersAndPopups()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null) return;

            if (_boss != null && !_boss.Stats.IsDead)
            {
                Vector3 bossScreen = cam.WorldToScreenPoint(_boss.transform.position + Vector3.up * 2.3f);
                if (bossScreen.z > 0.5f)
                {
                    float guiX = Mathf.Clamp(bossScreen.x, 80f, Screen.width - 80f);
                    float guiY = Mathf.Clamp(Screen.height - bossScreen.y, 80f, Screen.height - 80f);

                    if (_boss.Stats.isPostureBroken)
                    {
                        GUIStyle dbStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 24,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(1f, 0.15f, 0.15f) }
                        };
                        GUI.Label(new Rect(guiX - 140f, guiY - 25f, 280f, 50f), "◉ KẾT LIỄU [LMB]", dbStyle);
                    }
                    else if (_playerController != null && _playerController.isLockedOn)
                    {
                        GUIStyle lockStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 18,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = new Color(1f, 0.95f, 0.7f, 0.9f) }
                        };
                        GUI.Label(new Rect(guiX - 25f, guiY - 15f, 50f, 30f), "◈", lockStyle);
                    }

                    if (_boss.showPerilousKanji)
                    {
                        Vector3 kanjiScreen = cam.WorldToScreenPoint(_boss.transform.position + Vector3.up * 4.4f);
                        if (kanjiScreen.z > 0.5f)
                        {
                            float ky = Mathf.Clamp(Screen.height - kanjiScreen.y, 90f, Screen.height - 120f);
                            GUIStyle kanjiStyle = new GUIStyle(GUI.skin.label)
                            {
                                alignment = TextAnchor.MiddleCenter,
                                fontSize = 30,
                                fontStyle = FontStyle.Bold,
                                normal = { textColor = new Color(1f, 0.15f, 0.15f) }
                            };
                            GUI.Label(new Rect(kanjiScreen.x - 150f, ky - 25f, 300f, 50f), "危 HIỂM CẢNH!", kanjiStyle);
                        }
                    }
                }
            }

            // Hiển thị nhãn Vật Phẩm RPG khi đứng gần
            WorldItemPickup[] pickups = FindObjectsOfType<WorldItemPickup>();
            foreach (var item in pickups)
            {
                if (item.isConsumed) continue;
                float dist = Vector3.Distance(_playerCombat.transform.position, item.transform.position);
                if (dist <= 6.0f)
                {
                    Vector3 sPos = cam.WorldToScreenPoint(item.transform.position + Vector3.up * 1.8f);
                    if (sPos.z > 0.5f)
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

            // Hiển thị các thông báo sát thương / Parry nổi
            if (GameManager.Instance != null)
            {
                foreach (var popup in GameManager.Instance.activePopups)
                {
                    Vector3 sp = cam.WorldToScreenPoint(popup.worldPosition);
                    if (sp.z > 0.5f)
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
            float w = 295f;
            float x = Screen.width - w - 18f;
            float y = 20f;

            if (!_showControlsHelp)
            {
                DrawRect(new Rect(Screen.width - 215f, y, 198f, 26f), new Color(0.04f, 0.04f, 0.07f, 0.65f));
                GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.88f, 0.55f) }
                };
                GUI.Label(new Rect(Screen.width - 207f, y + 3f, 185f, 20f), "[Phím H] Mở Bảng Điều Khiển", hintStyle);
                return;
            }

            DrawRect(new Rect(x - 8f, y - 6f, w + 14f, 212f), new Color(0.04f, 0.04f, 0.07f, 0.82f));

            GUIStyle header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUIStyle body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f) }
            };

            GUI.Label(new Rect(x, y, w, 18f), "🎮 ĐIỀU KHIỂN GÓC NHÌN THỨ 3 ([H] Đóng)", header);
            GUI.Label(new Rect(x, y + 22f, w, 18f), "• Chuột: Xoay Góc Nhìn (Esc: Mở chuột)", body);
            GUI.Label(new Rect(x, y + 40f, w, 18f), "• WASD + Shift: Di chuyển & Chạy bứt tốc", body);
            GUI.Label(new Rect(x, y + 58f, w, 18f), "• Chuột Trái: Chém Combo / Kết Liễu Boss", body);
            GUI.Label(new Rect(x, y + 76f, w, 18f), "• Chuột Phải: Đỡ Đòn & PARRY (Sekiro)", body);
            GUI.Label(new Rect(x, y + 94f, w, 18f), "• Space (x2): Nhảy né đòn quét & Đạp Đầu", body);
            GUI.Label(new Rect(x, y + 112f, w, 18f), "• V / Alt / Ctrl: Lộn Nhào Né Đòn (i-frame)", body);
            GUI.Label(new Rect(x, y + 130f, w, 18f), "• Phím 1/2/3: Đổi Phép | Q/E: Bắn Phép", body);
            GUI.Label(new Rect(x, y + 148f, w, 18f), "• Phím R / F: Bình Máu Đỏ / Bình Phép Xanh", body);
            GUI.Label(new Rect(x, y + 166f, w, 18f), "• Tab / Chuột Giữa: Khóa Mục Tiêu Boss", body);
            GUI.Label(new Rect(x, y + 184f, w, 18f), "• Phím G: Nghỉ tại Điểm Ân Điển | F5: Reset", body);
        }

        private void DrawVictoryOrDefeatBanner()
        {
            if (GameManager.Instance == null || GameManager.Instance.bannerAlpha <= 0.01f) return;

            float alpha = GameManager.Instance.bannerAlpha;
            bool isEndGame = GameManager.Instance.currentState == GameState.Victory ||
                             GameManager.Instance.currentState == GameState.Defeat;

            // Khi chỉ là thông báo tên khu vực / chuyển giai đoạn Boss, đặt gọn ở phía dưới để KHÔNG bao giờ che tầm nhìn đánh Boss!
            float bannerY = isEndGame ? (Screen.height * 0.42f) : (Screen.height - 135f);
            float bannerH = isEndGame ? 104f : 56f;

            DrawRect(new Rect(Screen.width * 0.15f, bannerY - bannerH * 0.5f, Screen.width * 0.70f, bannerH),
                new Color(0.02f, 0.02f, 0.04f, (isEndGame ? 0.88f : 0.65f) * alpha));

            Color titleColor = GameManager.Instance.bannerColor;
            titleColor.a = alpha;

            GUIStyle mainStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = isEndGame ? 30 : 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = titleColor }
            };

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = isEndGame ? 14 : 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f, alpha) }
            };

            GUI.Label(new Rect(0f, bannerY - (isEndGame ? 32f : 22f), Screen.width, 36f), GameManager.Instance.bannerTitle, mainStyle);
            GUI.Label(new Rect(0f, bannerY + (isEndGame ? 8f : 2f), Screen.width, 24f), GameManager.Instance.bannerSubtitle, subStyle);
        }

        private void DrawBar(Rect rect, float ratio, Color fillColor, Color bgColor, string label)
        {
            ratio = Mathf.Clamp01(ratio);
            DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color(0.85f, 0.78f, 0.55f, 0.45f));
            DrawRect(rect, bgColor);
            DrawRect(new Rect(rect.x, rect.y, rect.width * ratio, rect.height), fillColor);

            GUIStyle txt = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoxelSouls.Core
{
    public enum GameState
    {
        Exploring,
        BossFight,
        Victory,
        Defeat
    }

    public class FloatingPopup
    {
        public Vector3 worldPosition;
        public string text;
        public Color color;
        public float timer;
        public float duration;
        public int fontSize;
    }

    /// <summary>
    /// Quản lý trạng thái trận đấu, hiệu ứng Hitstop / Slow-Motion chuẩn Sekiro khi Parry hoặc Kết Liễu,
    /// hiệu ứng tia lửa va chạm kiếm và thông báo nổi trên màn hình.
    /// Đã sửa lỗi kẹt bảng giới thiệu (Banner tự động mờ dần sau 2.0 giây trong cả lúc Khám Phá lẫn lúc Đánh Boss).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState currentState = GameState.Exploring;
        public List<FloatingPopup> activePopups = new List<FloatingPopup>();

        [Header("Banner State")]
        public string bannerTitle = "";
        public string bannerSubtitle = "";
        public Color bannerColor = Color.white;
        public float bannerAlpha = 0f;
        public float bannerHoldTimer = 0f;

        private Coroutine _timeScaleCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            // Cập nhật các dòng chữ sát thương / thông báo nổi
            for (int i = activePopups.Count - 1; i >= 0; i--)
            {
                FloatingPopup p = activePopups[i];
                p.timer += Time.unscaledDeltaTime;
                p.worldPosition += Vector3.up * (1.35f * Time.unscaledDeltaTime);
                if (p.timer >= p.duration)
                {
                    activePopups.RemoveAt(i);
                }
            }

            // Tự động tắt bảng giới thiệu sau khi hiển thị xong (chỉ giữ cố định khi Thắng hoặc Thua)
            if (bannerAlpha > 0f && (currentState == GameState.Exploring || currentState == GameState.BossFight))
            {
                if (bannerHoldTimer > 0f)
                {
                    bannerHoldTimer -= Time.unscaledDeltaTime;
                }
                else
                {
                    bannerAlpha = Mathf.Max(0f, bannerAlpha - Time.unscaledDeltaTime * 1.35f);
                }
            }

            // Phím khởi động lại nhanh khi Thắng hoặc Thua (Hoặc nhấn F5 bất kỳ lúc nào)
            if (Input.GetKeyDown(KeyCode.F5) ||
                ((currentState == GameState.Victory || currentState == GameState.Defeat) &&
                 (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))))
            {
                RestartGame();
            }
        }

        public void TriggerBossFight()
        {
            if (currentState == GameState.Exploring)
            {
                currentState = GameState.BossFight;
            }
        }

        public void OnBossDefeated()
        {
            if (currentState == GameState.Victory) return;
            currentState = GameState.Victory;
            bannerTitle = "SHINOBI EXECUTION / GREAT ENEMY FELLED";
            bannerSubtitle = "ĐÃ HẠ GỤC HỘ VỆ HẮC THẠCH — Nhấn [ENTER] hoặc [F5] để chơi lại";
            bannerColor = new Color(1f, 0.82f, 0.25f);
            bannerAlpha = 1f;
            bannerHoldTimer = 999f;
            TriggerHitStop(0.25f, 0.18f);
        }

        public void OnPlayerDied()
        {
            if (currentState == GameState.Defeat) return;
            currentState = GameState.Defeat;
            bannerTitle = "DEATH / YOU DIED";
            bannerSubtitle = "DŨNG SĨ ĐÃ GỤC NGÃ — Nhấn [ENTER] hoặc [F5] để hồi sinh tại Điểm Ân Điển";
            bannerColor = new Color(0.88f, 0.15f, 0.15f);
            bannerAlpha = 1f;
            bannerHoldTimer = 999f;
            TriggerHitStop(0.3f, 0.2f);
        }

        public void ShowAreaBanner(string title, string subtitle, Color color, float holdDuration = 1.8f)
        {
            bannerTitle = title;
            bannerSubtitle = subtitle;
            bannerColor = color;
            bannerAlpha = 1f;
            bannerHoldTimer = holdDuration;
        }

        public void SpawnFloatingText(Vector3 worldPos, string text, Color color, int fontSize = 16, float duration = 1.0f)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (activePopups.Count > 12)
            {
                activePopups.RemoveAt(0);
            }
            activePopups.Add(new FloatingPopup
            {
                worldPosition = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), 0.3f, Random.Range(-0.25f, 0.25f)),
                text = text,
                color = color,
                timer = 0f,
                duration = duration,
                fontSize = fontSize
            });
        }

        /// <summary>
        /// Tạo hiệu ứng ngưng đọng thời gian (Hitstop / Slow-mo) khi Parry hoàn hảo hoặc tung Đòn Kết Liễu.
        /// </summary>
        public void TriggerHitStop(float slowTimeScale = 0.15f, float duration = 0.08f)
        {
            if (_timeScaleCoroutine != null)
                StopCoroutine(_timeScaleCoroutine);
            _timeScaleCoroutine = StartCoroutine(HitStopRoutine(slowTimeScale, duration));
        }

        private IEnumerator HitStopRoutine(float slowScale, float duration)
        {
            Time.timeScale = slowScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Tạo hiệu ứng nổ hạt Voxel (Tia lửa vàng khi Parry, máu khi trúng đòn, hoặc phép thuật).
        /// </summary>
        public void SpawnVoxelBurst(Vector3 position, Color color, int count = 14, float speed = 6f, float scale = 0.12f)
        {
            StartCoroutine(VoxelBurstRoutine(position, color, count, speed, scale));
        }

        private IEnumerator VoxelBurstRoutine(Vector3 pos, Color color, int count, float speed, float scale)
        {
            GameObject container = new GameObject("VoxelBurstVFX");
            container.transform.position = pos;

            Transform[] cubes = new Transform[count];
            Vector3[] velocities = new Vector3[count];

            Material mat = ShaderHelper.CreateMaterial(color, 0.5f, 0.2f, color * 1.6f);

            for (int i = 0; i < count; i++)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.SetParent(container.transform, false);
                c.transform.position = pos;
                c.transform.localScale = Vector3.one * scale * Random.Range(0.7f, 1.35f);
                Collider col = c.GetComponent<Collider>();
                if (col != null) Destroy(col);
                c.GetComponent<Renderer>().sharedMaterial = mat;

                cubes[i] = c.transform;
                velocities[i] = Random.onUnitSphere * speed * Random.Range(0.5f, 1.2f);
                velocities[i].y = Mathf.Abs(velocities[i].y) + 1.5f;
            }

            float elapsed = 0f;
            float lifetime = 0.45f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = 1f - (elapsed / lifetime);
                for (int i = 0; i < count; i++)
                {
                    if (cubes[i] != null)
                    {
                        velocities[i] += Vector3.down * 15f * Time.deltaTime;
                        cubes[i].position += velocities[i] * Time.deltaTime;
                        cubes[i].Rotate(360f * Time.deltaTime, 240f * Time.deltaTime, 0f);
                        cubes[i].localScale = Vector3.one * (scale * t);
                    }
                }
                yield return null;
            }

            if (container != null)
                Destroy(container);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }
}

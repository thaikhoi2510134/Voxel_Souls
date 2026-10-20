#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoxelSouls.Core;

namespace VoxelSouls.Editor
{
    /// <summary>
    /// Thanh công cụ tự động hóa trên thanh Menu của Unity Editor (Hỗ trợ cả macOS & Windows).
    /// Giúp người dùng tạo toàn bộ Scene, Nhân vật, Boss và Môi trường chỉ với 1 cú click chuột!
    /// </summary>
    public static class VoxelSoulsSetupEditor
    {
        [MenuItem("VibeGames/1. Dựng Toàn Bộ Màn Chơi & Nhân Vật (1-Click Setup)", false, 1)]
        public static void BuildAndSavePrototypeScene()
        {
            WorldSceneBuilder.BuildCompletePlayableScene();

            string scenesDir = Path.Combine(Application.dataPath, "Scenes");
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            string scenePath = "Assets/Scenes/VoxelSouls_Prototype.unity";
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Voxel Souls RPG — Hoàn Tất!",
                "Đã tạo xong toàn bộ Thế Giới Nhỏ, Vật Phẩm RPG, Dũng Sĩ Voxel, Boss Hộ Vệ Hắc Thạch và Giao Diện!\n\n" +
                "Bây giờ bạn chỉ cần nhấn nút PLAY (▶) ở giữa phía trên màn hình để bắt đầu chơi!",
                "Tuyệt vời!");
        }

        [MenuItem("VibeGames/2. Xuất Mô Hình .FBX Tự Động Từ Blender (Mac & Windows)", false, 2)]
        public static void RunBlenderExportScript()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string scriptPath = Path.Combine(projectRoot, "BlenderScripts", "generate_voxel_models.py");

            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy script Blender tại:\n{scriptPath}", "Đóng");
                return;
            }

            string blenderExe = FindBlenderExecutable();
            if (string.IsNullOrEmpty(blenderExe))
            {
                EditorUtility.DisplayDialog(
                    "Chưa tìm thấy Blender",
                    "Không tìm thấy Blender ở đường dẫn mặc định.\n\n" +
                    "• Trên Mac: Hãy cài Blender vào /Applications/Blender.app\n" +
                    "• Trên Windows: Hãy cài Blender vào C:\\Program Files\\Blender Foundation\\\n\n" +
                    "(Lưu ý: Bạn vẫn có thể nhấn Play chơi game bình thường ngay lúc này vì game đã có sẵn bộ dựng Voxel 3D tích hợp trong Unity!)",
                    "Đã hiểu");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = blenderExe,
                Arguments = $"-b -P \"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process proc = Process.Start(startInfo))
            {
                string output = proc.StandardOutput.ReadToEnd();
                string err = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                UnityEngine.Debug.Log($"[Blender Export Output]\n{output}");
                if (!string.IsNullOrEmpty(err))
                    UnityEngine.Debug.LogWarning($"[Blender Export Info]\n{err}");
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Xuất Blender Hoàn Tất!",
                "Đã tự động dựng và xuất các file mô hình:\n" +
                "• Assets/Models/Hero_VoxelKnight.fbx (.blend)\n" +
                "• Assets/Models/Boss_ObsidianWarden.fbx (.blend)",
                "OK");
        }

        private static string FindBlenderExecutable()
        {
            // Kiểm tra trên macOS
            string macPath = "/Applications/Blender.app/Contents/MacOS/Blender";
            if (File.Exists(macPath)) return macPath;

            // Kiểm tra trên Windows (Các phiên bản phổ biến từ 3.6 đến 4.3)
            string progFiles = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            string blenderFoundation = Path.Combine(progFiles, "Blender Foundation");
            if (Directory.Exists(blenderFoundation))
            {
                string[] dirs = Directory.GetDirectories(blenderFoundation, "Blender*");
                for (int i = dirs.Length - 1; i >= 0; i--)
                {
                    string exe = Path.Combine(dirs[i], "blender.exe");
                    if (File.Exists(exe)) return exe;
                }
            }

            return null;
        }
    }
}
#endif

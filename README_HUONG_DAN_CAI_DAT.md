# HƯỚNG DẪN CÀI ĐẶT & CHẠY DỰ ÁN TRÊN CẢ MAC VÀ WINDOWS
**Dự án:** `Voxel Souls RPG` (Top-Down Action RPG — Phong cách Sekiro + Elden Ring đồ họa Blocky/Voxel)  
**Thư mục dự án:** `/Users/thaikhoi/Documents/Vibe_Games/VoxelSouls`

Dự án này đã được cấu hình chuẩn đa nền tảng (**Cross-Platform**) để bạn mở và làm việc song song trên cả **macOS** và **Windows** mà không bị xung đột file hay lỗi hiển thị màu hồng (Pink Shader).

## 1. Cài đặt phần mềm cần thiết (Trên Mac & Windows)

1. **Cài đặt Unity Hub & Unity Editor:**
   * Tải **Unity Hub** tại trang chủ [Unity Download](https://unity.com/download) (chọn bản **Download for Mac** trên máy Mac và **Download for Windows** trên máy Windows).
   * Mở **Unity Hub** -> vào mục **Installs** -> nhấn **Install Editor** -> chọn phiên bản **Unity 2022.3 LTS** (hoặc **Unity 6 LTS**).
   * Ở bước chọn Modules, bạn chỉ cần giữ mặc định (hoặc tích thêm *Mac Build Support* / *Windows Build Support* nếu muốn xuất file `.app` / `.exe`).
2. **Cài đặt Blender (Để chỉnh sửa / xuất mô hình 3D):**
   * Tải **Blender** (miễn phí) tại [Blender Download](https://www.blender.org/download/).
   * Trên Mac: Kéo `Blender.app` vào thư mục `/Applications`.
   * Trên Windows: Cài đặt vào đường dẫn mặc định `C:\Program Files\Blender Foundation\`.

## 2. Cách mở và chạy Game ngay với 1 cú Click

1. Mở **Unity Hub** -> ở tab **Projects**, nhấn nút **Add** -> chọn **Add project from disk**.
2. Chọn thư mục dự án: `/Users/thaikhoi/Documents/Vibe_Games/VoxelSouls`.
3. Khi Unity Editor mở lên (nếu Unity hỏi nâng cấp phiên bản Editor bạn đang có, hãy chọn **Confirm / Continue**):
   * **Cách 1 (Khuyên dùng):** Trên thanh Menu trên cùng của Unity, chọn **`VibeGames -> 1. Dựng Toàn Bộ Màn Chơi & Nhân Vật (1-Click Setup)`**. Unity sẽ tự động dựng toàn bộ Khu Điện Thờ, 3 Vật Phẩm RPG, Đấu Trường Hắc Thạch, Dũng Sĩ, Boss, Camera và lưu thành Scene `Assets/Scenes/VoxelSouls_Prototype.unity`. Sau đó nhấn nút **Play (▶)**!
   * **Cách 2 (Tự động hoàn toàn):** Bạn thậm chí chỉ cần nhấn thẳng nút **Play (▶)** trong bất kỳ Scene trống nào — hệ thống `RuntimeAutoBootstrapper` sẽ tự động sinh toàn bộ thế giới và nhân vật ngay lập tức!

## 3. Cách tạo / chỉnh sửa nhân vật bằng Blender

Dự án đã tích hợp sẵn bộ dựng nhân vật Voxel bằng C# để bạn chơi được ngay mà không cần chờ cài Blender. Khi bạn cài xong Blender và muốn xuất file `.fbx` / `.blend` riêng để tùy biến thêm:

* **Chạy trực tiếp từ Menu của Unity:** Chọn **`VibeGames -> 2. Xuất Mô Hình .FBX Tự Động Từ Blender (Mac & Windows)`**.
* **Hoặc chạy từ trong phần mềm Blender:** Mở Blender -> chuyển sang tab **Scripting** trên cùng -> nhấn **Open** -> chọn file `BlenderScripts/generate_voxel_models.py` -> nhấn **Run Script (▶)**.
* Kịch bản sẽ tự động dựng và lưu các file `Hero_VoxelKnight.fbx` (`.blend`) và `Boss_ObsidianWarden.fbx` (`.blend`) vào thư mục `Assets/Models/`.

## 4. Cách đồng bộ dự án dùng song song giữa máy Mac và Windows

Thư mục `VoxelSouls` đã được tôi khởi tạo sẵn **Git Repository** kèm bộ lọc `.gitignore` và `.gitattributes` (tự động chuẩn hóa xuống dòng LF/CRLF giữa Mac và Windows, đồng thời bỏ qua thư mục tạm `Library/`, `Temp/`):

* **Nếu dùng GitHub / GitLab:** Bạn chỉ cần đẩy (push) thư mục `VoxelSouls` lên GitHub. Khi sang máy Windows, dùng lệnh `git clone` về và mở bằng Unity Hub.
* **Nếu copy qua USB / Google Drive / OneDrive:** Chỉ cần copy các thư mục **`Assets`**, **`Packages`**, **`ProjectSettings`**, và **`BlenderScripts`** (không cần copy thư mục `Library` hay `Temp` vì mỗi hệ điều hành sẽ tự tạo lại bộ nhớ đệm phù hợp khi mở Unity).

# TÀI LIỆU THIẾT KẾ GAME (GAME DESIGN DOCUMENT - GDD)
**Tên dự án:** Voxel Souls RPG (Bản chơi thử đầu tiên — Prototype v1.0)  
**Thể loại:** Top-Down Action RPG (Hành động nhập vai góc nhìn từ trên xuống)  
**Cảm hứng chủ đạo:** *Sekiro: Shadows Die Twice* (Cơ chế Đỡ đòn Parry, Thanh Thế Posture, Đòn Hiểm Cảnh `危`, Đạp Đầu & Đòn Kết Liễu) kết hợp *Elden Ring* (Phép thuật FP, Bình Máu Đỏ / Năng Lượng Xanh, Điểm Ân Điển, Vật phẩm Cổ Ngữ và Boss đa giai đoạn).

## 1. Bối cảnh & Thế giới nhỏ (Small RPG World)

Người chơi vào vai **Dũng Sĩ Cổ Ngữ (Voxel Knight)** bước vào khu di tích cổ đại gồm 2 khu vực liên thông:
1. **Khu Điện Thờ Khởi Đầu (Sanctuary Courtyard - Phía Nam):**
   * **Điểm Ân Điển (Site of Grace):** Đài lửa vàng ở trung tâm giúp hồi phục toàn bộ Máu, FP và làm đầy lại số lượng Bình Thuốc khi nhấn phím **`G`**.
   * **Hình Nhân Tập Luyện (Training Dummy):** Đặt bên phải sân để người chơi làm quen với nhịp chém 3 đòn và thử nghiệm 3 loại phép thuật.
   * **3 Bệ Đá Vật Phẩm RPG:**
     * *Hạt Giống Hoàng Kim (Golden Seed):* Tăng vĩnh viễn +1 Bình Máu Đỏ và +1 Bình Năng Lượng Xanh.
     * *Đá Cổ Ngữ Rèn Kiếm (Ancient Smithing Rune):* Tăng +30% sát thương kiếm, phép thuật và sát thương phá thế.
     * *Bùa Hộ Mệnh Hồng Ngọc (Crimson Amber Medallion):* Tăng +40 Máu tối đa (Max HP) và hồi đầy máu.
2. **Đấu Trường Hắc Thạch (Obsidian Boss Colosseum - Phía Bắc):**
   * Bước qua cây cầu đá cổ đại sẽ tiến vào đấu trường rộng lớn với 6 cột đá cổ ngữ (có thể dùng để che chắn đạn phép của Boss) và đánh thức Boss đầu tiên: **Hộ Vệ Hắc Thạch (Obsidian Warden)**.

## 2. Hệ thống Điều khiển (Bàn phím + Chuột trên PC)

| Thao tác | Phím điều khiển | Mô tả chi tiết cơ chế (Sekiro + Elden Ring) |
| :--- | :--- | :--- |
| **Di chuyển & Chạy nhanh** | `W, A, S, D` + `Left Shift` | Di chuyển 8 hướng theo góc nhìn Camera; giữ `Shift` tiêu hao nhẹ Stamina để chạy bứt tốc. |
| **Chém Kiếm / Kết Liễu** | `Chuột Trái (LMB)` | Tung chuỗi Combo 3 đòn kiếm. Nếu Boss đang bị **Vỡ Thế (Posture Broken)**, nhấn Chuột Trái gần Boss sẽ tung **Đòn Kết Liễu (Deathblow)** gây sát thương chí mạng cực lớn. |
| **Đỡ Đòn & Parry (Sekiro)** | `Chuột Phải (RMB)` | Nhấn giữ để Đỡ đòn (giảm 80% sát thương). Nhấn **ngay trước khi đòn Boss chạm người (trong 0.25 giây)** để kích hoạt **Perfect Parry**: Không mất máu, không bao giờ vỡ thế, bắn tia lửa vàng và phản sát thương Thế (Posture) cực mạnh vào Boss. |
| **Nhảy & Đạp Đầu Phá Thế** | `Space` (Nhấn lần 2 trên không) | Nhảy qua đòn **Quét Hiểm Cảnh (`危`)** sát đất của Boss. Khi đang ở trên không gần Boss, nhấn `Space` lần nữa để **Đạp Đầu Boss** gây +48 sát thương Thế chuẩn Sekiro. |
| **Lộn Nhào Né Đòn (Roll)** | `V` hoặc `Left Alt` / `Left Ctrl` | Lộn nhào 360 độ theo hướng di chuyển kèm khung hình bất tử (**i-frames**) để né các đòn đập búa diện rộng. |
| **Đổi & Chưởng Phép Thuật** | `1`, `2`, `3` và `Q` hoặc `E` | Phím `1`: *Cung Sao Băng* (14 FP, tự dẫn đường); Phím `2`: *Hỏa Cầu Hủy Diệt* (24 FP, nổ diện rộng); Phím `3`: *Lôi Thương Cổ Đại* (20 FP, tốc độ cao). Nhấn `Q` hoặc `E` để bắn phép. |
| **Bình Máu & Bình Phép** | `R` (Máu Đỏ) / `F` (Phép Xanh) | Phím `R` hồi +65 HP; Phím `F` hồi +55 FP. |
| **Khóa Mục Tiêu / Tiện ích** | `Tab` (Lock-On) / `H` / `F5` | `Tab` hoặc Chuột Giữa để khóa góc quay vào Boss; `H` ẩn/hiện bảng phím tắt; `F5` khởi động lại màn chơi. |

## 3. Thiết kế Boss Đầu Tiên: Hộ Vệ Hắc Thạch (Obsidian Warden)

* **Chỉ số:** 450 HP, 120 Posture, chia làm **2 Giai Đoạn (Phase 1 & Phase 2 khi máu dưới 55%)**.
* **Hệ thống Chiêu thức:**
  1. **Combo Đại Đao 3 Nhịp:** Chém ngang -> Chém ngược -> Bổ dọc. Người chơi có thể căn nhịp bấm Chuột Phải 3 lần liên tiếp để làm đầy nhanh thanh Posture của Boss.
  2. **Đâm Xung Kích (Shadow Thrust):** Thu hẹp khoảng cách từ xa, có thể Parry để làm Boss khựng lại.
  3. **`危` Đòn Quét Hiểm Cảnh (Perilous Sweep):** Hiện chữ **`危` màu đỏ rực** trên đầu Boss. Không thể đỡ bằng khiên/kiếm — người chơi phải nhấn **`Space`** nhảy lên và nhấn **`Space`** lần nữa để đạp đầu phản công.
  4. **`危` Đập Búa Dung Nham (Perilous Magma Slam):** Đập đại đao gây nổ dung nham diện rộng phía trước. Người chơi phải nhấn **`V / Alt / Ctrl`** lộn nhào thoát khỏi vùng nổ.
  5. **Phép Hắc Tinh (Mở khóa ở Giai Đoạn 2):** Bắn liên hoàn 3 cầu năng lượng hắc ám. Người chơi có thể lộn né, nấp sau cột đá, hoặc **Parry ngay khoảnh khắc cầu phép chạm kiếm để đánh bật ngược đạn phép về phía Boss**!

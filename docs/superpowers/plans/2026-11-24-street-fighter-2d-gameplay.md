# Street Fighter 2.5D Fighting Combat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Triển khai cơ chế gameplay chiến đấu 2.5D chuẩn đối kháng phong cách Street Fighter giữa JangHyu và MonsterBall (Red), khóa trục di chuyển 1D/2.5D, camera tự động bám góc ngang, combo kỹ năng, và tích hợp hệ thống test bot tự động quay chụp frame kiểm chứng khớp 100% video mẫu.

**Architecture:** Sử dụng kiến trúc 2.5D Fighting Game trên trục X cố định ($Z = 0$), Player và Monster luôn auto-face đối phương. Camera bên hông (Side View) tự động zoom theo khoảng cách 2 đối thủ. Bộ điều khiển tách biệt giữa di chuyển (Walk Forward/Back, Dash) và tấn công combo (NormalAttack 1-2-3). Test Bot tự động mô phỏng các pha chiến đấu và ghi lại frame.

**Tech Stack:** Unity 6 Universal Render Pipeline (URP), C#, Animator Controller, TextMeshPro, Cinemachine/Transform Lerp Camera, Particle System.

**Spec:** `spec_monsterball.md`, `Test Guide.txt`, 3 video mẫu `Guide video 1.mp4`, `Guide video 2.mp4`, `Guide video 3.mp4`.

## Global Constraints
- Hệ tọa độ chiến đấu: Trục X là trục di chuyển chính, trục Z cố định ($Z = 0$), trục Y là độ cao.
- Góc nhìn: Camera đặt ở sườn bên hông (Side-view 2.5D perspective), nhìn vuông góc vào mặt phẳng chiến đấu $Z = 0$.
- Model quái vật: Chỉ sử dụng duy nhất mô hình màu đỏ `MonsterBall_red.prefab`.
- Toàn bộ phản hồi, giải thích và phân tích 100% bằng tiếng Việt.

---

### Task 1: Bộ Điều Khiển Chiến Đấu 2.5D Cho Người Chơi (`JangHyuFightingController.cs`)
**Files:**
- Create: `Assets/Game/1. Scripts/Combat/Player/JangHyuFightingController.cs`
- Modify: `Assets/Game/3. Resources/Characters/Player/JangHyu/Anmation/JangHyu_CombatController.controller`
- Test: Play Mode test di chuyển tiến/lùi, dash, combo đòn phím J

- [ ] **Step 1: Viết script `JangHyuFightingController.cs`**
  - Khóa trục $Z = 0$.
  - Tính toán hướng Tiến (Forward) và Lùi (Back) dựa theo vị trí đối thủ MonsterBall.
  - Phím A/D: Đi tiến/lùi với tốc độ 4.5 m/s.
  - Phím Shift / Nhấn đúp: Dash lướt nhanh 10.0 m/s với thời gian 0.22s.
  - Phím J / Click chuột trái: Combo 3 đòn đánh thường (`NormalAttack_01` -> `02` -> `03`).
  - Auto-Face: Quay mặt sang phải ($Y=90^\circ$) nếu đứng bên trái quái, sang trái ($Y=270^\circ$) nếu vượt qua quái.

- [ ] **Step 2: Cập nhật Animator Controller của JangHyu**
  - Thêm state `WalkForward`, `WalkBackward`, `DashForward`, `DashBackward`, `Attack1`, `Attack2`, `Attack3`.
  - Cấu hình transition mượt mà không delay.

---

### Task 2: Camera 2.5D Fighting Game Chuẩn Street Fighter (`StreetFighterCamera.cs`)
**Files:**
- Create: `Assets/Game/1. Scripts/Camera/StreetFighterCamera.cs`
- Modify: `Assets/Scenes/SampleScene.unity` (Main Camera)

- [ ] **Step 1: Viết script `StreetFighterCamera.cs`**
  - Bám theo trung điểm $X_{mid} = (X_{player} + X_{monster}) / 2$.
  - Tự động điều chỉnh khoảng cách $Z$ và chiều cao $Y$ theo khoảng cách giữa 2 nhân vật:
    $$Z_{cam} = - (4.5f + \text{distance} \times 0.65f)$$
  - Khóa góc nhìn ngang thẳng vào trục $Z = 0$.

---

### Task 3: Tối Ưu Quái Vật MonsterBall Lăn Tấn Công 2.5D (`MonsterBallAmbushController.cs`)
**Files:**
- Modify: `Assets/Game/1. Scripts/Combat/MonsterBall/MonsterBallAmbushController.cs`

- [ ] **Step 1: Cập nhật cơ chế lăn tấn công trên trục 2.5D**
  - Quái vật spawn tại $X = 5.0, Z = 0$.
  - Khi trồi lên: Nhảy bổng lên cao $Y = 2.8$, tiếp đất bốc cháy ngọn lửa màu cam vàng.
  - Lăn tấn công theo trục X thẳng về phía người chơi.
  - Hất văng người chơi ngã ngửa khi va chạm, gây thiêu đốt DoT.
  - Phanh chậm dần sau khi hết cự ly, quay đầu chuẩn bị đợt tấn công tiếp theo.

---

### Task 4: Hệ Thống Test Bot Tự Động & Chụp Frame Gameplay (`GameplayTestBotAndRecorder.cs`)
**Files:**
- Create: `Assets/Game/1. Scripts/Test/GameplayTestBotAndRecorder.cs`

- [ ] **Step 1: Viết kịch bản tự động diễn tập (Automated Sequence)**
  - Tự động cho JangHyu đứng thủ $\to$ quái trồi lên $\to$ quái lăn tấn công $\to$ JangHyu dash né $\to$ JangHyu combo đánh trúng quái $\to$ quái tụt máu $\to$ quái phản công trúng JangHyu $\to$ JangHyu tụt máu HUD.
  - Tự động chụp chuỗi ảnh screenshot sắc nét từng bước gửi vào artifact directory để kiểm chứng so sánh với 3 video gốc.


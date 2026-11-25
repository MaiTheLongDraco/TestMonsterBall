# MonsterBall Ambush & Flame Roll Attack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hiện thực hóa cơ chế xuất hiện phục kích (ambush eruption) và húc lăn lửa (flame roll attack) của quái vật MonsterBall Red đối với nhân vật người chơi JangHyu trong Unity scene, bám sát 100% video hướng dẫn và tài liệu đặc tả [spec_monsterball.md](file:///C:/Users/Admin/.gemini/antigravity/brain/65c98726-979a-4b32-944b-88da764e48fb/spec_monsterball.md).

**Architecture:** 
- Xây dựng state machine gameplay độc lập `MonsterBallAmbushController` quản lý 5 trạng thái: `Underground_Hidden` -> `Emerge_Launch` -> `Land_Ignite` -> `Roll_Attack` -> `Impact_Recovery`.
- Đồng bộ hóa chuyển động vật lý (Rigidbody/SphereCollider) với Animator (`CharacterMonsterRedBall.overrideController`) và hệ thống hiệu ứng hạt VFX (Ground Eruption, Flame Aura, Fiery Roll Trail, Burning DoT).
- Tích hợp phản ứng nhận đòn của JangHyu (Knockdown, gắn hiệu ứng thiêu đốt lửa, trừ máu liên tục và hiển thị sát thương nhảy số).

**Tech Stack:** Unity 6000.0.49f1, C# 12 (Roslyn), URP, Shuriken Particle System, Animator Controller, UnityMCP.

**Spec:** [spec_monsterball.md](file:///C:/Users/Admin/.gemini/antigravity/brain/65c98726-979a-4b32-944b-88da764e48fb/spec_monsterball.md)

---

## Global Constraints

- Chỉ sử dụng duy nhất mô hình quái vật màu đỏ `MonsterBall_red` (không dùng bản Blue hoặc Green) theo đúng yêu cầu `Test Guide.txt`.
- Vật liệu nhân vật JangHyu phải được gán chính xác `JangHyu.mat` kèm texture `JangHyu_BaseColor.png` và `JangHyu_Normal.png`.
- Tốc độ lăn tấn công đạt ~10.0 m/s với tầm húc tối đa 12m; lực hất văng người chơi theo vector `knockbackDir * 8.0f + Vector3.up * 4.0f`.
- Trạng thái bốc cháy (Burn DoT) trên người chơi duy trì 3.0s, giật sát thương 1 HP mỗi 0.3s kèm hiệu ứng lửa bám trên cơ thể.
- Đảm bảo 0 lỗi biên dịch C# trên toàn bộ dự án và xác minh hành vi runtime trực tiếp trong Unity Editor thông qua `unityMCP`.

---

## Review Focus

1. **Player không ở cùng cao độ (chênh lệch trục Y):** MonsterBall cần khóa trục Y hoặc bám bề mặt sàn để không bị phóng vọt lên trời hoặc cắm xuống đất khi lăn.
2. **Player né tránh thành công (Roll miss):** MonsterBall phải có cơ chế Timeout hoặc Max Roll Distance (12m) để tự phanh, dập vệt khói và quay lại trạng thái Idle/Patrol thay vì lăn vô tận.
3. **Player bị húc liên tục (Multi-hit lock):** Áp dụng cờ bất tử ngắn (i-frame 0.5s) hoặc chỉ trigger sát thương 1 lần duy nhất trong mỗi cú lướt.
4. **VFX không được dọn dẹp khi đổi trạng thái:** Đảm bảo toàn bộ particle system con (Trail, Aura) được Stop/Deactivate đúng thời điểm khi va chạm hoặc hết thời gian.
5. **Không tìm thấy mục tiêu JangHyu:** Fallback an toàn tự tìm qua Tag `Player` hoặc Component `Character` mà không gây `NullReferenceException`.

---

## Task Structure

### Task 1: Tạo Script `MonsterBallAmbushController.cs` và State Machine

**Files:**
- Create: `Assets/Game/1. Scripts/Combat/MonsterBall/MonsterBallAmbushController.cs`
- Create: `Assets/Game/1. Scripts/Combat/Player/JangHyuBurnReceiver.cs`

**Interfaces:**
- Consumes: `UnityEngine.Rigidbody`, `UnityEngine.Animator`, `UnityEngine.SphereCollider`.
- Produces: `MonsterBallAmbushController` component quản lý toàn bộ vòng đời kích hoạt, bay lên, tiếp đất, húc lăn và va chạm; `JangHyuBurnReceiver` tiếp nhận hiệu ứng cháy và sát thương giật.

- [ ] **Step 1: Viết script `JangHyuBurnReceiver.cs`**
  - Quản lý trạng thái bị thiêu đốt: nhận damage định kỳ, instantiate/toggle particle lửa gắn trên người chơi, gọi animation Groggy/Knockdown.
- [ ] **Step 2: Viết script `MonsterBallAmbushController.cs`**
  - Định nghĩa enum `AmbushState { Hidden, Emerge, LandIgnite, RollAttack, ImpactRecover, Idle }`.
  - Triển khai logic kích hoạt theo khoảng cách `Vector3.Distance(transform.position, player.position) <= triggerDistance (3.5f)`.
  - Triển khai impulse nhảy thẳng đứng + hướng về phía trước khi Emerge (`Vector3.up * jumpForce + forward * 1.5f`).
  - Triển khai cơ chế bốc cháy khi tiếp đất và lao tới húc người chơi kèm xoay tròn hình cầu quanh trục ngang.
  - Xử lý va chạm `OnCollisionEnter` / `OnTriggerEnter` tác động lực đẩy lùi và gọi `JangHyuBurnReceiver.ApplyBurn()`.
- [ ] **Step 3: Biên dịch và kiểm tra 0 lỗi C#**
  - Chạy `execute_code` hoặc kiểm tra log compilation để xác nhận 0 lỗi.

---

### Task 2: Cấu hình Animator và VFX Components

**Files:**
- Modify/Setup: `Assets/Game/3. Resources/Characters/Enemies/Stage2/MonsterBall/Animator/CharacterMonsterRedBall.overrideController`
- Create Prefabs/Assets:
  - `Assets/Game/3. Resources/Effects/Prefabs/VFX_MonsterBall_GroundErupt.prefab` (Đất nổ tung, khói xám, mảnh vỡ)
  - `Assets/Game/3. Resources/Effects/Prefabs/VFX_MonsterBall_RollTrail.prefab` (Vệt khói đen cuộn + tàn lửa)
  - `Assets/Game/3. Resources/Effects/Prefabs/VFX_Player_BurnAura.prefab` (Hiệu ứng thiêu đốt trên người JangHyu từ UNI VFX)

**Interfaces:**
- Consumes: Assets từ `Assets/UNI VFX` và `Assets/FEY` (textures, materials).
- Produces: Các prefabs VFX sẵn sàng gắn vào các socket trên MonsterBall và JangHyu.

- [ ] **Step 1: Tạo Prefab VFX Ground Eruption**
  - Tích hợp hạt bụi đất văng tung tóe, khói mờ bốc lên và chớp sáng khi MonsterBall phá đất trồi lên.
- [ ] **Step 2: Cấu hình Body Flame Aura và Roll Smoke Trail**
  - Gắn `Destructive Elements Flame Skinned Mesh` hoặc `Destructive Elements Flame Simple` làm hào quang lửa quanh thân MonsterBall.
  - Tạo vệt khói đen cuồn cuộn (Dark Smoke Trail) kéo dài phía sau đường lăn kèm tia lửa văng.
- [ ] **Step 3: Cấu hình VFX Player Burn Aura**
  - Thiết lập prefab lửa cháy bám theo khung xương của JangHyu.

---

### Task 3: Setup Scene `SampleScene` qua Unity MCP

**Files:**
- Modify: `Assets/Scenes/SampleScene.unity`

**Interfaces:**
- Consumes: `MonsterBall_red.prefab`, `JangHyu_AttackCombo2`, các VFX prefabs, `MonsterBallAmbushController`.
- Produces: Môi trường hoàn chỉnh trong `SampleScene` với đầy đủ vật liệu, vị trí xuất phát, collider, và tham chiếu component.

- [ ] **Step 1: Gán chuẩn vật liệu & texture cho JangHyu**
  - Chạy script qua `execute_code` để gán `JangHyu.mat` chuẩn (Base Color + Normal) cho toàn bộ SkinnedMeshRenderer của JangHyu.
- [ ] **Step 2: Cấu hình MonsterBall Red trong Scene**
  - Đặt vị trí xuất phát ban đầu ẩn dưới đất `Y = -1.2f` (vết nứt xuất hiện trên mặt đất).
  - Gắn Rigidbody, SphereCollider, Animator (`CharacterMonsterRedBall.overrideController`), và `MonsterBallAmbushController`.
  - Kết nối các tham chiếu VFX (Ground Erupt, Body Aura, Roll Trail) và mục tiêu Player (`JangHyu_AttackCombo2`).
- [ ] **Step 3: Cấu hình JangHyu trong Scene**
  - Gắn CapsuleCollider, Rigidbody (kinematic/dynamic), và `JangHyuBurnReceiver`.
- [ ] **Step 4: Thiết lập Camera và Ánh sáng góc nhìn beat-em-up**
  - Điều chỉnh `Main Camera` với góc nghiêng isometric/side-angle nhìn rõ toàn cảnh từ lúc quái trồi lên đến khi húc văng người chơi.

---

### Task 4: Chạy kiểm thử Runtime và Xác minh độc lập qua Unity MCP

**Files:**
- Test Script / Runtime Execution via `execute_code`

**Interfaces:**
- Consumes: Scene đang chạy trong Unity Editor (Play Mode).
- Produces: Bằng chứng runtime (logs, vị trí, state transitions, event triggers) chứng minh cơ chế hoạt động chính xác 100%.

- [ ] **Step 1: Khởi động Play Mode qua Unity MCP**
  - Bật `EditorApplication.isPlaying = true` và giám sát frame loop.
- [ ] **Step 2: Xác minh Trigger Distance & Emerge Phase**
  - Kiểm tra quái vật phát hiện người chơi khi vào bán kính 3.5m, kích hoạt `GroundEruptVFX`, phóng vọt lên cao ~2.5m.
- [ ] **Step 3: Xác minh Land & Ignite Phase**
  - Kiểm tra khi quái tiếp đất: hào quang lửa `BodyFlameAura` bật sáng rực rỡ, animation chuyển sang tư thế sẵn sàng lăn.
- [ ] **Step 4: Xác minh Roll Attack & Trail**
  - Kiểm tra quái vật lăn tròn tốc độ cao (~10m/s), vệt khói đen dày đặc cuộn theo sau.
- [ ] **Step 5: Xác minh Va chạm, Knockdown và Thiêu đốt**
  - Kiểm tra va chạm với JangHyu: người chơi bị hất tung, lửa bén lên cơ thể, nhận sát thương nhảy số, quái vật hồi phục tư thế.
- [ ] **Step 6: Chụp ảnh viewport xác thực trực quan**
  - Dùng camera render chụp lại frame tiếp đất, lăn lửa và va chạm để lưu làm bằng chứng độc lập.


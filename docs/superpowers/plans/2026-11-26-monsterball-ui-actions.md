# MonsterBall UI Controls & English Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Làm cho toàn bộ UI điều khiển MonsterBall (Spawn, Reset, AI, Dropdown, Demo Actions) hoạt động thực tế trong Play Mode và chuyển toàn bộ nhãn tiếng Hàn trên UI sang tiếng Anh.

**Architecture:** `MonsterBallAIController` cung cấp API `TriggerDemoAction(MonsterAIState)` và `SpawnActive()`. `CombatUIManager` chịu trách nhiệm dựng/bind đầy đủ sự kiện runtime cho `TopRight_Control_Panel`, `Dropdown_MonsterBallRed`, `Demo_Actions_Panel`, và ép nhãn tiếng Anh. Các script Editor (`SetupStreetFighterScene`, `BakeStreetFighterDirectly`) đồng bộ cấu trúc UI này và bake vào `SampleScene.unity`.

**Tech Stack:** Unity 6 / C# (`UnityEngine.UI`, `UnityEngine.EventSystems`, `UnityEditor`), URP.

**Spec:** `docs/superpowers/specs/2026-11-26-monsterball-ui-actions-design.md`

---

### Task 1: Hỗ trợ Demo Actions trong MonsterBallAIController

**Files:**
- Modify: `Assets/Game/1. Scripts/Combat/MonsterBall/MonsterBallAIController.cs`

- [ ] **Step 1:** Thêm `SpawnAndEmerge()`, `TriggerDemoAction(MonsterAIState state)` và chuẩn hóa xử lý `Emerge -> LandIgnite -> CombatIdle` (khi `_aiEnabled == false` thì sau `LandIgnite` chuyển `CombatIdle` đứng chờ thay vì tự `RollAttack`).

### Task 2: Cập nhật CombatUIManager sang tiếng Anh, bind nút runtime & thêm Demo Actions

**Files:**
- Modify: `Assets/Game/1. Scripts/UI/CombatUIManager.cs`

- [ ] **Step 1:** Chuyển `"장휴 (長休)"` -> `"JangHyu"`, `"적구"` -> `"MonsterBall Red"`, `"생성"` -> `"Spawn"`, `"리셋"` -> `"Reset"`.
- [ ] **Step 2:** Thay `Dropdown_MonsterBallRed` thành `UnityEngine.UI.Dropdown` thực thụ với Template hợp lệ (hoặc hỗ trợ chọn chế độ), bind lại toàn bộ `Btn_Spawn`, `Btn_Reset`, `AI_Checkbox_Container` trong `BindPlayerHUDElements()`.
- [ ] **Step 3:** Dựng & bind `Demo_Actions_Panel` gồm 5 nút: `Emerge`, `Roll`, `Jump Slam`, `Melee`, `Groggy` gọi sang `MonsterBallAIController.TriggerDemoAction(...)`.

### Task 3: Đồng bộ Editor Setup & Bake Script vào SampleScene.unity

**Files:**
- Modify: `Assets/Editor/SetupStreetFighterScene.cs`
- Modify: `Assets/Editor/BakeStreetFighterDirectly.cs`
- Modify: `Assets/Editor/CaptureStreetFighterEvidence.cs`

- [ ] **Step 1:** Đồng bộ `SetupStreetFighterScene.SetupFullUI()` và `BakeStreetFighterDirectly.BakeUIHierarchy()` sử dụng nhãn tiếng Anh và cấu trúc `TopRight_Control_Panel` + `Demo_Actions_Panel`.
- [ ] **Step 2:** Cập nhật `CaptureStreetFighterEvidence.cs` để chụp UI (`CaptureFrameCoroutine`) và kiểm chứng trực tiếp bấm `Btn_Spawn`, `Btn_Reset`, `AI_Checkbox_Container` và 5 nút Demo Action (`Emerge`, `Roll`, `Jump Slam`, `Melee`, `Groggy`), đảm bảo 0 text Hangul còn trên Scene UI.

### Task 4: Bake Scene & Chạy kiểm chứng thực tế qua Unity Editor

- [ ] **Step 1:** Thực thi `SetupStreetFighterScene.BakeAndClean()` để lưu `SampleScene.unity`.
- [ ] **Step 2:** Chạy `CaptureStreetFighterEvidence.RunVerification()` (hoặc `RunInEditorVerification`) để kiểm chứng runtime exit code / logs / screenshots thực tế.

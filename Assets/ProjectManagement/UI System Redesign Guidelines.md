# UI & Menu System Redesign Guidelines

> **Status:** Current UI scripts have been isolated into `Temp/` subdirectories (`Assets/Scripts/Core/UI/Temp/` and `Assets/Scripts/World/Temp/`). They serve solely as a functional throwaway harness to unblock integration testing for higher-level gameplay systems (such as the Dialogue System and Map Zone transitions).

---

## 1. Core Architectural Pillars for Future Rework

### Pillar 1: Animator as the Master Choreographer (State-Driven Sequencing)
* **Problem in Prototype:** Executing transitions and data loading directly from C# Button `onClick` handlers creates race conditions and timing glitches (e.g., menus lingering behind loading curtains, visual pop-in before data builds).
* **Target Architecture:**
  1. **Dumb Buttons:** UI Buttons should *only* send triggers or set integer parameters on the Animator (`animator.SetTrigger("OpenLoad")`).
  2. **Animation Events & `StateMachineBehaviour`:**
     - The Animator plays visual transitions first (sliding, fading out).
     - At the exact target keyframe (or upon `OnStateExit`), the Animator fires an Animation Event or calls a script method to execute the payload (e.g., `BuildLoadList()`, `StartGameSession()`).
  3. **Atomic Scene Handshakes:**
     - Player clicks Start $\rightarrow$ Curtain begins fading in $\rightarrow$ Keyframe at 100% black fires `SceneCoordinator.StartGameSessionAsync()` $\rightarrow$ Scene loads $\rightarrow$ Animation completes fade-out.

---

### Pillar 2: Asset-First UI Workflow
* **Problem in Prototype:** Writing UI logic before finalizing sprites, 9-slices, font metrics, and element placement causes duplicated effort and brittle layout math.
* **Target Architecture:**
  1. Complete pixel art assets, 9-slice frames, icons, and fonts first.
  2. Build and visually validate the static UGUI hierarchy in the Scene view.
  3. Attach and bind lean, data-only controller scripts once the layout is locked.

---

## 2. Scripts Flagged for Deprecation / Full Rewrite

The following scripts are located in temporary folders and are slated for replacement during the UI Polish milestone:

### `Assets/Scripts/Core/UI/Temp/`
- **`MenuAnimatorController.cs`**: Temporary integer-based state switcher. Will be replaced by dedicated UI state machines and Animator event dispatchers.
- **`LoadGamePanel.cs`**: Basic file list instantiator. Will be redesigned with proper pooling, audio feedback, and gamepad navigation.
- **`SaveGamePanel.cs`**: Prototype save overwrite and modal prompt. Will be redesigned with asset-accurate dialog popups and input sanitization.
- **`SaveSlotItem.cs`**: Prototype save bullet with basic color tinting. Will be replaced with customized prefab templates featuring animated hover/select states.
- **`MenuStateNotifier.cs` & `MenuViewState.cs`**: Interim event bus connecting UI states to camera viewpoints.
- **`LoadingScreenCurtain.cs`**: Basic CanvasGroup fader. Will be integrated directly into the master transition animator.

### `Assets/Scripts/World/Temp/`
- **`MenuCameraController.cs`**: Subscribed observer lerping camera positions in `CoolMenuScene`. Will be hooked into Cinemachine or Animator timeline tracks.

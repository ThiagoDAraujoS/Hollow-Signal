# Hollow Signal — Control Schemes & Input Design

This document details the active control layout, the technical input pipeline, and the planned systems connected to player controls.

---

## 1. Active Control Scheme (Implemented)

### Mouse Controls (ARPG / BG3 Hybrid Scheme)

| Input | Interaction Pattern | Event / Action | Description |
| :--- | :--- | :--- | :--- |
| **Left Click** | **Tap / Click Ground** | `PlayerBrain.OnDirectCommand` | Commands selected squad to navigate to destination in a dynamic tactical wedge formation. |
| **Left Click** | **Tap / Click IUsable** | `CharacterMovement.MoveToAndUse` | Orders squad `Lead` to walk to `UseSpot`, smoothly rotate to `UseRotation`, and execute `Use()`. |
| **Left Click** | **Hold & Drag** | `PlayerBrain.OnContinuousCommand` | **Diablo-style steering.** Continuously streams formation destination updates throttled at `0.08s`. |
| **Right Click** | **Short Tap** (< 0.35s, low drag) | Selection Logic | **Selects single character** or clears selection. Holding `Shift` toggles/adds to current selection. |
| **Right Click** | **Hold & Drag** (Distance $\ge$ 10px) | Marquee Box Selection | Draws a green selection box on screen. Enclosed active party members are selected upon button release. |
| **Right Click** | **Hold Stationary** ($\ge$ 0.35s) | `PlayerBrain.OnContextMenuRequested` | Triggers a context menu event at the cursor position (for inspect, talk, interact, etc.). |
| **Mouse Wheel** | **Scroll Up / Down** | Camera Zoom | Adjusts `CinemachineCamera` orthographic lens size between **3.0** (close-up) and **6.0** (wide tactical view). |

---

### Keyboard Hotkeys & Modifiers

| Key / Modifier | Input Action | Event / Query | Description |
| :--- | :--- | :--- | :--- |
| **`W`, `A`, `S`, `D`** | `CameraMovement` | `CameraAnchor.MoveAnchor` | Moves the `CameraAnchor` relative to camera angle, clamped to map `Bounds`. |
| **`Z`** | `Stop` | `PlayerBrain.StopSelectedUnits` | **Immediate squad halt.** Cancels navigation paths and pending interactions for all selected units. |
| **`1`, `2`, `3`, `4`** | `Slot1` – `Slot4` | `PlayerBrain.SelectSlot` | Immediately selects the corresponding party member in roster order. |
| **`~` (Backquote)** | `SelectAll` | `PartySelection.SelectAll` | Selects all currently active party members at once. |
| **`Tab`** | `CycleLeader` | `PartySelection.CycleLeader` | Cycles through selected units to designate the active squad `Lead`. |
| **`Escape`** | `Deselect` | `PartySelection.Clear` | Clears all unit selections (units continue executing their current tasks). |
| **`Shift` (Left/Right)** | `ModifierShift` | `PlayerBrain.IsShiftPressed` | Modifies selection to add/toggle units instead of replacing. |
| **`Alt` (Left/Right)** | `ModifierAlt` | `PlayerBrain.IsAltPressed`<br>`PlayerBrain.OnAltModifierChanged` | Classic CRPG inspect/highlight modifier. Ready for world item & door highlights. |

---

## 2. Technical Architecture

### `PlayerBrain.cs` (Input Coordinator)
- **Singleton Facade:** `private static PlayerBrain _instance` with public static methods and properties (`Selection`, `Lead`, `ActivePartyMembers`, `IsShiftPressed`, `IsAltPressed`, `StopSelectedUnits`).
- **Input Action Lifecycle:** Managed via `BoundAction` wrappers around `CRPGInput.inputactions` to ensure clean enable/disable states.
- **Raycast Dispatching:**
  - Left click queries `groundLayer` and inspects for `IUsable` components in parent hierarchy.
  - If `IUsable` found: dispatches `MoveToAndUse` to the squad `Lead`.
  - If Ground hit: feeds point to `FormationCalculator` and commands each selected unit to their offset.
- **Continuous Steering Throttling:** Uses `continuousRepathInterval = 0.08f` to stream formation paths smoothly while holding Left Click.

### `FormationCalculator.cs` (Squad Formations)
- Calculates tactical wedge / V-formations with automatic slot distribution.
- **NavMesh Tangent Facing:** Evaluates the final corner leg of travel (`path.corners[^1] - path.corners[^2]`) so squads arrive facing forward down winding corridors and U-turns.
- **Displacement Check & Relaxation Pass:** Only triggers pairwise separation relaxation if an obstacle or wall displaced a unit by $> 0.15$m from its ideal spot, preventing wall clumping with zero overhead in open rooms.

### `CharacterMovement.cs` (Movement & Interaction Actor)
- Autonomous pathfinding via Unity's `NavMeshAgent`.
- **Animator Integration:** Automatically drives `InputForward` (local forward speed) and `InputSide` (turning rate / angular speed for turn animations).
- **Arrival Alignment:** When interacting with `IUsable`, navigates to `UseSpot.position`, smoothly turns to face `UseRotation`, and calls `target.Use(sheet)`.
- **Interruptible:** Calling `MoveTo`, `Stop`, or disabling the component instantly cancels any active interaction coroutines.

### `CameraAnchor.cs` (Camera & Zoom Coordinator)
- Pans the anchor transform along the ground plane based on WASD input.
- Clamps position inside serialized `Bounds` so the camera never leaves the level boundaries.
- Directly updates `CinemachineCamera.Lens` (unpacking the `LensSettings` struct) for smooth orthographic zoom.

---

## 3. Planned Systems (Next Steps)

### A. Context Menu UI
- **Status:** Trigger hook implemented (`OnContextMenuRequested`); UI popup pending.
- **Design:** Holding Right-Click spawns a radial or vertical context menu at cursor position:
  - **Analyze / Inspect:** Calls `PlayerBrain.Inspect(target)` to open the Character Sheet UI.
  - **Talk / Trade:** Initiates dialogue system with NPCs.
  - **Context Actions:** Disarm, Pickpocket, or examine specific machine components.

### B. World Interactable Highlights (Alt Key)
- **Status:** Action and state listeners ready (`ModifierAlt`, `OnAltModifierChanged`).
- **Design:** Holding `Alt` illuminates hovering names/badges over items on the ground, chests, and doors.

### C. Tactical Combat Zones
- **Status:** Planned for Combat Phase.
- **Design:** In combat mode, free real-time movement switches to action-point budget movement across tactical Voronoi area zones and pre-allocated anchor slots.

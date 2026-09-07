# Hollow Signal — Control Schemes & Input Design

This document details the active control layout, the technical input pipeline, and the planned systems connected to player controls.

---

## 1. Active Control Scheme (Implemented)

### Mouse Controls (ARPG / BG3 Hybrid Scheme)

| Input | Interaction Pattern | Event / Action | Description |
| :--- | :--- | :--- | :--- |
| **Left Click** | **Tap / Click** | `PlayerBrain.OnDirectCommand` | Immediate action order. Dispatched instantly on press without release delay. |
| **Left Click** | **Hold & Drag** | `PlayerBrain.OnContinuousCommand` | **Diablo-style steering.** Streams world coordinates throttled at `0.08s` so the party steers continuously with the cursor. |
| **Right Click** | **Short Tap** (< 0.35s, low drag) | Selection Logic | **Selects single character** or clears selection. Holding `Shift` toggles/adds to current selection. |
| **Right Click** | **Hold & Drag** (Distance $\ge$ 10px) | Marquee Box Selection | Draws a green selection box on screen. Enclosed active party members are selected upon button release. |
| **Right Click** | **Hold Stationary** ($\ge$ 0.35s) | `PlayerBrain.OnContextMenuRequested` | Triggers a context menu event at the cursor position (for inspect, talk, interact, etc.). |
| **Mouse Wheel** | **Scroll Up / Down** | Camera Zoom | Adjusts `CinemachineCamera` orthographic lens size between **3.0** (close-up) and **6.0** (wide tactical view). |

---

### Keyboard Hotkeys & Modifiers

| Key / Modifier | Input Action | Event / Query | Description |
| :--- | :--- | :--- | :--- |
| **`W`, `A`, `S`, `D`** | `CameraMovement` | `CameraAnchor.MoveAnchor` | Moves the `CameraAnchor` relative to camera angle, clamped to map `Bounds`. |
| **`1`, `2`, `3`, `4`** | `Slot1` – `Slot4` | `PlayerBrain.SelectSlot` | Immediately selects the corresponding party member in roster order. |
| **`~` (Backquote)** | `SelectAll` | `PartySelection.SelectAll` | Selects all currently active party members at once. |
| **`Tab`** | `CycleLeader` | `PartySelection.CycleLeader` | Cycles through selected units to designate the active squad `Lead`. |
| **`Escape`** | `Deselect` | `PartySelection.Clear` | Clears all unit selections. |
| **`Shift` (Left/Right)** | `ModifierShift` | `PlayerBrain.IsShiftPressed` | Modifies selection to add/toggle units instead of replacing. |
| **`Alt` (Left/Right)** | `ModifierAlt` | `PlayerBrain.IsAltPressed`<br>`PlayerBrain.OnAltModifierChanged` | Classic CRPG inspect/highlight modifier. Ready for world item & door highlights. |

---

## 2. Technical Architecture

### `PlayerBrain.cs` (Input Coordinator)
- **Singleton Facade:** `private static PlayerBrain _instance` with public static methods and properties (`Selection`, `Lead`, `ActivePartyMembers`, `IsShiftPressed`, `IsAltPressed`).
- **Input Action Lifecycle:** Managed via `BoundAction` wrappers around `CRPGInput.inputactions` to ensure clean enable/disable states.
- **Selection Synchronization:** Listens to `_selection.OnSelectionChanged` and updates characters' 2D ground selection rings (`selectionCircle`) in real time.
- **Continuous Steering Throttling:** Uses `continuousRepathInterval = 0.08f` to prevent overwhelming Unity's NavMesh engine during hold-to-move.

### `CameraAnchor.cs` (Camera & Zoom Coordinator)
- Pans the anchor transform along the ground plane based on WASD input.
- Clamps position inside serialized `Bounds` so the camera never leaves the level boundaries.
- Directly updates `CinemachineCamera.Lens` (unpacking the `LensSettings` struct) for smooth orthographic zoom.

---

## 3. Planned Systems (Next Steps)

### A. Squad Formations (`FormationCalculator`)
- **Status:** Pending implementation.
- **Design:** When multiple units are selected, `OnDirectCommand` will not send everyone to the exact same point.
  - Slot 0 goes to the `Lead`.
  - Additional slots form dynamic offsets (wedge/triangle for 3, diamond/box for 4) based on travel direction.
  - Each target point is sampled onto the NavMesh (`NavMesh.SamplePosition`) to avoid walls and obstacles.

### B. Context-Sensitive Interaction Pathing (`IUsable`)
- **Status:** Interface exists (`World.IUsable.cs`); movement routing pending.
- **Design:** Left-clicking an object implementing `IUsable` (doors, terminals, chests):
  1. Orders the squad `Lead` to navigate to `IUsable.UseSpot.position`.
  2. Cancels if the player clicks elsewhere.
  3. Once within interaction range, calls `usable.Use(lead.sheet)`.

### C. Context Menu UI
- **Status:** Trigger hook implemented (`OnContextMenuRequested`); UI popup pending.
- **Design:** Holding Right-Click spawns a radial or vertical context menu at cursor position:
  - **Analyze / Inspect:** Calls `PlayerBrain.Inspect(target)` to open the Character Sheet UI.
  - **Talk / Trade:** Initiates dialogue system with NPCs.
  - **Context Actions:** Disarm, Pickpocket, or examine specific machine components.

### D. Tactical Combat Zones
- **Status:** Planned for Combat Phase.
- **Design:** In combat mode, free real-time movement switches to action-point budget movement across tactical Voronoi area zones and pre-allocated anchor slots (cover, machine fronts).

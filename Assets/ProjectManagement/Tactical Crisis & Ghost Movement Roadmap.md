# Hollow Signal — Tactical Crisis & Ghost Movement Roadmap

## 1. Overview & Core Pillars

This document establishes the implementation roadmap for the **Crisis System** (Turn-Based Tactical Combat) in *Hollow Signal*, bridging spatial zone navigation, input restrictions, and the **Ghost Planner Avatar** architecture.

### Key Architectural Pillars
1. **Discrete Tactical Topology (No Grid, Organic Zones):**
   - Characters navigate through pre-authored **Tactical Zones** containing discrete **Tactical Slots**.
   - Ground clicks are projected to the NavMesh and evaluated against nearby zones using true NavMesh path distances to locate the intended destination slot.
2. **Organic Combat Walk-in (No Warping):**
   - When Crisis triggers, party members do not warp or snap instantly; they physically walk to their closest available `TacticalSlot` and dock their orientation.
3. **Single-Hero Command Focus:**
   - In Crisis mode, exactly one hero receives commands at a time.
   - Box (marquee) selection remains active to accommodate player preference, but is strictly constrained to selecting only one character (the primary hero or current lead inside the box). Continuous steering is disabled.
4. **The Ghost Planner Avatar (Tactical Projection):**
   - The Ghost is an immune phantom planner representing the active hero's tactical calculation.
   - The player navigates the Ghost freely across reachable zones, examines dialogue terminals, and evaluates options without risking live character state or burning actions.
   - The Ghost records a **Plan Track** containing movement waypoints, hazard triggers stepped on (ice, spikes, electricity), and the terminal action chosen.
5. **Real Hero Traversal & Hazard Replay:**
   - Upon selecting an action (`<!>`, `<!!>`) or passing turn, the Ghost hides, and the real hero physically traverses the planned path.
   - World hazards and status effects execute sequentially along the path. If an obstacle knocks the hero down (e.g. slipped on ice) or incapacitates them, the remainder of the plan is interrupted.
   - Upon reaching the destination slot, the hero claims the slot, and their saved dialogue knot is pre-loaded for their next turn.

---

## 2. Phase Breakdown & Implementation Roadmap

```mermaid
flowchart TD
    subgraph Phase 1: Shared Core Foundation
        A1[1.1 CharacterMovement Decoupling: MoveToSlot]
        A2[1.2 Zone & Slot Spatial Registries]
        A3[1.3 Initial Combat Walk-in]
        A4[1.4 Crisis Selection & Gesture Constraints]
        A5[1.5 TacticalZoneLocator: Click to NavMesh to Slot]
    end

    subgraph Phase 2: Ghost Planner & Track Recording
        B1[2.1 Ghost Proxy Entity & Hologram Visuals]
        B2[2.2 Ghost Turn Activation & Handoff]
        B3[2.3 Track Recorder: Waypoints & Hazard Milestones]
    end

    subgraph Phase 3: Commitment & Replay Execution
        C1[3.1 Commit Triggers: Tags & Pass Turn]
        C2[3.2 State Handover: CrisisTurn & Dialogue Knot]
        C3[3.3 Real Hero Path Replay & Hazard Evaluation]
        C4[3.4 Slot Claim & Next Turn Resume]
    end

    subgraph Phase 4: Sprint & Turn Economy
        D1[4.1 Sprint 3d6 Test Flow & Composure Burn]
        D2[4.2 Winded & Stumbled State Application]
    end

    Phase 1 --> Phase 2 --> Phase 3 --> Phase 4
```

---

## Phase 1: Shared Core Foundation & Movement Pipeline

> **Objective:** Establish the spatial registries, gesture constraints, slot navigation primitives, and initial combat entry behaviors that underpin all Crisis gameplay.

### 1.1 Character Movement Decoupling (`MoveToSlot`)
- [ ] Add `MoveToSlot(AreaSlot slot, bool triggerUse = false)` to `CharacterMovement.cs`.
- [ ] Allow walking to, aligning facing, and claiming a slot without triggering its interactive use effect or use animation unless explicitly requested.
- [ ] Ensure `CancelInteraction()` safely aborts pending slot moves without corrupting previous slot claims.

### 1.2 Zone & Slot Static Spatial Registries
- [ ] Add static collections `TacticalZone.AllZones` and `TacticalSlot.AllSlots` registered on `OnEnable` / `OnDisable`.
- [ ] Cache zone centers and slot coordinates to eliminate expensive runtime scene searches.

### 1.3 Combat Initialization Walk-in
- [ ] In `CrisisManager.StartCrisis()`:
  - Query all active heroes via `PlayerBrain.ActivePartyMembers`.
  - For each hero, find the nearest available `TacticalSlot` on the NavMesh.
  - Issue `hero.movement.MoveToSlot(slot, triggerUse: false)` so heroes walk into tactical formation.
  - Set the active party leader as the initial acting character.

### 1.4 Crisis Selection & Gesture Constraints
- [ ] **Single-Unit Constraint in Crisis:**
  - In `PlayerBrain.cs` (`HandleSelectCharacter`), if `CrisisManager.Instance.IsCrisis` is true, force single-unit selection regardless of `isAdditive` / Shift modifier.
  - In `PlayerBrain.cs` (`HandleMarqueeSelect`), keep box selection active, but pick only the single primary character enclosed (preferring existing lead or the first enclosed hero).
- [ ] **Steering & Command Constraints:**
  - Disable continuous repathing / steering drag (`OnContinuousCommandMove`) when `IsCrisis` is true.
  - Ensure ground clicks issue discrete destination commands.

### 1.5 Click $\to$ NavMesh $\to$ Closest Reachable Zone/Slot Locator (`TacticalZoneLocator`)
- [ ] Create `TacticalZoneLocator.cs`:
  1. Raycast clicked screen position to get ground point $P$.
  2. Project $P$ to NavMesh: `NavMesh.SamplePosition(P, out hit, 5f, NavMesh.AllAreas)`.
  3. Filter active zones: compute Euclidean distance to candidate zones, pick the top 4–5 nearest.
  4. Compare NavMesh path lengths (`NavMesh.CalculatePath`) to find the zone with the true shortest walkable route.
  5. In the winning zone, call `zone.GetBestAvailableFallbackSlot(hit.position)`.
- [ ] Route non-slot ground clicks during Crisis through `TacticalZoneLocator`.

---

## Phase 2: Ghost Planner & Track Recording

> **Objective:** Implement the Ghost Avatar that players control during their turn to preview movement and interact with terminals without risking live character state.

### 2.1 Ghost Proxy Entity & Visuals
- [ ] Create a lightweight `GhostPlanner` GameObject/prefab:
  - Equipped with `NavMeshAgent` and `CharacterMovement`.
  - Dieselpunk holographic shader / semi-transparent silhouette material.
  - Has zero physics damage colliders; immune to world hazards.
  - References the acting hero's `Sheet` (so dialogue skill checks, perks, and stats reflect the real hero).

### 2.2 Ghost Turn Activation & Control Handoff
- [ ] At the start of the active hero's turn:
  - Spawn or enable the Ghost at the active hero's current `TacticalSlot`.
  - Transfer camera target / selection focus to the Ghost.
  - Keep the real hero in their idle stance at their origin slot.
- [ ] Provide a "Reset Ghost / Cancel Plan" UI shortcut to snap the Ghost back to the hero's origin slot.

### 2.3 Track Recorder (`TurnPlanTrack`)
- [ ] Create `TurnPlanTrack.cs` to log the Ghost's journey:
  - Records NavMesh waypoints traversed.
  - Records encountered hazard triggers along the way (e.g. `HazardMilestone { Position, HazardType, Effect }`).
  - Records the destination `TacticalSlot`.
  - Records staged dialogue interaction (`DialogueBehaviour`, `DialogueChoice`, `TargetKnotId`).

---

## Phase 3: Commitment, Replay & Hazard Execution

> **Objective:** Hand over the completed plan from the Ghost to the real hero, playing back the physical traversal and applying all consequences.

### 3.1 Commit Triggers
- [ ] Detect when the plan is locked:
  - Ghost selects a dialogue choice with `<!>` (consumes action) or `<!!>` (ends turn).
  - Ghost selects a choice that exhausts remaining turn resources.
  - Player clicks the **Pass Turn / Commit** button.

### 3.2 State Handover
- [ ] Deactivate/hide the Ghost.
- [ ] Deduct action and movement costs on the real hero's `CrisisTurn`.
- [ ] Store the dialogue reference and resulting knot ID into `realHero.dialogueSession.SetKnot(...)`.

### 3.3 Real Hero Path Replay & Hazard Evaluation
- [ ] Command the real hero to navigate along the recorded `TurnPlanTrack` waypoints.
- [ ] As the hero reaches each milestone:
  - Evaluate the corresponding hazard or effect (e.g., slip check on ice, damage from spikes, trap trigger).
  - **Interrupt Check:** If the hero is knocked prone (`[Stumbled & Sprawled]`), stunned, or incapacitated:
    - Stop movement immediately.
    - Cancel remaining waypoints.
    - Dock hero at the point of interruption.
- [ ] If path completes uninterrupted:
  - Hero steps into the destination slot and aligns facing.
  - Calls `slot.CommitTurn(hero)`.

### 3.4 Next Turn Dialogue Resumption
- [ ] When the hero's next turn arrives, if an active dialogue session with a saved knot exists, the terminal/dialogue prompt is pre-loaded at that knot, ready for immediate progression.

---

## Phase 4: Sprint & Turn Economy Integration

> **Objective:** Connect double-movement, sprint rolls, and status effects into the planned trajectory.

### 4.1 Sprint Detection & Prompt
- [ ] When the Ghost moves across 2 tactical zones:
  - Display the Sprint badge / prompt: *"Sprint required to act (3d6 vs DC 11)"*.
- [ ] Support Composure push (spend 2 Composure to reroll lowest die if hero has mobility Mastery).

### 4.2 Sprint Outcomes
- [ ] **Pass:** Hero retains Action for the turn.
- [ ] **Fail:** Action is lost; hero receives temporary decorator `[Winded]` ($-1$ Defense until next turn).
- [ ] **Critical Fumble (Double 1s):** Hero gains `[Stumbled & Sprawled]`, knocked prone on the track.

---

## 3. Immediate Next Steps

1. **Implement `MoveToSlot` in `CharacterMovement.cs`** (clean slot arrival without auto-use).
2. **Add static registries to `TacticalZone.cs` and `TacticalSlot.cs`**.
3. **Hook party slot walk-in inside `CrisisManager.StartCrisis()`**.
4. **Implement `TacticalZoneLocator.cs`** (Ground click $\to$ NavMesh $\to$ candidate zones $\to$ best slot).
5. **Apply Crisis selection guards in `PlayerBrain.cs`**.

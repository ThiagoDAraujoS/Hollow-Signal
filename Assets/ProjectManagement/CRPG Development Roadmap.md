# Hollow Signal — Development Roadmap

## Phase 1: Core Memory, Foundation & Locomotion (Complete)
- [x] **Blackboard & Save System:** Global, Scene, and Entity dictionaries, UUID generation, and JSON serialize/deserialize loop.
- [x] **Localization & Text Registry:** Map string IDs to text without hardcoding.
- [x] **Skill & Mastery Database Parsers:** JSON parsers loading mechanical strings and Mastery definitions.
- [x] **Character Sheet Component:** Active masteries, skill level calculations with mastery deltas.
- [x] **NavMesh & Click-to-Move:** Walkable layer, flipped mouse controls (Left: Command, Right: Select), continuous Diablo-style steering.
- [x] **Player Brain & Unit Selection:** BG3-style single/double-click selection, box select, camera tracking, ModifierAppend (Shift) multi-selection.
- [x] **Squad Movement & Formations:** FormationCalculator tactical wedge distribution, arrival facing, stop command.
- [x] **Base Scene & Sleep-Spawn Coordinator:** 5-step boot loop, GameSessionManager waking and placing heroes on map load.

---

## Phase 2: Skill Checks, Interactions & Dialog Engine (Current)
- [ ] **2.1 Skill Check Resolver & Dice Engine:**
  - Query `CharacterSheet.GetEffectiveSkill(skill)`, roll dice against Target DC.
  - Return rich results: Critical Success, Success, Failure, Critical Failure with degree of margin.
  - Foundation for all world challenges, lockpicking, terminal hacking, and combat Dash/Ambush tests.
- [ ] **2.2 Dialog & Interaction Prompt UI:**
  - Prompt modal with title, description, and interactive action options.
  - Action options display required skill and target DC previews.
  - Dice roll resolution feedback and branching task execution callbacks.
- [ ] **2.3 Interactive Objects & Map Portals (IUsable & Map Transitions):**
  - Implement interactable doors, portals, terminals, and transition triggers implementing `IUsable`.
  - Character arrival alignment to `UseSpot` and `UseRotation`.
  - Usable confirmation handshake: arrival triggers prompt dialog; player confirmation burns action and executes task.
  - Map transition pipeline: disable agents -> record target `AnchorPoint` -> unload map -> load additive map -> rewarp squad on NavMesh.
- [ ] **2.4 Hold-to-Open Context Menu UI:**
  - Hook into `SelectionGestureHandler.OnContextMenuRequested`.
  - Classic RPG context menu on right-click hold (Examine, Use, Talk, Attack).
- [ ] **2.5 Alt-Key World Highlight System:**
  - Hook `modifierAltActionRef` and `PlayerBrain.OnAltModifierChanged`.
  - Display screen-space highlight indicators/tooltips over interactable objects, items, and NPCs while Alt is held.

---

## Phase 3: Tactical Zone / Turn-Based Combat System (Crisis Mode)
- [ ] **3.1 Spatial Topology: Tactical Zones & Modular Slots:**
  - Define `TacticalZone` nodes across key map areas with neighbor adjacency graph.
  - Implement `TacticalSlot` pre-selected standing positions with occupancy tracking.
  - Create `ISlotModifier` for modular slot bonuses (+1 skill bonuses, cover, terminal spots, environmental hazards).
  - Support free intra-zone spot adjustments at the start of a turn (0 Movement cost).
  - Voronoi nearest-zone click raycasting and BFS step-distance path calculation.
- [ ] **3.2 Turn Budget & Action Economy:**
  - Standard Turn: 1 Zone Move + 1 Action.
  - Double Move: 2 Zone Moves + 0 Actions.
  - Overdrive / Dash: 2 Zone Moves + 1 Action (calls Skill Check Resolver for Dash test; failure halts movement at second zone and inflicts penalty).
- [ ] **3.3 Command Dissector & Usable Handshake:**
  - Dissect clicks into atomic execution plan: `[Walk, Walk, (Dash Roll), Use]`.
  - Confirmation handshake on terminals/attacks before action expenditure.
- [ ] **3.4 IGOUGO Turn Flow & Ambush Checks:**
  - Ambush roll at Crisis start (perception/hearing check to seize initiative; enemies go first by default).
  - Team phase state machine: Player Phase <-> Enemy Phase.
  - End Turn button and turn budget reset loop.

---

## Phase 4: Progression & Narrative Expansion
- [ ] **Inventory & Equipment:** Equippable items modifying character stats/skills.
- [ ] **Loot & Scavenging Containers:** World containers feeding into inventory.
- [ ] **Narrative Dialog Engine:** Full branching conversation trees with condition gating and journal updates.
- [ ] **Level Up & Mastery Acquisition:** Experience thresholds, leveling UI, choosing new masteries.

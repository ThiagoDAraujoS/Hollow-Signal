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

## Phase 2: Boot Scene Architecture & Core Menu Suite (Current Milestone)
> **Context & Pivot:** Rather than relying on fragile temporary mockups that require double work later, we paused gameplay iteration to construct the definitive, production-grade UI harness in the `Boot` scene. This establishes the visual language, Animator state choreography, and foundational screens needed before integrating in-game dialogue and map loops.

- [x] **2.1 Spline-Arc Carousel & Save Bullet System (Complete):**
  - Continuous parametric track rail with square-wave cam profile and start/end card padding.
  - Tactile Save Bullet prefab with broken-neon pulse shader/curve and single-select tagging.
  - Decoupled `SaveBulletData` container struct.
- [x] **2.2 Load Game Panel & Coordinator (Complete):**
  - `LoadPanelController` bridging `SaveSystem.GetSaveFileList()` to `SaveCarouselController`.
  - Animation Event hooks: `BuildLoadList()` and `DestroyLoadList()`.
  - Asynchronous game session boot trigger via `SceneCoordinator.StartGameSessionAsync(slot)`.
- [ ] **2.3 Save Game Panel (Tomorrow's Priority):**
  - Adapt `SaveCarouselController` for save game capture and overwrite flow.
  - "New Save" slot creation element at index 0.
  - Slot overwrite modal confirmation prompt.
  - Save file metadata snapshot rendering (location, play time, hero roster preview).
- [ ] **2.4 Main Menu & Master UI Choreographer:**
  - Production Title Menu layout (New Game, Continue, Load Game, Save Game, Settings, Quit).
  - State-driven Animator architecture (Dumb buttons triggering Animator parameters; keyframes driving scene curtains and view swaps).
  - Curtain fade-out/fade-in transitions synchronized with scene load completion.
- [ ] **2.5 Settings Panel (Essential Scope):**
  - Language toggle/dropdown to test and validate multi-file streaming localization runtime.
  - Master volume slider placeholder.
  - *(Full display settings, keybinding rebinding, and credits deferred to late polish phase)*.

---

## Phase 3: Dialogue Engine, Prompts & Narrative Runtime (Next Milestone)
> **Context:** The compiler pipeline and multi-file localization stream are built. Once the Boot UI suite is finalized, we build the in-game message presentation UI to test live conversation trees.

- [x] **3.1 Dialogue Compiler & Code Generation (Complete):**
  - CSV-to-AST parser and code generator emitting type-safe C# dialogue node trees.
- [x] **3.2 Multi-File Streaming Localization (Complete):**
  - Extended localization registry to stream language files on demand.
- [ ] **3.3 In-Game Dialogue Box & Message Presentation UI:**
  - Tactile CRT/terminal presentation window for dialogue, speaker names, and portraits.
  - Typewriter text effect with click-to-skip.
  - Interactive response choice buttons with skill DC previews and disabled state styling.
- [ ] **3.4 Dialogue Runtime Runner & State Machine:**
  - Runtime interpreter traversing compiled C# dialogue tree nodes.
  - Blackboard condition evaluation (`CheckFlag`, `GetVariable`) and mutation (`SetFlag`, `ModifyStat`).
- [ ] **3.5 Skill Check Resolver & Dice Engine:**
  - Query `CharacterSheet.GetEffectiveSkill(skill)`, roll dice against Target DC.
  - Return rich margins: Critical Success, Success, Failure, Critical Failure.
  - Integrate skill check rolls directly into dialogue choice branching.

---

## Phase 4: World Interaction & Exploration Mechanics
- [ ] **4.1 Interactive Objects & Map Portals (IUsable & Map Transitions):**
  - Implement interactable doors, terminals, containers, and transition triggers implementing `IUsable`.
  - Character arrival alignment to `UseSpot` and `UseRotation`.
  - Usable confirmation handshake: arrival triggers prompt dialog; player confirmation burns action and executes task.
  - Map transition pipeline: disable agents -> record target `AnchorPoint` -> unload map -> load additive map -> rewarp squad on NavMesh.
- [ ] **4.2 Hold-to-Open Context Menu UI:**
  - Hook into `SelectionGestureHandler.OnContextMenuRequested`.
  - Classic RPG context menu on right-click hold (Examine, Use, Talk, Attack).
- [ ] **4.3 Alt-Key World Highlight System:**
  - Hook `modifierAltActionRef` and `PlayerBrain.OnAltModifierChanged`.
  - Display screen-space highlight indicators/tooltips over interactable objects, items, and NPCs while Alt is held.

---

## Phase 5: Tactical Zone / Turn-Based Combat System (Crisis Mode)
- [ ] **5.1 Spatial Topology: Tactical Zones & Modular Slots:**
  - Define `TacticalZone` nodes across key map areas with neighbor adjacency graph.
  - Implement `TacticalSlot` pre-selected standing positions with occupancy tracking.
  - Create `ISlotModifier` for modular slot bonuses (+1 skill bonuses, cover, terminal spots, environmental hazards).
  - Support free intra-zone spot adjustments at the start of a turn (0 Movement cost).
  - Voronoi nearest-zone click raycasting and BFS step-distance path calculation.
- [ ] **5.2 Turn Budget & Action Economy:**
  - Standard Turn: 1 Zone Move + 1 Action.
  - Double Move: 2 Zone Moves + 0 Actions.
  - Overdrive / Dash: 2 Zone Moves + 1 Action (calls Skill Check Resolver for Dash test; failure halts movement at second zone and inflicts penalty).
- [ ] **5.3 Command Dissector & Usable Handshake:**
  - Dissect clicks into atomic execution plan: `[Walk, Walk, (Dash Roll), Use]`.
  - Confirmation handshake on terminals/attacks before action expenditure.
- [ ] **5.4 IGOUGO Turn Flow & Ambush Checks:**
  - Ambush roll at Crisis start (perception/hearing check to seize initiative; enemies go first by default).
  - Team phase state machine: Player Phase <-> Enemy Phase.
  - End Turn button and turn budget reset loop.

---

## Phase 6: Progression & Narrative Expansion
- [ ] **Inventory & Equipment:** Equippable items modifying character stats/skills.
- [ ] **Loot & Scavenging Containers:** World containers feeding into inventory.
- [ ] **Level Up & Mastery Acquisition:** Experience thresholds, leveling UI, choosing new masteries.

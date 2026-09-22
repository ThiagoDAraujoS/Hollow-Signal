# Hollow Signal — Master Development Roadmap

## Core Vision & Design Pillars
- **Dieselpunk Arcane Story:** A story-driven tactical CRPG set in an isolated city trapped inside an atmospheric bubble.
- **The Triad Simulation Engine (Masteries → Black Box Skills → Problem Archetypes):**
  - **Masteries:** Character background identities (e.g. *Dockyard Boiler-Hauler*) acquired at creation and level-up.
  - **Black Box Skills:** An extensive list (~100) of hidden skills never directly manipulated by the player. Masteries and equipped items silently provide stacking bonuses to these skills.
  - **Problem Archetypes:** Physical obstacles, narrative dilemmas, attacks, and defenses presented as sensory situations with divergent physical approaches.
  - **Unified Action Resolution:** Whether picking a rusted lock, negotiating with a smuggler, or swinging an industrial wrench in combat, all actions flow through the same engine: **Sensory Problem Description → Deduce Approach → Query Best Black Box Skill & Contributing Mastery → Roll d20 vs DC (with optional Advantage / Reroll burn) → Reveal Mastery & Method Quip**.
- **Tactical Zone Crisis Mode:** Fast, fluid, non-grid tactical combat where characters move between spatial zones, claim spots that grant **temporary masteries** (cover, high ground, machinery controls), and execute actions through the archetype choice interface.

---

## Phase 1: Core Foundation, Memory & Locomotion (COMPLETE)
- [x] **Memory Model & Multi-File Streaming:** Modular data loading, JSON persistence, and dynamic asset streaming.
- [x] **Blackboard & Save System:** Global, Scene, and Entity blackboard dictionaries with full serialize/deserialize loop.
- [x] **Localization Registry:** Multi-file localization streaming without hardcoded strings.
- [x] **Skill & Mastery Database:** Structured mechanical enums, JSON definitions, and data asset parsers.
- [x] **NavMesh & Locomotion:** NavMesh navigation, inverted CRPG mouse controls (Left: Command/Move, Right: Select), Diablo-style continuous steering.
- [x] **Player Brain & Unit Selection:** Single-click, double-click, drag-box marquee, and Shift-append squad selection.
- [x] **Squad Formations:** `FormationCalculator` tactical wedge distribution, arrival facing alignment, and stop command.
- [x] **Boot Scene & Sleep-Spawn Coordinator:** 5-step boot loop, GameSessionManager waking and placing heroes on map load.
- [x] **Dialogue Compiler Pipeline:** CSV-to-AST parser and code generator emitting compiled C# dialogue state machines (`ExplorationConsoleDialogue.cs`).
- [x] **Dialogue Graph Runtime:** Core node traversal, branching choice evaluation, one-shot choice consumption, and knot navigation.

---

## Phase 2: Menu Suite & UI Shell
### 2.1 Boot Menu Suite
- [x] **Main Menu:** Production Title Menu layout (New Game, Continue, Load Game, Settings, Quit).
- [x] **Load Game Menu:** Spline-arc carousel with tactile Save Bullets, metadata snapshot preview, and async session loading.
- [ ] **Boot Settings Menu:**
  - Language toggle/dropdown validating multi-file streaming localization runtime.
  - Master, Music, and SFX volume sliders.
  - Fullscreen / Resolution display options.

### 2.2 In-Game System Suite (Pause / Escape Menu)
- [ ] **In-Game Menu Frame:** Modal pause overlay invoked via `Escape`.
- [ ] **In-Game Save Menu:** Save Game carousel view with "Create New Save" bullet at index 0 and overwrite confirmation.
- [ ] **In-Game Load Menu:** Shared carousel instance allowing quick loads during gameplay.
- [ ] **In-Game Settings Menu:** Audio, video, and gameplay preferences while in session.
- [ ] **Exit Flow:** Return to Boot Scene with memory cleanup and session teardown.

### 2.3 Tactical TurnTable Widget (Crisis UI)
- [x] **3D Mechanical TurnTable Prefab:** Complete physical assembly of housing, gears, indicator lens, arrow, and spark emitters (`TurnTable.prefab`).
- [x] **Animator State Choreography:** Authored `AC_TurnTableAnimController` with 7 state clips (`ANIM_Off`, `ANIM_TurningOn`, `ANIM_On`, `ANIM_PickingASide`, `ANIM_Green`, `ANIM_Red`, `ANIM_TurningOff`).
- [x] **Dynamic Filament Flicker & Voltage Drops:** Perpetual Perlin micro-shimmer and intermittent voltage drops modulating animatable brightness fields (`centerBrightness`, `allyBrightness`, `enemyBrightness`).
- [x] **Mechanical Gear Drive & Clock Throttle:** Curve-sampled clock throttle (`throttleCurve`) scaling evaluation cadence with `gearSpeed` and `referenceSpeed` pivot.
- [x] **Directional Spark Particle Systems:** Direction-aware burst triggers (`CastParticles`) randomly firing emitters from ally/enemy pools with snappier half-durations.
- [x] **MVC Observer Integration with CrisisManager:** Pure View architecture observing `CrisisManager` model lifecycle events (`OnCrisisStarted`, `OnPlayerPhaseStarted`, `OnEnemyPhaseStarted`, `OnCrisisEnded`) via boolean parameters (`IsOn`, `IsRed`).

---

## Phase 3: Character Sheet, Inventory & Resource Economy
> **Design Goal:** Expand the character sheet from a raw stat container into a full CRPG identity sheet where equipped items feed into the Black Box skills and players manage their push-your-luck resource pool.

### 3.1 Character Sheet Expansion & Stats
- [ ] **Core Attributes & Derived Stats:** Health, Action Points / Move Speed, Initiative, and Resistances.
- [ ] **Reroll / Advantage Resource Pool (e.g., Grit / Steam / Resolve):**
  - Burn resource to gain **Advantage** (roll 2 d20s, take the highest).
  - Burn resource to **Reroll** a failed check at critical narrative moments.
  - Replenished via rest, consumables, or roleplaying triumphs.
- [ ] **Dynamic Skill Evaluation:** Method querying `(Base + Mastery Deltas + Equipment Deltas + Temporary Spot Buffs)` for any hidden skill.

### 3.2 Inventory & Equipment System
- [ ] **Item Data Structure:** Weapons, armor, tools, curios, and consumables with stat/skill modifiers.
- [ ] **Equipment Slots:** Main Hand, Off-Hand, Head, Body, Accessory 1, Accessory 2.
- [ ] **Equipment Modifiers:** Items inject direct bonuses/penalties into the Black Box skills (e.g. *Hydraulic Wrench: +3 FixMachinery, +2 Smash*).
- [ ] **Tactile Grid / Slot Inventory UI:** Dieselpunk inventory grid with equip/unequip, item tooltips, and weight/slot limits.

### 3.3 Character Sheet & Progression UI
- [ ] **Character Sheet Inspection UI:** View character identity, equipped masteries, health/status, and equipment.
- [ ] **Character Level-Up Menu:** Spend progression points to acquire new background Masteries.
- [ ] **Character Creator Menu:** Custom hero creation at New Game (portrait, name, starting Masteries, starting equipment).

---

## Phase 4: Black Box Resolution & Narrative Runtime
> **Design Goal:** Seamlessly connect physical world obstacles to the player's deduced choices, hidden skill evaluation, and narrative payoff.

### 4.1 World Interaction & Dialogue Architecture
- [x] **World Input Pausing:**
  - Input actions and movement commands paused during active dialogue.
  - Active dialogue stops interacting characters and resets command dispatchers.
  - Re-enables world controls cleanly upon dialogue exit.
- [x] **Click-to-Start Dialogue & IUsable Handshake:**
  - Walk up to interactable object/NPC, align to `UseSpot` and `UseRotation`, reserve and claim slot (`AreaSlot`), then launch dialogue.
  - Prevents party members from double-occupying active slots or interrupting in-use dialogues.
- [x] **Multi-Hero Dialogue Synchronization:**
  - Asynchronous per-character dialogue sessions (`CharacterDialogueSession`).
  - Active party leader owns current screen slot; cycling or selecting characters dynamically swaps active dialogue UI.
  - Automatic shared background (`SPR_DialogMenu`) lifecycle management ensuring clean deactivation on dialogue exit.

### 4.2 Tactical Dialogue Costs & Crisis Integration
- [x] **Tag Parsing (`<!>`, `<!!>`, `<M>`):** Strips syntactic tags from choice strings and maps them to tactical flags (`consumesAction`, `endsTurn`, `consumesMove`).
- [x] **Crisis Turn Economy Integration:** Evaluates hero's `CrisisTurn` during crisis mode to deduct action/move points upon selection or end turn.
- [x] **Resource Availability & Choice Locking:**
  - Exploration mode: all choices freely selectable with standard colors.
  - Crisis mode: unaffordable choices are grayed out and non-interactable with explanatory tooltips.
  - Distinct tactical color themes for Actions, Move, and End-Turn choices.

### 4.3 Black Box Skill Check Engine
- [ ] **Skill Check Resolution Pipeline:**
  - Hook `DialogueSkillCheck` evaluation into `DialogueController.GoToKnot`:
    1. Query acting hero's `CharacterSheet` for highest applicable skill bonus.
    2. Trace the contributing `Mastery` (or equipment) granting that bonus.
    3. Roll `d20 + TotalBonus vs Target DC`.
    4. Support Advantage / Disadvantage (2d20 pick high/low).
    5. Prompt player for Resource Burn (Grit/Reroll) upon failure.
    6. Transition to `successKnot` or `failureKnot`.
- [ ] **Mastery Reveal & Flavor Quip Display:**
  - Format transcript output with dieselpunk styling:
    - Action Header: `[ACTION: BRUTE FORCE]`
    - Mastery Credit: `:: RELEVANT MASTERY: DOCKYARD BOILER-HAULER ::`
    - Method Quip: Narrative text explaining how their specific background solved the obstacle.
    - Roll Breakdown: `[PASSED] // Roll: 12 + 3 = 15 vs DC 13`.

### 4.4 Dialogue UI Presentation Polish
- [x] **Choice Tooltip Display:** Dynamic screen-space hover tooltips displaying tactical turn costs and lockout reasons.
- [ ] **Dialogue Scroll Unrolling Animation:** Play the scroll unrolling / opening animation when entering dialogue and roll-up animation on exit instead of instant popping.
- [ ] **Typewriter Text Effect:** Smooth text animation with click-to-skip.
- [ ] **Hotkeys:** Keyboard number keys (`1`, `2`, `3`...) to trigger choices.
- [ ] **Dialogue Test Room (`DialogTest.unity`):** End-to-end verification of dialogue, branching, skill rolls, and mastery reveals.

---

## Phase 5: World Exploration & Map Topology
- [ ] **Interactive World Objects:** Usable containers, doors, valve wheels, terminals.
- [ ] **Map Portals & Seamless Map Swapping:**
  - Transition trigger: disable agents → record target anchor → unload current map → load additive map → re-spawn party on NavMesh.
- [ ] **(Deferred) Alt-Key World Highlight:** Silhouette or label highlight over interactables when holding Alt.

---

## Phase 6: Tactical Zone & Combat Simulation (Crisis Mode)
> **Design Goal:** Turn-based combat that uses the exact same simulation rules as dialogue. Actions, attacks, and defenses are choices made through problem archetypes.

### 6.1 Spatial Topology: Tactical Zones & Modular Spots
- [ ] **Tactical Zones:** Pre-authored spatial polygon zones across combat areas with adjacency graphs.
- [ ] **Modular Spots:** Fixed positions inside zones (Cover, High Ground, Terminal console, Chokepoint).
- [ ] **Temporary Masteries from Spots:**
  - Stepping onto a Heavy Cover spot grants temporary mastery: *Covered Defender (+3 Dodge, +3 BallisticDefense)*.
  - Stepping onto a Console spot grants temporary mastery: *Substation Operator (+4 HackCircuits)*.
- [ ] **Intra-Zone Micro Movement:** Free repositioning between spots inside the current zone (0 Move cost).

### 6.2 Turn Economy & Action Flow
- [ ] **Turn Budget:** 1-2 Zone Moves + 1 Action per turn.
- [ ] **Actions via Dialogue/Choice Archetypes:**
  - Attacking an enemy or operating an environmental hazard opens an action prompt:
    - *[1] [OVERCHARGE] Slam power conduit into the wet floor.*
    - *[2] [POINT BLANK] Fire trench shotgun at the lead automaton.*
    - *[3] [TAKE COVER] Hunker behind reinforced sandbags.*
- [x] **IGOUGO Turn Flow & Visual Indicator:**
  - Player Phase ↔ Enemy Phase lifecycle managed by `CrisisManager`.
  - TurnTable widget providing physical mechanical feedback for phase transitions.
- [ ] **Enemy Phase Simulation:** AI units navigate zones, claim spots, and execute archetype actions against players.

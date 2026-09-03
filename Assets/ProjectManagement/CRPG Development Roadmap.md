## Phase 1: Core Memory & Foundation

These systems form the bedrock. Nothing else can communicate or persist without them.

- [x] **Blackboard & Save System:** Establish the Global, Scene, and Entity dictionaries, UUID generation, and the JSON serialize/deserialize loop.
- [x] **Localization & Text Registry:** Build the manager that maps string IDs to text. All subsequent systems will rely on this instead of hardcoded strings.
- [x] **Skill & Mastery Database Parsers:** Write the scripts that read your JSON files and load the raw mechanical strings and Mastery definitions into memory.

## Phase 2: Actors & World Presence

With data in memory, you need entities to hold that data and a way to move them.

- [x] **Character Component (Stats):** Build the component that tracks the active masteries and skill levels (relying on the Skill/Mastery databases). _(C# code finished in_ _CharacterSheet-v14.cs__!)_
- [ ] **Movement & Area System:** Set up the NavMesh agents, anchor points, and the logic to move a character to a specific coordinate.
- [ ] **Brain Controller (Input):** Build the raycaster that selects characters and sends movement commands to the Movement System.
- [ ] **Camera Controller:** Implement the isometric camera so you can actually see and navigate the test space you are building.
- [ ] **Base Scene & Entity Prefabs (Spawn Asleep):** Construct the foundational Unity prefabs (Hero, NPCs, chests, doors) ensuring they start disabled (`activeSelf = false` in the inspector) and are pre-configured with `UniqueId`, `BlackboardClient`, and their respective state scripts.
- [ ] **Area Batch Loader & Sleep-Spawn Coordinator:** Build the system that runs your 5-step boot loop: destroy live dynamic instances -> instantiate scene prefabs disabled -> determine relevant sector/area IDs -> instruct the Blackboard to deserialize only those memories -> run `OnLoadState` and wake them up with `SetActive(true)`.

## Phase 3: Interaction & Logic

Now the player can move, they need to interact with the world using their data.

- [ ] **Problem Archetype Database:** Parse the JSON defining the rules for doors, locks, and traps.
- [ ] **Condition Evaluator:** Write the utility that reads logic like `G_Money > 500` and checks it against the Blackboard.
- [ ] **Challenge Evaluator:** Place this on world objects. It uses the Brain Controller (to know who clicked), the Character Component (to check stats), the Archetype Database (to know the rules), and the Condition Evaluator (to verify state).
- [ ] **Dry Archetype Use Strings:** Connect the Challenge Evaluator to the Localization Registry to output basic success/failure text.

## Phase 4: The Narrative Engine

The world is interactive; now it needs to speak.

- [ ] **Dialog System Syntax:** Finalize the text formatting rules (how you write nodes, choices, and condition checks in your raw text files).
- [ ] **Dialog Parsing Engine (File Reader):** Write the compiler that ingests your syntax and converts it into logic nodes and JSON structures.
- [ ] **Dialog UI Controller:** Build the front-end that reads the parsed nodes, displays the text, and renders clickable choices.

## Phase 5: Progression & Game Loop

The core game is fully playable. Now add the RPG layers.

- [ ] **Inventory & Equipment:** Define items and build the logic for equipping them (which should hook back into the Character Component to boost stats).
- [ ] **Loot & Scavenging Manager:** Connect world containers to the Inventory System.
- [ ] **Quest & Journal Manager:** Create the system that listens to Blackboard changes and updates the UI journal.
- [ ] **Level up system with mastery acquisition:** Build a system where characters can level up and choose new masteries and skills.

## Phase 6: World Management & Polish

The systems required to turn a single test scene into a full game.

- [ ] **Scene Transition Manager:** Connects to the Blackboard to offload scene data, unloads the map, loads the next, and re-initializes.
- [ ] **HUD & System Menus:** Build the party portraits, pause menu, and save/load UI (hooking into the Phase 1 Save System).
- [ ] **Time, Weather, & Audio:** The final atmospheric systems that listen to Blackboard states (e.g., monsoon cycle) and react globally.

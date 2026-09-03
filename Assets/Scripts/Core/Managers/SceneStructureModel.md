# Architectural Context: CRPG Scene Management System

This document describes the additive, multi-scene architecture used to manage game sessions, user interfaces, and level transitions. It serves as a structural blueprint and reference for writing future scene loaders, area coordinators, and menu controllers.

---

## 1. The Additive Multi-Scene Layout
To prevent reloading global managers and to enable seamless visual transitions (such as loading screen overlays), the game runs up to **three core scenes simultaneously** during gameplay.

```
▼ [1. Menu / Boot Scene]          <-- Loaded at launch; NEVER unloaded.
   ├── SaveSystem (Persistent GameObject)
   ├── InputManager, Audio, etc.
   └── [MainMenu_Canvas] (Deactivated via SetActive(false) during active gameplay)

▼ [2. GameSession Scene]          <-- Persistent during gameplay; unloaded on return to Main Menu.
   ├── Blackboard (Passive in-memory DB)
   └── GameSessionManager (Session coordinator, inherits TrackedBehaviour)

▼ [3. Active Level Scene]         <-- Swapped additively during zone transitions.
   ├── Map Geometry & NavMesh
   ├── AreaLoadCoordinator (Local map initialization manager)
   └── Sleep-Spawn Entities (NPCs, breakables, interactive objects)
```

---

## 2. Component Responsibilities

### A. Boot / Menu Scene (The Persistent Anchor)
*   **Role:** The launch point of the game application. It acts as both the Title Menu and the root anchor of all global subsystems.
*   **Persistent Managers:** High-level singletons (like `SaveSystem`) live as flat, top-level GameObjects in this scene so they survive throughout the game's entire runtime.
*   **UI Sandboxing:** The Main Menu UI lives in this scene. When entering gameplay, we do not unload the scene; we simply deactivate the main menu canvas overlay (`MainMenu_Canvas.SetActive(false)`), allowing global elements to remain active.

### B. GameSession Scene (The Session Coordinator)
*   **Role:** The bridge between active level instances and global game state.
*   **GameSessionManager:** Inherits from `TrackedBehaviour`. It is loaded when gameplay starts. It reads core state (like the current map name) from the `Blackboard` and handles the mechanics of opening and closing level scenes additively.

### C. Active Level Scene (The Local Environment)
*   **Role:** Contains localized environment data, colliders, and local entities.
*   **AreaLoadCoordinator:** A scene-specific manager responsible for coordinating local map startup. It prevents local entities from awaking prematurely until their respective zone file is fully loaded into the `Blackboard`.

---

## 3. Step-by-Step Lifecycles

### I. App Boot to Main Menu
1.  The game launches; Unity loads the **Menu/Boot Scene** (containing the `SaveSystem` and the Main Menu UI canvas).
2.  The Menu Scene additively loads a lightweight, animated **Cool Fancy Background Scene** to display behind the title menu buttons.
3.  The Main Menu UI queries `SaveSystem.Instance.GetSaveFileList()` to populate the save/load menu list.

### II. Main Menu into Gameplay (The Bootstrap Sequence)
1.  The player clicks **"Load Save"** or **"New Game"** on the Main Menu UI.
2.  The Menu UI configures the active save slot on the singleton:
    `SaveSystem.Instance.SetSaveSlot(selectedSlotName)`.
3.  The **Cool Fancy Background Scene** is unloaded to free up memory.
4.  The **MainMenu_Canvas** is deactivated (`SetActive(false)`).
5.  The **GameSession Scene** is loaded additively.
6.  `GameSessionManager` starts up:
    *   Calls `SaveSystem.Instance.LoadFilesAsync(new[] { "global", "characters" })` to read core state.
    *   Once loaded, its `BlackboardClient` binds and pulls its state, populating the tracked `currentMapName` variable.
    *   It triggers `SceneManager.LoadSceneAsync(currentMapName.Value, LoadSceneMode.Additive)` to open the gameplay map.

### III. Zone-to-Zone Transition (Leaving the Desert for the Mountains)
1.  The player steps on a transition trigger (e.g., leaving `zone_desert` to enter `zone_mountain`).
2.  `GameSessionManager` initiates the transition sequence:
    *   Saves active session data: `Client.SaveToBlackboard()`.
    *   Runs an atomic auto-save: `await SaveSystem.AutosaveAsync()`.
    *   Unloads the old map scene additively: `SceneManager.UnloadSceneAsync("zone_desert")`.
    *   **Soft-Purges Volatile Memory:** Frees up RAM by releasing the old map data from the Blackboard: `SaveSystem.ReleaseFile("zone_desert")`.
    *   Mutates the tracked variable `currentMapName.Value = "zone_mountain"`.
    *   Loads the incoming map scene additively: `SceneManager.LoadSceneAsync("zone_mountain", LoadSceneMode.Additive)`.
3.  The mountain scene's `AreaLoadCoordinator` takes over local initialization.

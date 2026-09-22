# Asynchronous Multi-Hero Dialogue Architecture

## 1. System Vision & The Baldur's Gate 3 Paradigm

In Hollow Signal, dialogue is not a global modal that freezes the game world. In both real-time exploration and **Crisis Mode (Turn-Based Tactical Combat)**, heroes act with individual agency:
- **Hero 1** can initiate a dialogue with a jammed door during their turn, pick a turn-consuming brute force action (`<!>`), and continue moving or end their turn (`<!!>`).
- While Hero 1's action is underway, the player can click **Hero 2**:
  - The camera smoothly tracks / refocuses onto Hero 2.
  - Hero 1's dialogue window is cleanly hidden (`SetVisible(false)`), breaking out of the conversation view without resetting or destroying Hero 1's conversation progress.
  - Hero 2 can move across the room, kick a villain, or inspect a terminal.
  - When the player selects back to Hero 1, the camera smoothly refocuses on Hero 1 and reopens Hero 1's dialogue window, restoring the exact knot and options where they left off.

---

## 2. Window Architecture: Dedicated Window per Hero

Instead of a single window that clears and overwrites text when swapping heroes, the system allocates **one dedicated `DialogueController` screen per party member** (managed by `PlayerBrain`).

```
┌────────────────────────────────────────────────────────────┐
│                        PlayerBrain                         │
│   • Listens to Selection.OnSelectionChanged                │
│   • Calls UpdateDialogueScreens() on hero swap             │
│   • Enforces: EXACTLY ONE window visible at any time       │
└────────────────────────────┬───────────────────────────────┘
                             │
            ┌────────────────┴────────────────┐
            ▼                                 ▼
┌─────────────────────────┐       ┌─────────────────────────┐
│CharacterDialogueSession │       │CharacterDialogueSession │
│       (on Hero 1)       │       │       (on Hero 2)       │
│                         │       │                         │
│ • State: In Dialogue    │       │ • State: Idle / Active  │
│ • Owns Window Screen 0  │       │ • Owns Window Screen 1  │
│ • Separate Transcript   │       │ • Separate Transcript   │
│ • Preserved Knot State  │       │ • Preserved Knot State  │
└─────────────────────────┘       └─────────────────────────┘
```

### Why Dedicated Windows & Session Tracking?
1. **Zero State Rebuilding**: Switching between heroes is an instantaneous visibility toggle (`SetVisible(true/false)`). The UI does not need to serialize, parse, and rebuild rich-text layouts every time the player clicks between party portraits.
2. **Preserved Scroll Positions**: If Hero 1 scrolled up into the transcript to review an ancient inscription, switching to Hero 2 and back leaves Hero 1's scroll bar exactly where they left it.
3. **Clean Separation**: Transcript histories never interleave. Hero 1's lockpicking log remains purely on Hero 1's screen.
4. **Camera Synchronization**: When swapping lead selection, `CameraAnchor.Track(Lead.BodyTransform)` frames the active character, and `UpdateDialogueScreens()` displays only the active leader's conversation window if they have one ongoing.

---

## 3. Tactical Turn Economy Integration (`<!>`, `<!!>`, `<M>`)

During tactical Crisis mode, dialogue options consume the character's turn budget:

| Tag | Meaning | Resource Consumption | Styling in Crisis | Hover Tooltip |
| :--- | :--- | :--- | :--- | :--- |
| `<!>` | **Major Action** | `turn.ConsumeAction()` | **Bold Orange** (`#E67E22`) | *"Consumes Major Action"* |
| `<!!>` | **End Turn** | `turn.EndTurn()` (Action, Move, Sprint) | **Bold Red** (`#E74C3C`) | *"Ends Turn (Consumes Action, Move, and Dash)"* |
| `<M>` | **Movement & Burn Sprint** | `turn.ConsumeMoveAndBurnSprint()` | **Bold Blue** (`#2980B9`) | *"Consumes Movement (Dash / Double Move disabled)"* |
| *(Any)* | **Resource Depleted** | Unclickable / Disabled | **Muted Gray** (`#7F8C8D`) | *"Unavailable: [Reason]"* |

### Turn Economy Rules:
1. **Tags Stripped**: Tags (`<!>`, `<!!>`, `<M>`) are parsed by the compiler and dialogue controller, and are never rendered into the visible button label.
2. **Gated Availability**:
   - If `turn.HasActed == true`, `<!>` and `<!!>` choices are grayed out, unclickable, and display a tooltip explaining that the major action was already spent.
   - If `turn.HasMoved == true`, `<M>` choices are grayed out and unclickable.
3. **Double-Move Dash Lockout (`<M>`)**:
   - Choices tagged `<M>` are agnostic of the double-move dash mechanic: choosing them consumes the move AND burns `canSprint = false`, barring the character from rolling an athletics sprint test for a second move that turn.
4. **Exploration Mode Agnostic**:
   - Outside Crisis mode, all choices are freely selectable, styled in regular font weight and default text colors, with zero cost tags or tooltips.
5. **Color Palette Compendium**:
   - All colors and styling rules are centralized in [`DialogueColorTheme.cs`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Scripts/UI/Dialog/DialogueColorTheme.cs) for designer tuning.

---

## 4. Centralized Style Tokens & Dynamic UI Formatting

To decouple dialogue writers from UI artists, semantic tokens are written directly into dialogue scripts and resolved visually in [`DialogueOptionUI`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Scripts/UI/Dialog/DialogueOptionUI.cs):

| Token | Meaning | Inspector-Configured Style |
| :--- | :--- | :--- |
| `<!>` | Major Action Cost | Orange Bold badge/styling (`DialogueColorTheme.ActionCostColor`) |
| `<!!>` | End Turn Cost | Red Bold badge/styling (`DialogueColorTheme.EndTurnCostColor`) |
| `<M>` | Move Cost (Burns Sprint) | Blue Bold badge/styling (`DialogueColorTheme.MoveCostColor`) |
| `(item: ID xCount)` | Inventory Item Requirement | Consumed upon choice confirmation |
| `{expression}` | C# State Visibility Gate | Evaluated dynamically |

- Writers write clean syntax: `* [<!> Brute force the bulkhead latch] -> ForceNode`
- `DialogueParser` extracts tags into AST flags and cleans visible localization text.
- `DialogueOptionUI` dynamically displays hover tooltips and underline interactions.
- Designers tweak color palettes directly in `DialogueColorTheme.cs` without re-baking dialogue assets.
